using UnityEngine;

//==============================================================================
// 기능: class::NpcDialogueInteractable
// 설명: NPC ID를 CSV 대화 연결로 전달해 플레이어 상호작용 대화를 시작하는 컴포넌트
//==============================================================================
[DisallowMultipleComponent]
public sealed class NpcDialogueInteractable : MonoBehaviour, IInteractable, IHighlightable
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Dialogue")]
    [Tooltip("npc_dialogue_bindings.csv의 npc_id와 일치하는 NPC 고유 ID입니다.")]
    [SerializeField]
    private string _npcId;

    [Header("Highlight")]
    [Tooltip("상호작용 포커스 시 켜고 끌 하이라이트 오브젝트입니다.")]
    [SerializeField]
    private GameObject _highlightObject;

    [Tooltip("Awake에서 하이라이트 오브젝트를 숨길지 여부입니다.")]
    [SerializeField]
    private bool _hideHighlightOnAwake = true;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 하이라이트 오브젝트의 초기 표시 상태를 정리합니다.
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
    /// 플레이어가 NPC와 상호작용하면 CSV의 NPC 대화 경로를 시작합니다.
    /// </summary>
    public void Interact()
    {
        if (string.IsNullOrWhiteSpace(_npcId))
        {
            Debug.LogWarning($"[NpcDialogueInteractable] NPC ID가 비어 있습니다. object={name}");
            return;
        }

        if (!GameRoot.TryGetInstance(out GameRoot gameRoot) || gameRoot.Dialogue == null)
        {
            Debug.LogWarning($"[NpcDialogueInteractable] 대화 서비스를 찾을 수 없습니다. npcId={_npcId}");
            return;
        }

        gameRoot.Dialogue.TryStartNpcDialogue(_npcId);
    }

    //==============================================================================
    // IHighlightable
    //==============================================================================

    /// <summary>
    /// 상호작용 포커스 여부에 따라 하이라이트를 표시하거나 숨깁니다.
    /// </summary>
    /// <param name="isActive">하이라이트 활성 여부입니다.</param>
    public void SetHighlight(bool isActive)
    {
        if (_highlightObject != null)
        {
            _highlightObject.SetActive(isActive);
        }
    }
}
