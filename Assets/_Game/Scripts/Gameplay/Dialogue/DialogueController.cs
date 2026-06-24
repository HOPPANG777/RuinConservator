using UnityEngine;
using UnityEngine.InputSystem;

//==============================================================================
// 기능: class::DialogueController
// 설명: 대화 그래프 진행, 선택지 처리, 플레이어 입력 잠금 관리
//==============================================================================
[DisallowMultipleComponent]
public class DialogueController : MonoBehaviour, IDialogueService
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Dialogue")]
    [Tooltip("대화 내용을 실제 UI에 출력하는 뷰입니다.")]
    [SerializeField]
    private DialogueView _dialogueView;

    [Header("Dialogue")]
    [Tooltip("선택지가 없을 때 다음 대사로 넘기는 입력 액션입니다. 보통 상호작용 입력과 같은 액션을 연결합니다.")]
    [SerializeField]
    private InputActionReference _advanceAction;

    [Header("Player Lock")]
    [Tooltip("대화 중 플레이어 조작을 잠글지 여부입니다.")]
    [SerializeField]
    private bool _lockPlayerControl = true;

    [Header("Player Lock")]
    [Tooltip("대화 중 상호작용 탐지와 입력을 막을 플레이어 상호작용 컨트롤러입니다.")]
    [SerializeField]
    private PlayerInteractionController _playerInteractionController;

    [Header("Player Lock")]
    [Tooltip("대화 중 이동을 막을 플레이어 이동 컨트롤러입니다.")]
    [SerializeField]
    private PlayerMoveController _playerMoveController;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 현재 실행 중인 대화 그래프입니다.
    private DialogueGraphSO _currentGraph;

    // NPC에서 넘겨준 기본 화자 덮어쓰기 데이터입니다.
    private DialogueActorSO _actorOverride;

    // 현재 표시 중인 대화 노드입니다.
    private DialogueNodeData _currentNode;

    // 대화가 시작된 프레임입니다. 시작 입력이 곧바로 넘김 입력으로 소비되는 것을 막습니다.
    private int _startedFrame;

    // 현재 대화창이 열려 있는지 여부입니다.
    private bool _isDialogueOpen;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>현재 대화창이 열려 있는지 여부입니다.</summary>
    public bool IsDialogueOpen => _isDialogueOpen;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출하는 초기화 함수입니다.
    /// </summary>
    public void Init()
    {
        if (_dialogueView != null)
        {
            _dialogueView.Hide();
        }
    }

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 다음 대사 입력을 감지합니다.
    /// </summary>
    private void Update()
    {
        if (_isDialogueOpen == false || _advanceAction == null || _advanceAction.action == null)
        {
            return;
        }

        if (Time.frameCount == _startedFrame)
        {
            return;
        }

        if (_currentNode != null && _currentNode.HasChoices)
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
    /// 지정한 대화 그래프를 시작합니다.
    /// </summary>
    /// <param name="dialogueGraph">실행할 대화 그래프입니다.</param>
    /// <param name="actorOverride">NPC별로 덮어쓸 화자 데이터입니다.</param>
    /// <returns>대화를 시작했으면 true입니다.</returns>
    public bool TryStartDialogue(DialogueGraphSO dialogueGraph, DialogueActorSO actorOverride = null)
    {
        if (_isDialogueOpen)
        {
            return false;
        }

        if (dialogueGraph == null)
        {
            Debug.LogWarning("[DialogueController] 시작할 DialogueGraphSO가 없습니다.");
            return false;
        }

        if (_dialogueView == null)
        {
            Debug.LogWarning("[DialogueController] DialogueView가 연결되지 않았습니다.");
            return false;
        }

        if (!dialogueGraph.TryGetStartNode(out DialogueNodeData startNode))
        {
            Debug.LogWarning($"[DialogueController] 시작 노드를 찾을 수 없습니다. graph={dialogueGraph.name}");
            return false;
        }

        _currentGraph = dialogueGraph;
        _actorOverride = actorOverride;
        _currentNode = startNode;
        _isDialogueOpen = true;
        _startedFrame = Time.frameCount;

        SetPlayerControl(false);
        ShowCurrentNode();

        return true;
    }

    /// <summary>
    /// 선택지가 없는 현재 노드를 다음 노드로 진행합니다.
    /// </summary>
    public void Advance()
    {
        if (_isDialogueOpen == false || _currentNode == null)
        {
            return;
        }

        if (_currentNode.HasChoices)
        {
            return;
        }

        MoveToNodeOrEnd(_currentNode.NextNodeId);
    }

    /// <summary>
    /// 현재 노드의 선택지를 선택합니다.
    /// </summary>
    /// <param name="choiceIndex">선택지 인덱스입니다.</param>
    public void SelectChoice(int choiceIndex)
    {
        if (_isDialogueOpen == false || _currentNode == null || _currentNode.HasChoices == false)
        {
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= _currentNode.Choices.Count)
        {
            return;
        }

        DialogueChoiceData choice = _currentNode.Choices[choiceIndex];
        ApplyChoiceAction(choice);

        if (choice.ActionType == DialogueChoiceActionType.END_DIALOGUE)
        {
            EndDialogue();
            return;
        }

        MoveToNodeOrEnd(choice.NextNodeId);
    }

    /// <summary>
    /// 현재 대화를 종료합니다.
    /// </summary>
    public void EndDialogue()
    {
        if (_isDialogueOpen == false)
        {
            return;
        }

        _isDialogueOpen = false;
        _currentGraph = null;
        _actorOverride = null;
        _currentNode = null;

        if (_dialogueView != null)
        {
            _dialogueView.Hide();
        }

        SetPlayerControl(true);
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 현재 노드를 UI에 출력합니다.
    /// </summary>
    private void ShowCurrentNode()
    {
        if (_dialogueView == null || _currentNode == null)
        {
            return;
        }

        DialogueActorSO actor = ResolveActor(_currentNode);
        _dialogueView.ShowNode(actor, _currentNode, SelectChoice);
    }

    /// <summary>
    /// 다음 노드 ID로 이동하거나 대화를 종료합니다.
    /// </summary>
    /// <param name="nextNodeId">다음 노드 ID입니다.</param>
    private void MoveToNodeOrEnd(string nextNodeId)
    {
        if (string.IsNullOrWhiteSpace(nextNodeId))
        {
            EndDialogue();
            return;
        }

        if (_currentGraph == null || !_currentGraph.TryGetNode(nextNodeId, out DialogueNodeData nextNode))
        {
            string graphName = _currentGraph != null ? _currentGraph.name : "(null)";
            Debug.LogWarning($"[DialogueController] 다음 노드를 찾을 수 없습니다. graph={graphName}, nodeId={nextNodeId}");
            EndDialogue();
            return;
        }

        _currentNode = nextNode;
        ShowCurrentNode();
    }

    /// <summary>
    /// 노드, NPC, 그래프 순서로 화자 데이터를 결정합니다.
    /// </summary>
    /// <param name="node">화자를 결정할 노드입니다.</param>
    /// <returns>출력할 화자 데이터입니다.</returns>
    private DialogueActorSO ResolveActor(DialogueNodeData node)
    {
        if (node != null && node.ActorOverride != null)
        {
            return node.ActorOverride;
        }

        if (_actorOverride != null)
        {
            return _actorOverride;
        }

        if (_currentGraph != null)
        {
            return _currentGraph.DefaultActor;
        }

        return null;
    }

    /// <summary>
    /// 선택지에 연결된 간단한 액션을 실행합니다.
    /// </summary>
    /// <param name="choice">선택한 선택지 데이터입니다.</param>
    private void ApplyChoiceAction(DialogueChoiceData choice)
    {
        if (choice == null)
        {
            return;
        }

        switch (choice.ActionType)
        {
            case DialogueChoiceActionType.SET_PROGRESS_FLAG_TRUE:
                SetProgressFlag(choice.ActionId, true);
                break;

            case DialogueChoiceActionType.SET_PROGRESS_FLAG_FALSE:
                SetProgressFlag(choice.ActionId, false);
                break;
        }
    }

    /// <summary>
    /// ProgressController의 전역 플래그 값을 변경합니다.
    /// </summary>
    /// <param name="flagId">변경할 플래그 ID입니다.</param>
    /// <param name="value">저장할 값입니다.</param>
    private void SetProgressFlag(string flagId, bool value)
    {
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return;
        }

        if (!GameRoot.TryGetInstance(out GameRoot gameRoot) || gameRoot.Progress == null)
        {
            Debug.LogWarning($"[DialogueController] ProgressController를 찾을 수 없어 flag를 저장하지 못했습니다. flagId={flagId}");
            return;
        }

        gameRoot.Progress.SetFlag(flagId, value);
    }

    /// <summary>
    /// 대화 중 플레이어 조작 가능 여부를 설정합니다.
    /// </summary>
    /// <param name="canControl">조작 허용 여부입니다.</param>
    private void SetPlayerControl(bool canControl)
    {
        if (_lockPlayerControl == false)
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
