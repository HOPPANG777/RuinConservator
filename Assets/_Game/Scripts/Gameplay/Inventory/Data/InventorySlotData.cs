using UnityEngine;

//==============================================================================
// 기능: class::InventorySlotData
// 설명: 인벤토리 슬롯의 런타임 저장 데이터를 보관하는 순수 데이터 클래스
//==============================================================================
// NOTE:
//  - 저장 데이터가 원본 ItemDataSO를 직접 참조하지 않도록 ItemId만 보관합니다.
//  - 아이콘, 설명, 가격 같은 표시 데이터는 ItemId로 ItemDataSO를 다시 조회해서 사용합니다.
//==============================================================================
[System.Serializable]
public class InventorySlotData
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 슬롯에 보관할 수 있는 최소 아이템 수량입니다.
    private const int INVENTORY_SLOT_MIN_QUANTITY = 1;

    // 플레이어가 아직 수동으로 배치하지 않은 슬롯 순서입니다.
    private const int INVENTORY_SLOT_UNASSIGNED_MANUAL_ORDER = -1;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Tooltip("저장과 조회에 사용하는 아이템 고유 ID입니다.")]
    [SerializeField]
    private string _itemId;

    [Tooltip("현재 슬롯에 들어 있는 아이템 수량입니다.")]
    [SerializeField]
    private int _quantity;

    [Tooltip("최초 획득 후 아직 플레이어가 확인하지 않은 아이템인지 여부입니다.")]
    [SerializeField]
    private bool _isNewItem;

    [Tooltip("플레이어 드래그 조작으로 지정된 수동 배치 순서입니다. -1이면 기본 정렬 대상입니다.")]
    [SerializeField]
    private int _manualSlotOrder = INVENTORY_SLOT_UNASSIGNED_MANUAL_ORDER;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>저장과 조회에 사용하는 아이템 고유 ID입니다.</summary>
    public string ItemId => _itemId;

    /// <summary>현재 슬롯에 들어 있는 아이템 수량입니다.</summary>
    public int Quantity => _quantity;

    /// <summary>최초 획득 후 아직 플레이어가 확인하지 않은 아이템인지 여부입니다.</summary>
    public bool IsNewItem => _isNewItem;

    /// <summary>플레이어 드래그 조작으로 지정된 수동 배치 순서입니다.</summary>
    public int ManualSlotOrder => _manualSlotOrder;

    /// <summary>저장과 조회에 사용할 수 있는 아이템 ID가 있는지 여부입니다.</summary>
    public bool HasValidItemId => string.IsNullOrWhiteSpace(_itemId) == false;

    /// <summary>플레이어가 수동으로 배치한 슬롯 순서가 있는지 여부입니다.</summary>
    public bool HasManualSlotOrder => _manualSlotOrder >= 0;

    /// <summary>아이템 ID가 없거나 수량이 0 이하인 빈 슬롯인지 여부입니다.</summary>
    public bool IsEmpty => HasValidItemId == false || _quantity <= 0;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// Unity 직렬화와 저장/로드 복원을 위한 기본 생성자입니다.
    /// </summary>
    public InventorySlotData()
    {
        _itemId = string.Empty;
        _quantity = 0;
        _isNewItem = false;
        _manualSlotOrder = INVENTORY_SLOT_UNASSIGNED_MANUAL_ORDER;
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 슬롯에 저장할 아이템 ID, 수량, new 표시, 수동 배치 순서를 설정합니다.
    /// </summary>
    /// <param name="itemId">저장과 조회에 사용할 아이템 고유 ID입니다.</param>
    /// <param name="quantity">슬롯에 들어갈 아이템 수량입니다.</param>
    /// <param name="isNewItem">최초 획득 후 아직 확인하지 않은 아이템인지 여부입니다.</param>
    /// <param name="manualSlotOrder">플레이어가 지정한 수동 배치 순서입니다.</param>
    /// <returns>아이템 ID와 수량이 유효하면 true입니다.</returns>
    public bool TrySetSlot(string itemId, int quantity, bool isNewItem, int manualSlotOrder)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (quantity < INVENTORY_SLOT_MIN_QUANTITY)
        {
            return false;
        }

        _itemId = itemId.Trim();
        _quantity = quantity;
        _isNewItem = isNewItem;
        SetManualSlotOrder(manualSlotOrder);

        return true;
    }

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

    /// <summary>
    /// 슬롯 수량을 변경합니다.
    /// </summary>
    /// <param name="quantity">변경할 아이템 수량입니다.</param>
    /// <returns>수량이 1 이상이면 true입니다.</returns>
    public bool TrySetQuantity(int quantity)
    {
        if (quantity < INVENTORY_SLOT_MIN_QUANTITY)
        {
            return false;
        }

        _quantity = quantity;
        return true;
    }

    /// <summary>
    /// 최초 획득 아이템임을 표시합니다.
    /// </summary>
    public void MarkAsNew()
    {
        _isNewItem = true;
    }

    /// <summary>
    /// 플레이어가 아이템을 확인했으므로 new 표시를 해제합니다.
    /// </summary>
    public void MarkAsChecked()
    {
        _isNewItem = false;
    }

    /// <summary>
    /// 플레이어가 드래그로 지정한 수동 배치 순서를 저장합니다.
    /// </summary>
    /// <param name="manualSlotOrder">저장할 수동 배치 순서입니다.</param>
    public void SetManualSlotOrder(int manualSlotOrder)
    {
        if (manualSlotOrder < 0)
        {
            _manualSlotOrder = INVENTORY_SLOT_UNASSIGNED_MANUAL_ORDER;
            return;
        }

        _manualSlotOrder = manualSlotOrder;
    }

    /// <summary>
    /// 수동 배치 순서를 지우고 기본 정렬 대상 슬롯으로 되돌립니다.
    /// </summary>
    public void ClearManualSlotOrder()
    {
        _manualSlotOrder = INVENTORY_SLOT_UNASSIGNED_MANUAL_ORDER;
    }
}
