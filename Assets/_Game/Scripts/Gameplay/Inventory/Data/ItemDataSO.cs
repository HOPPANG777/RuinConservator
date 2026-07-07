using UnityEngine;

//==============================================================================
// 기능: class::ItemDataSO
// 설명: 인벤토리에 등록되는 아이템의 원본 데이터 ScriptableObject
//==============================================================================
[CreateAssetMenu(fileName = "ItemData", menuName = "Ruin Conservator/Inventory/Item Data")]
public class ItemDataSO : ScriptableObject
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 문서 기준 한 칸에 중첩 가능한 기본 아이템 개수입니다.
    private const int ITEM_DEFAULT_MAX_STACK_COUNT = 99;

    // 한 슬롯이 가질 수 있는 최소 중첩 가능 개수입니다.
    private const int ITEM_MIN_STACK_COUNT = 1;

    // 아이템 가격의 최소값입니다.
    private const int ITEM_MIN_PRICE = 0;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Identity")]
    [Tooltip("저장과 조회에 사용하는 아이템 고유 ID입니다. 비워두면 인벤토리 DB에 등록할 수 없습니다.")]
    [SerializeField]
    private string _itemId;

    [Header("Display")]
    [Tooltip("인벤토리 UI에 표시할 아이템 이름입니다.")]
    [SerializeField]
    private string _displayName;

    [Header("Display")]
    [Tooltip("인벤토리 상세 페이지에 표시할 아이템 설명입니다.")]
    [TextArea(3, 8)]
    [SerializeField]
    private string _description;

    [Header("Display")]
    [Tooltip("인벤토리 슬롯과 상세 페이지에 표시할 아이템 이미지입니다.")]
    [SerializeField]
    private Sprite _icon;

    [Header("Economy")]
    [Tooltip("아이템 상세 페이지에 표시할 가격입니다.")]
    [Min(ITEM_MIN_PRICE)]
    [SerializeField]
    private int _price;

    [Header("Inventory")]
    [Tooltip("아이템의 용도별 타입입니다.")]
    [SerializeField]
    private ItemType _itemType = ItemType.ITEM_MISC;

    [Header("Inventory")]
    [Tooltip("한 인벤토리 칸에 중첩할 수 있는 최대 개수입니다.")]
    [Min(ITEM_MIN_STACK_COUNT)]
    [SerializeField]
    private int _maxStackCount = ITEM_DEFAULT_MAX_STACK_COUNT;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>저장과 조회에 사용하는 아이템 고유 ID입니다.</summary>
    public string ItemId => _itemId;

    /// <summary>인벤토리 UI에 표시할 아이템 이름입니다.</summary>
    public string DisplayName => _displayName;

    /// <summary>인벤토리 상세 페이지에 표시할 아이템 설명입니다.</summary>
    public string Description => _description;

    /// <summary>인벤토리 슬롯과 상세 페이지에 표시할 아이템 이미지입니다.</summary>
    public Sprite Icon => _icon;

    /// <summary>아이템 상세 페이지에 표시할 가격입니다.</summary>
    public int Price => _price;

    /// <summary>아이템의 용도별 타입입니다.</summary>
    public ItemType ItemType => _itemType;

    /// <summary>한 인벤토리 칸에 중첩할 수 있는 최대 개수입니다.</summary>
    public int MaxStackCount => _maxStackCount;

    /// <summary>저장과 조회에 사용할 수 있는 유효한 아이템 ID인지 여부입니다.</summary>
    public bool HasValidItemId => string.IsNullOrWhiteSpace(_itemId) == false;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 인스펙터 입력값이 인벤토리 규칙을 벗어나지 않도록 보정합니다.
    /// </summary>
    private void OnValidate()
    {
        if (_itemId != null)
        {
            _itemId = _itemId.Trim();
        }

        if (_price < ITEM_MIN_PRICE)
        {
            _price = ITEM_MIN_PRICE;
        }

        if (_maxStackCount < ITEM_MIN_STACK_COUNT)
        {
            _maxStackCount = ITEM_DEFAULT_MAX_STACK_COUNT;
        }
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 저장과 조회에 사용할 수 있는 아이템 ID를 조회합니다.
    /// </summary>
    /// <param name="itemId">조회된 아이템 ID입니다.</param>
    /// <returns>아이템 ID가 비어 있지 않으면 true입니다.</returns>
    public bool TryGetItemId(out string itemId)
    {
        itemId = string.Empty;

        if (string.IsNullOrWhiteSpace(_itemId))
        {
            return false;
        }

        itemId = _itemId.Trim();
        return true;
    }
}
