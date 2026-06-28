using System.Collections.Generic;
using UnityEngine;

//==============================================================================
// 기능: class::NpcQuestDialogueRoute
// 설명: NPC가 퀘스트 상태에 따라 사용할 대화 그래프 매핑
//==============================================================================
[System.Serializable]
public class NpcQuestDialogueRoute
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Quest")]
    [Tooltip("상태를 확인할 퀘스트 데이터입니다. 비워두면 Quest Id를 사용합니다.")]
    [SerializeField]
    private QuestDataSO _questData;

    [Header("Quest")]
    [Tooltip("상태를 확인할 퀘스트 ID입니다. Quest Data가 비어 있을 때 사용합니다.")]
    [SerializeField]
    private string _questId;

    [Header("Quest")]
    [Tooltip("이 상태일 때 아래 대화 그래프를 사용합니다.")]
    [SerializeField]
    private QuestState _questState;

    [Header("Dialogue")]
    [Tooltip("조건이 맞을 때 사용할 대화 그래프입니다.")]
    [SerializeField]
    private DialogueGraphSO _dialogueGraph;

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 현재 퀘스트 상태가 라우트 조건과 일치하는지 확인합니다.
    /// </summary>
    /// <param name="questService">퀘스트 서비스입니다.</param>
    /// <param name="dialogueGraph">조건이 맞을 때 사용할 대화 그래프입니다.</param>
    /// <returns>조건이 일치하면 true입니다.</returns>
    public bool TryGetDialogueGraph(IQuestService questService, out DialogueGraphSO dialogueGraph)
    {
        dialogueGraph = null;

        if (questService == null || _dialogueGraph == null)
        {
            return false;
        }

        QuestState currentState = _questData != null
            ? questService.GetQuestState(_questData)
            : questService.GetQuestState(_questId);

        if (currentState != _questState)
        {
            return false;
        }

        dialogueGraph = _dialogueGraph;
        return true;
    }
}

//==============================================================================
// 기능: class::NpcDialogueInteractable
// 설명: 플레이어 상호작용 입력으로 NPC 대화를 시작하는 컴포넌트
//==============================================================================
[DisallowMultipleComponent]
public class NpcDialogueInteractable : MonoBehaviour, IInteractable, IHighlightable
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Dialogue")]
    [Tooltip("NPC가 실행할 대화 그래프입니다.")]
    [SerializeField]
    private DialogueGraphSO _dialogueGraph;

    [Header("Dialogue")]
    [Tooltip("이 NPC에서 사용할 화자 데이터입니다. 비워두면 DialogueGraph의 기본 화자를 사용합니다.")]
    [SerializeField]
    private DialogueActorSO _actorOverride;

    [Header("Dialogue")]
    [Tooltip("직접 연결할 대화 컨트롤러입니다. 비워두면 GameRoot.Dialogue를 사용합니다.")]
    [SerializeField]
    private DialogueController _dialogueController;

    [Header("Quest Dialogue")]
    [Tooltip("퀘스트 상태에 따라 기본 대화 그래프 대신 사용할 대화 그래프 목록입니다. 위에서부터 먼저 매칭됩니다.")]
    [SerializeField]
    private List<NpcQuestDialogueRoute> _questDialogueRoutes = new List<NpcQuestDialogueRoute>();

    [Header("Highlight")]
    [Tooltip("상호작용 포커스 시 켜고 끌 하이라이트 오브젝트입니다.")]
    [SerializeField]
    private GameObject _highlightObject;

    [Header("Highlight")]
    [Tooltip("Awake 시 하이라이트 오브젝트를 숨길지 여부입니다.")]
    [SerializeField]
    private bool _hideHighlightOnAwake = true;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 하이라이트 초기 상태를 정리합니다.
    /// </summary>
    private void Awake()
    {
        if (_hideHighlightOnAwake && _highlightObject != null)
        {
            _highlightObject.SetActive(false);
        }
    }

    //==============================================================================
    // IInteractable
    //==============================================================================

    /// <summary>
    /// 플레이어가 NPC와 상호작용할 때 대화를 시작합니다.
    /// </summary>
    public void Interact()
    {
        IDialogueService dialogueService = GetDialogueService();

        if (dialogueService == null)
        {
            Debug.LogWarning($"[NpcDialogueInteractable] DialogueController를 찾을 수 없습니다. npc={name}");
            return;
        }

        DialogueGraphSO dialogueGraph = ResolveDialogueGraph();
        dialogueService.TryStartDialogue(dialogueGraph, _actorOverride);
    }

    //==============================================================================
    // IHighlightable
    //==============================================================================

    /// <summary>
    /// 상호작용 포커스 여부에 따라 하이라이트를 토글합니다.
    /// </summary>
    /// <param name="isActive">하이라이트 활성 여부입니다.</param>
    public void SetHighlight(bool isActive)
    {
        if (_highlightObject != null)
        {
            _highlightObject.SetActive(isActive);
        }
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 사용할 대화 서비스를 조회합니다.
    /// </summary>
    /// <returns>대화 서비스입니다.</returns>
    private IDialogueService GetDialogueService()
    {
        if (_dialogueController != null)
        {
            return _dialogueController;
        }

        if (GameRoot.TryGetInstance(out GameRoot gameRoot))
        {
            return gameRoot.Dialogue;
        }

        return null;
    }

    /// <summary>
    /// 퀘스트 상태에 따라 이번에 실행할 대화 그래프를 결정합니다.
    /// </summary>
    /// <returns>실행할 대화 그래프입니다.</returns>
    private DialogueGraphSO ResolveDialogueGraph()
    {
        if (_questDialogueRoutes == null || _questDialogueRoutes.Count == 0)
        {
            return _dialogueGraph;
        }

        if (!TryGetQuestService(out IQuestService questService))
        {
            return _dialogueGraph;
        }

        for (int i = 0; i < _questDialogueRoutes.Count; i++)
        {
            NpcQuestDialogueRoute route = _questDialogueRoutes[i];

            if (route != null && route.TryGetDialogueGraph(questService, out DialogueGraphSO dialogueGraph))
            {
                return dialogueGraph;
            }
        }

        return _dialogueGraph;
    }

    /// <summary>
    /// GameRoot에서 퀘스트 서비스를 조회합니다.
    /// </summary>
    /// <param name="questService">조회된 퀘스트 서비스입니다.</param>
    /// <returns>퀘스트 서비스를 찾으면 true입니다.</returns>
    private bool TryGetQuestService(out IQuestService questService)
    {
        questService = null;

        if (GameRoot.TryGetInstance(out GameRoot gameRoot) && gameRoot.Quest != null)
        {
            questService = gameRoot.Quest;
            return true;
        }

        return false;
    }
}
