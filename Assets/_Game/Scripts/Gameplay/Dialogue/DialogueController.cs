using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//==============================================================================
// 기능: class::DialogueController
// 설명: CSV 대화 Definition의 진행, 선택 액션, UI 출력과 플레이어 입력 잠금을 관리하는 컨트롤러
//==============================================================================
[DisallowMultipleComponent]
public sealed class DialogueController : MonoBehaviour, IDialogueService
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Dialogue")]
    [Tooltip("CSV 대화 내용을 실제 UI에 출력하는 뷰입니다.")]
    [SerializeField]
    private DialogueView _dialogueView;

    [Tooltip("CSV 비주얼 에셋 ID를 Unity Sprite로 변환하는 카탈로그입니다.")]
    [SerializeField]
    private DialogueVisualCatalogSO _visualCatalog;

    [Tooltip("선택지가 없을 때 다음 대사로 진행하는 입력 액션입니다.")]
    [SerializeField]
    private InputActionReference _advanceAction;

    [Header("Player Lock")]
    [Tooltip("대화 중 플레이어 조작을 잠글지 여부입니다.")]
    [SerializeField]
    private bool _lockPlayerControl = true;

    [Tooltip("대화 중 상호작용 탐지와 입력을 막을 플레이어 상호작용 컨트롤러입니다.")]
    [SerializeField]
    private PlayerInteractionController _playerInteractionController;

    [Tooltip("대화 중 이동을 막을 플레이어 이동 컨트롤러입니다.")]
    [SerializeField]
    private PlayerMoveController _playerMoveController;

    //==============================================================================
    // 내부 상태 - 의존성
    //==============================================================================

    // 대화 Definition과 NPC 경로를 조회할 읽기 전용 게임 데이터 저장소입니다.
    private IGameDataRepository _gameDataRepository;

    // 대화 문장 Key를 현재 언어 문자열로 변환할 서비스입니다.
    private ILocalizationService _localizationService;

    // 선택지의 Progress Flag 액션을 실행할 서비스입니다.
    private IProgressService _progressService;

    // 선택지와 NPC 경로의 퀘스트 상태를 처리할 서비스입니다.
    private IQuestService _questService;

    //==============================================================================
    // 내부 상태 - 대화
    //==============================================================================

    // 현재 실행 중인 CSV 대화 그래프 Definition입니다.
    private DialogueGraphDefinition _currentGraph;

    // NPC 연결에서 전달된 선택적 기본 화자 덮어쓰기 ID입니다.
    private string _actorOverrideId;

    // 현재 표시 중인 CSV 대화 노드 Definition입니다.
    private DialogueNodeDefinition _currentNode;

    // 시작 입력이 같은 프레임의 진행 입력으로 소비되지 않도록 기록한 프레임입니다.
    private int _startedFrame;

    // 현재 대화창이 열려 있는지 여부입니다.
    private bool _isDialogueOpen;

    // 진행 액션을 이 컨트롤러가 활성화했는지 여부입니다.
    private bool _didEnableAdvanceAction;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>현재 대화창이 열려 있는지 여부입니다.</summary>
    public bool IsDialogueOpen => _isDialogueOpen;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 대화 진행에 필요한 읽기 전용 계약들을 주입합니다.
    /// </summary>
    /// <param name="gameDataRepository">CSV 게임 데이터 저장소입니다.</param>
    /// <param name="localizationService">현재 언어 문장 서비스입니다.</param>
    /// <param name="progressService">Progress Flag 서비스입니다.</param>
    /// <param name="questService">퀘스트 상태 서비스입니다.</param>
    public void Init(
        IGameDataRepository gameDataRepository,
        ILocalizationService localizationService,
        IProgressService progressService,
        IQuestService questService)
    {
        _gameDataRepository = gameDataRepository;
        _localizationService = localizationService;
        _progressService = progressService;
        _questService = questService;

        if (_dialogueView != null)
        {
            _dialogueView.Init(_localizationService);
        }

        if (_gameDataRepository == null)
        {
            Debug.LogError("[DialogueController] 게임 데이터 저장소가 주입되지 않았습니다.");
        }
    }

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 다른 시스템에서 사용하지 않는 대화 진행 입력 액션을 활성화합니다.
    /// </summary>
    private void OnEnable()
    {
        if (_advanceAction == null || _advanceAction.action == null || _advanceAction.action.enabled)
        {
            return;
        }

        _advanceAction.action.Enable();
        _didEnableAdvanceAction = true;
    }

    /// <summary>
    /// 이 컨트롤러가 활성화했던 대화 진행 입력 액션만 비활성화합니다.
    /// </summary>
    private void OnDisable()
    {
        if (_didEnableAdvanceAction && _advanceAction != null && _advanceAction.action != null)
        {
            _advanceAction.action.Disable();
        }

        _didEnableAdvanceAction = false;
    }

    /// <summary>
    /// 선택지가 없는 현재 대화 노드의 진행 입력을 감지합니다.
    /// </summary>
    private void Update()
    {
        if (!_isDialogueOpen || _advanceAction == null || _advanceAction.action == null)
        {
            return;
        }

        if (Time.frameCount == _startedFrame || (_currentNode != null && _currentNode.HasChoices))
        {
            return;
        }

        if (_advanceAction.action.WasPressedThisFrame())
        {
            Advance();
        }
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 대화 그래프 ID와 선택적 화자 ID로 대화를 시작합니다.
    /// </summary>
    /// <param name="graphId">실행할 CSV 대화 그래프 ID입니다.</param>
    /// <param name="actorOverrideId">그래프 기본 화자를 덮어쓸 선택적 화자 ID입니다.</param>
    /// <returns>대화를 시작했으면 true입니다.</returns>
    public bool TryStartDialogue(string graphId, string actorOverrideId = null)
    {
        if (_isDialogueOpen || _gameDataRepository == null || string.IsNullOrWhiteSpace(graphId))
        {
            return false;
        }

        if (_dialogueView == null)
        {
            Debug.LogWarning("[DialogueController] DialogueView가 연결되지 않았습니다.");
            return false;
        }

        if (!_gameDataRepository.TryGetDialogueGraph(graphId, out DialogueGraphDefinition graph))
        {
            Debug.LogWarning($"[DialogueController] 대화 그래프를 찾을 수 없습니다. graphId={graphId}");
            return false;
        }

        if (!_gameDataRepository.TryGetDialogueNode(graph.GraphId, graph.StartNodeId, out DialogueNodeDefinition startNode))
        {
            Debug.LogWarning($"[DialogueController] 시작 노드를 찾을 수 없습니다. graphId={graphId}");
            return false;
        }

        _currentGraph = graph;
        _actorOverrideId = actorOverrideId ?? string.Empty;
        _currentNode = startNode;
        _isDialogueOpen = true;
        _startedFrame = Time.frameCount;

        SetPlayerControl(false);
        ShowCurrentNode();
        return true;
    }

    /// <summary>
    /// NPC ID의 기본 연결과 퀘스트 상태별 경로를 해석해 대화를 시작합니다.
    /// </summary>
    /// <param name="npcId">상호작용한 NPC ID입니다.</param>
    /// <returns>NPC 대화를 시작했으면 true입니다.</returns>
    public bool TryStartNpcDialogue(string npcId)
    {
        if (_gameDataRepository == null || string.IsNullOrWhiteSpace(npcId))
        {
            return false;
        }

        if (!_gameDataRepository.TryGetNpcDialogueBinding(npcId, out NpcDialogueBindingDefinition binding))
        {
            Debug.LogWarning($"[DialogueController] NPC 대화 연결을 찾을 수 없습니다. npcId={npcId}");
            return false;
        }

        string graphId = ResolveNpcGraphId(npcId, binding.DefaultGraphId);
        return TryStartDialogue(graphId, binding.ActorOverrideId);
    }

    /// <summary>
    /// 선택지가 없는 현재 노드를 다음 노드로 진행합니다.
    /// </summary>
    public void Advance()
    {
        if (!_isDialogueOpen || _currentNode == null || _currentNode.HasChoices)
        {
            return;
        }

        MoveToNodeOrEnd(_currentNode.NextNodeId);
    }

    /// <summary>
    /// 현재 노드의 선택지를 선택하고 연결된 액션과 이동을 처리합니다.
    /// </summary>
    /// <param name="choiceIndex">선택지 인덱스입니다.</param>
    public void SelectChoice(int choiceIndex)
    {
        if (!_isDialogueOpen || _currentNode == null || !_currentNode.HasChoices)
        {
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= _currentNode.Choices.Count)
        {
            return;
        }

        DialogueChoiceDefinition choice = _currentNode.Choices[choiceIndex];
        ApplyChoiceAction(choice);

        if (choice.ActionType == DialogueActionType.END_DIALOGUE)
        {
            EndDialogue();
            return;
        }

        MoveToNodeOrEnd(choice.NextNodeId);
    }

    /// <summary>
    /// 현재 대화를 종료하고 UI와 플레이어 조작 상태를 복원합니다.
    /// </summary>
    public void EndDialogue()
    {
        if (!_isDialogueOpen)
        {
            return;
        }

        _isDialogueOpen = false;
        _currentGraph = null;
        _actorOverrideId = string.Empty;
        _currentNode = null;

        if (_dialogueView != null)
        {
            _dialogueView.Hide();
        }

        SetPlayerControl(true);
    }

    //==============================================================================
    // 내부 로직 - NPC 경로
    //==============================================================================

    /// <summary>
    /// NPC의 우선순위별 퀘스트 상태 경로에서 실행할 대화 그래프 ID를 선택합니다.
    /// </summary>
    /// <param name="npcId">경로를 조회할 NPC ID입니다.</param>
    /// <param name="defaultGraphId">조건이 일치하지 않을 때 사용할 기본 그래프 ID입니다.</param>
    /// <returns>현재 퀘스트 상태에 맞는 그래프 ID입니다.</returns>
    private string ResolveNpcGraphId(string npcId, string defaultGraphId)
    {
        if (_questService == null
            || !_gameDataRepository.TryGetNpcQuestDialogueRoutes(
                npcId,
                out IReadOnlyList<NpcQuestDialogueRouteDefinition> routes))
        {
            return defaultGraphId;
        }

        for (int i = 0; i < routes.Count; i++)
        {
            NpcQuestDialogueRouteDefinition route = routes[i];

            if (route != null && _questService.GetQuestState(route.QuestId) == route.QuestState)
            {
                return route.GraphId;
            }
        }

        return defaultGraphId;
    }

    //==============================================================================
    // 내부 로직 - 노드 표시
    //==============================================================================

    /// <summary>
    /// 현재 Definition에서 문장 Key와 Unity Sprite를 해석해 대화 UI에 출력합니다.
    /// </summary>
    private void ShowCurrentNode()
    {
        if (_dialogueView == null || _currentGraph == null || _currentNode == null)
        {
            return;
        }

        string actorId = ResolveCurrentActorId();

        if (!_gameDataRepository.TryGetDialogueActor(actorId, out DialogueActorDefinition actor))
        {
            Debug.LogWarning($"[DialogueController] 대화 화자를 찾을 수 없습니다. actorId={actorId}");
            EndDialogue();
            return;
        }

        string leftActorId = _currentGraph.LeftActorId;
        string rightActorId = _currentGraph.RightActorId;
        bool isLeftActorSpeaking = ResolveSpeakerSide(actorId, ref leftActorId, ref rightActorId);
        Sprite speakingPortrait = ResolvePortraitSprite(actor, _currentNode.PortraitId);
        Sprite leftPortrait = ResolveDefaultPortraitSprite(leftActorId);
        Sprite rightPortrait = ResolveDefaultPortraitSprite(rightActorId);

        if (isLeftActorSpeaking)
        {
            leftPortrait = speakingPortrait;
        }
        else
        {
            rightPortrait = speakingPortrait;
        }

        _dialogueView.ShowNode(
            ResolveSpeakerNameKey(actor, _currentNode.PortraitId),
            _currentNode,
            SelectChoice,
            leftPortrait,
            rightPortrait,
            isLeftActorSpeaking,
            ResolveBackgroundSprite());
    }

    /// <summary>
    /// 다음 노드 ID로 이동하거나 ID가 비어 있으면 대화를 종료합니다.
    /// </summary>
    /// <param name="nextNodeId">다음 대화 노드 ID입니다.</param>
    private void MoveToNodeOrEnd(string nextNodeId)
    {
        if (string.IsNullOrWhiteSpace(nextNodeId))
        {
            EndDialogue();
            return;
        }

        if (_currentGraph == null
            || !_gameDataRepository.TryGetDialogueNode(
                _currentGraph.GraphId,
                nextNodeId,
                out DialogueNodeDefinition nextNode))
        {
            string graphId = _currentGraph != null ? _currentGraph.GraphId : "(null)";
            Debug.LogWarning(
                $"[DialogueController] 다음 노드를 찾을 수 없습니다. graphId={graphId}, nodeId={nextNodeId}");
            EndDialogue();
            return;
        }

        _currentNode = nextNode;
        ShowCurrentNode();
    }

    /// <summary>
    /// 노드, NPC 덮어쓰기, 그래프 기본값 순서로 현재 화자 ID를 결정합니다.
    /// </summary>
    /// <returns>현재 노드에서 말하는 화자 ID입니다.</returns>
    private string ResolveCurrentActorId()
    {
        if (_currentNode != null && !string.IsNullOrWhiteSpace(_currentNode.ActorId))
        {
            return _currentNode.ActorId;
        }

        if (!string.IsNullOrWhiteSpace(_actorOverrideId))
        {
            return _actorOverrideId;
        }

        return _currentGraph != null ? _currentGraph.DefaultActorId : string.Empty;
    }

    /// <summary>
    /// 현재 화자를 좌우 참가자에 배치하고 왼쪽 화자인지 결정합니다.
    /// </summary>
    /// <param name="actorId">현재 말하는 화자 ID입니다.</param>
    /// <param name="leftActorId">왼쪽 참가자 ID이며 비어 있으면 현재 화자로 채웁니다.</param>
    /// <param name="rightActorId">오른쪽 참가자 ID입니다.</param>
    /// <returns>현재 화자가 왼쪽 위치에 있으면 true입니다.</returns>
    private static bool ResolveSpeakerSide(
        string actorId,
        ref string leftActorId,
        ref string rightActorId)
    {
        if (actorId == rightActorId)
        {
            return false;
        }

        if (actorId == leftActorId)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(leftActorId))
        {
            leftActorId = actorId;
            return true;
        }

        if (string.IsNullOrWhiteSpace(rightActorId))
        {
            rightActorId = actorId;
            return false;
        }

        leftActorId = actorId;
        return true;
    }

    /// <summary>
    /// 화자의 지정 초상화 또는 기본 초상화 Sprite를 조회합니다.
    /// </summary>
    /// <param name="actor">초상화를 소유한 화자 Definition입니다.</param>
    /// <param name="portraitId">우선 적용할 선택적 초상화 ID입니다.</param>
    /// <returns>카탈로그에서 찾은 Unity Sprite이며 없으면 null입니다.</returns>
    private Sprite ResolvePortraitSprite(DialogueActorDefinition actor, string portraitId)
    {
        if (actor == null || _visualCatalog == null)
        {
            return null;
        }

        string resolvedPortraitId = !string.IsNullOrWhiteSpace(portraitId)
            ? portraitId
            : actor.DefaultPortraitId;

        if (TryResolvePortraitSprite(actor.ActorId, resolvedPortraitId, out Sprite sprite))
        {
            return sprite;
        }

        if (resolvedPortraitId == actor.DefaultPortraitId)
        {
            return null;
        }

        return TryResolvePortraitSprite(actor.ActorId, actor.DefaultPortraitId, out sprite)
            ? sprite
            : null;
    }

    /// <summary>
    /// 화자와 초상화 ID를 CSV Sprite ID로 변환한 뒤 비주얼 카탈로그에서 조회합니다.
    /// </summary>
    /// <param name="actorId">초상화를 소유한 화자 ID입니다.</param>
    /// <param name="portraitId">조회할 초상화 ID입니다.</param>
    /// <param name="sprite">조회된 Unity Sprite입니다.</param>
    /// <returns>초상화 Definition과 Unity Sprite를 모두 찾으면 true입니다.</returns>
    private bool TryResolvePortraitSprite(string actorId, string portraitId, out Sprite sprite)
    {
        sprite = null;

        if (!_gameDataRepository.TryGetDialoguePortrait(
            actorId,
            portraitId,
            out DialoguePortraitDefinition portrait))
        {
            return false;
        }

        return _visualCatalog.TryGetPortraitSprite(portrait.SpriteId, out sprite);
    }

    /// <summary>
    /// 화자 ID의 기본 초상화 Sprite를 조회합니다.
    /// </summary>
    /// <param name="actorId">기본 초상화를 조회할 화자 ID입니다.</param>
    /// <returns>카탈로그에서 찾은 기본 초상화 Sprite이며 없으면 null입니다.</returns>
    private Sprite ResolveDefaultPortraitSprite(string actorId)
    {
        return _gameDataRepository.TryGetDialogueActor(actorId, out DialogueActorDefinition actor)
            ? ResolvePortraitSprite(actor, actor.DefaultPortraitId)
            : null;
    }

    /// <summary>
    /// 초상화별 이름 Key가 있으면 우선하고 없으면 화자 기본 이름 Key를 사용합니다.
    /// </summary>
    /// <param name="actor">현재 화자 Definition입니다.</param>
    /// <param name="portraitId">현재 노드의 선택적 초상화 ID입니다.</param>
    /// <returns>화자 이름에 사용할 Localization Key입니다.</returns>
    private string ResolveSpeakerNameKey(DialogueActorDefinition actor, string portraitId)
    {
        string resolvedPortraitId = !string.IsNullOrWhiteSpace(portraitId)
            ? portraitId
            : actor.DefaultPortraitId;

        if (_gameDataRepository.TryGetDialoguePortrait(
                actor.ActorId,
                resolvedPortraitId,
                out DialoguePortraitDefinition portrait)
            && !string.IsNullOrWhiteSpace(portrait.DisplayNameKey))
        {
            return portrait.DisplayNameKey;
        }

        return actor.DisplayNameKey;
    }

    /// <summary>
    /// 노드 배경 ID를 우선하고 없으면 그래프 기본 배경 ID로 Sprite를 조회합니다.
    /// </summary>
    /// <returns>카탈로그에서 찾은 배경 Sprite이며 없으면 null입니다.</returns>
    private Sprite ResolveBackgroundSprite()
    {
        if (_visualCatalog == null)
        {
            return null;
        }

        string backgroundId = !string.IsNullOrWhiteSpace(_currentNode.BackgroundId)
            ? _currentNode.BackgroundId
            : _currentGraph.BackgroundId;
        return _visualCatalog.TryGetBackgroundSprite(backgroundId, out Sprite sprite)
            ? sprite
            : null;
    }

    //==============================================================================
    // 내부 로직 - 선택 액션
    //==============================================================================

    /// <summary>
    /// 선택지에 연결된 Progress 또는 Quest 액션을 실행합니다.
    /// </summary>
    /// <param name="choice">선택한 CSV 선택지 Definition입니다.</param>
    private void ApplyChoiceAction(DialogueChoiceDefinition choice)
    {
        if (choice == null)
        {
            return;
        }

        switch (choice.ActionType)
        {
            case DialogueActionType.SET_PROGRESS_FLAG_TRUE:
                SetProgressFlag(choice.ActionId, true);
                break;

            case DialogueActionType.SET_PROGRESS_FLAG_FALSE:
                SetProgressFlag(choice.ActionId, false);
                break;

            case DialogueActionType.ACCEPT_QUEST:
                _questService?.TryAcceptQuest(choice.ActionId);
                break;

            case DialogueActionType.REFRESH_QUEST_PROGRESS:
                _questService?.RefreshQuestProgress(choice.ActionId);
                break;

            case DialogueActionType.COMPLETE_QUEST:
                _questService?.TryCompleteQuest(choice.ActionId);
                break;

            case DialogueActionType.CLAIM_QUEST_REWARD:
                _questService?.TryClaimReward(choice.ActionId);
                break;
        }
    }

    /// <summary>
    /// Progress Flag 값을 변경하고 진행 중인 퀘스트 목표를 갱신합니다.
    /// </summary>
    /// <param name="flagId">변경할 Progress Flag ID입니다.</param>
    /// <param name="value">저장할 값입니다.</param>
    private void SetProgressFlag(string flagId, bool value)
    {
        if (_progressService == null || string.IsNullOrWhiteSpace(flagId))
        {
            return;
        }

        _progressService.SetFlag(flagId, value);
        _questService?.RefreshAllInProgressQuests();
    }

    //==============================================================================
    // 내부 로직 - 플레이어 잠금
    //==============================================================================

    /// <summary>
    /// 대화 중 플레이어 상호작용과 이동 가능 여부를 설정합니다.
    /// </summary>
    /// <param name="canControl">플레이어 조작 허용 여부입니다.</param>
    private void SetPlayerControl(bool canControl)
    {
        if (!_lockPlayerControl)
        {
            return;
        }

        if (_playerInteractionController != null)
        {
            _playerInteractionController.SetCanInteract(canControl);
        }

        if (_playerMoveController != null)
        {
            _playerMoveController.SetCanMove(canControl);
        }
    }
}
