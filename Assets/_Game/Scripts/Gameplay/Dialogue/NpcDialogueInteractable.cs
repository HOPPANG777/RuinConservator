using UnityEngine;

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

        dialogueService.TryStartDialogue(_dialogueGraph, _actorOverride);
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
}
