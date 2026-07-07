using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//==============================================================================
// 기능: class::InventorySlotView
// 설명: 인벤토리 슬롯 한 칸의 표시와 선택/드래그 입력을 담당하는 UI 뷰
//==============================================================================
public class InventorySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 드래그 중인 슬롯의 표시 투명도입니다.
    private const float INVENTORY_SLOT_DRAG_ALPHA = 0.65f;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Slot")]
    [Tooltip("슬롯 선택 가능 영역으로 사용할 버튼입니다.")]
    [SerializeField]
    private Button _button;

    [Header("Slot")]
    [Tooltip("슬롯 전체 투명도와 Raycast 차단 상태를 제어할 CanvasGroup입니다.")]
    [SerializeField]
    private CanvasGroup _canvasGroup;

    [Header("Display")]
    [Tooltip("아이템 아이콘을 출력할 Image입니다.")]
    [SerializeField]
    private Image _iconImage;

    [Header("Display")]
    [Tooltip("아이템 수량을 출력할 TMP 텍스트입니다.")]
    [SerializeField]
    private TMP_Text _quantityText;

    [Header("Display")]
    [Tooltip("최초 획득 아이템 new 표시 오브젝트입니다.")]
    [SerializeField]
    private GameObject _newMarkerRoot;

    [Header("Display")]
    [Tooltip("현재 선택된 슬롯 표시 오브젝트입니다.")]
    [SerializeField]
    private GameObject _selectedMarkerRoot;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 현재 슬롯 뷰가 표시하는 슬롯 인덱스입니다.
    private int _slotIndex;

    // 현재 슬롯 뷰가 표시하는 런타임 슬롯 데이터입니다.
    private InventorySlotData _slotData;

    // 슬롯 선택 입력 콜백입니다.
    private Action<int> _slotClicked;

    // 슬롯 드래그 시작 콜백입니다.
    private Action<int, PointerEventData> _slotDragStarted;

    // 슬롯 드래그 중 콜백입니다.
    private Action<int, PointerEventData> _slotDragged;

    // 슬롯 드래그 종료 콜백입니다.
    private Action<int, PointerEventData> _slotDragEnded;

    // 슬롯 드롭 콜백입니다.
    private Action<int, int> _slotDropped;

    // 현재 슬롯을 드래그 이동할 수 있는지 여부입니다.
    private bool _canDrag;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>현재 슬롯 뷰가 표시하는 슬롯 인덱스입니다.</summary>
    public int SlotIndex => _slotIndex;

    /// <summary>현재 슬롯 뷰가 표시하는 런타임 슬롯 데이터입니다.</summary>
    public InventorySlotData SlotData => _slotData;

    /// <summary>현재 슬롯 뷰가 실제 아이템 슬롯을 표시 중인지 여부입니다.</summary>
    public bool HasSlotData => _slotData != null && _slotData.IsEmpty == false;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 같은 오브젝트에 있는 기본 UI 컴포넌트 참조를 보강합니다.
    /// </summary>
    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 슬롯 표시 데이터와 입력 콜백을 갱신합니다.
    /// </summary>
    /// <param name="slotIndex">표시할 슬롯 인덱스입니다.</param>
    /// <param name="slotData">표시할 런타임 슬롯 데이터입니다.</param>
    /// <param name="itemData">표시에 사용할 아이템 원본 데이터입니다.</param>
    /// <param name="isSelected">현재 선택된 슬롯인지 여부입니다.</param>
    /// <param name="canDrag">드래그 이동 가능 여부입니다.</param>
    /// <param name="slotClicked">슬롯 선택 입력 콜백입니다.</param>
    /// <param name="slotDragStarted">슬롯 드래그 시작 콜백입니다.</param>
    /// <param name="slotDragged">슬롯 드래그 중 콜백입니다.</param>
    /// <param name="slotDragEnded">슬롯 드래그 종료 콜백입니다.</param>
    /// <param name="slotDropped">슬롯 드롭 콜백입니다.</param>
    public void Bind(
        int slotIndex,
        InventorySlotData slotData,
        ItemDataSO itemData,
        bool isSelected,
        bool canDrag,
        Action<int> slotClicked,
        Action<int, PointerEventData> slotDragStarted,
        Action<int, PointerEventData> slotDragged,
        Action<int, PointerEventData> slotDragEnded,
        Action<int, int> slotDropped)
    {
        _slotIndex = slotIndex;
        _slotData = slotData;
        _canDrag = canDrag;
        _slotClicked = slotClicked;
        _slotDragStarted = slotDragStarted;
        _slotDragged = slotDragged;
        _slotDragEnded = slotDragEnded;
        _slotDropped = slotDropped;

        gameObject.SetActive(true);
        ApplyItemDisplay(slotData, itemData);
        SetSelected(isSelected);
        ResetDragVisual();
    }

    /// <summary>
    /// 슬롯 표시와 입력 콜백을 비우고 숨깁니다.
    /// </summary>
    public void Clear()
    {
        _slotIndex = -1;
        _slotData = null;
        _canDrag = false;
        _slotClicked = null;
        _slotDragStarted = null;
        _slotDragged = null;
        _slotDragEnded = null;
        _slotDropped = null;

        ApplyItemDisplay(null, null);
        SetSelected(false);
        ResetDragVisual();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 슬롯 선택 표시 상태를 변경합니다.
    /// </summary>
    /// <param name="isSelected">선택 표시 여부입니다.</param>
    public void SetSelected(bool isSelected)
    {
        if (_selectedMarkerRoot != null)
        {
            _selectedMarkerRoot.SetActive(isSelected);
        }
    }

    //==============================================================================
    // EventSystem API
    //==============================================================================

    /// <summary>
    /// 슬롯 클릭 입력을 처리합니다.
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_slotClicked != null)
        {
            _slotClicked(_slotIndex);
        }
    }

    /// <summary>
    /// 슬롯 드래그 시작 입력을 처리합니다.
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_canDrag == false || HasSlotData == false)
        {
            return;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = INVENTORY_SLOT_DRAG_ALPHA;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_slotDragStarted != null)
        {
            _slotDragStarted(_slotIndex, eventData);
        }
    }

    /// <summary>
    /// 슬롯 드래그 중 입력을 처리합니다.
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (_canDrag == false || HasSlotData == false)
        {
            return;
        }

        if (_slotDragged != null)
        {
            _slotDragged(_slotIndex, eventData);
        }
    }

    /// <summary>
    /// 슬롯 드래그 종료 입력을 처리합니다.
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        ResetDragVisual();

        if (_canDrag == false || HasSlotData == false)
        {
            return;
        }

        if (_slotDragEnded != null)
        {
            _slotDragEnded(_slotIndex, eventData);
        }
    }

    /// <summary>
    /// 다른 슬롯이 이 슬롯 위에 드롭되었을 때 처리합니다.
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
        {
            return;
        }

        InventorySlotView sourceSlotView = eventData.pointerDrag.GetComponentInParent<InventorySlotView>();

        if (sourceSlotView == null || sourceSlotView.HasSlotData == false)
        {
            return;
        }

        if (_slotDropped != null)
        {
            _slotDropped(sourceSlotView.SlotIndex, _slotIndex);
        }
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 아이템 아이콘, 수량, new 표시를 갱신합니다.
    /// </summary>
    /// <param name="slotData">표시할 런타임 슬롯 데이터입니다.</param>
    /// <param name="itemData">표시에 사용할 아이템 원본 데이터입니다.</param>
    private void ApplyItemDisplay(InventorySlotData slotData, ItemDataSO itemData)
    {
        bool hasSlotData = slotData != null && slotData.IsEmpty == false;
        Sprite icon = itemData != null ? itemData.Icon : null;

        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = hasSlotData && icon != null;
        }

        if (_quantityText != null)
        {
            _quantityText.text = hasSlotData ? slotData.Quantity.ToString() : string.Empty;
        }

        if (_newMarkerRoot != null)
        {
            _newMarkerRoot.SetActive(hasSlotData && slotData.IsNewItem);
        }

        if (_button != null)
        {
            _button.interactable = true;
        }
    }

    /// <summary>
    /// 드래그 중 변경한 표시 상태를 기본값으로 되돌립니다.
    /// </summary>
    private void ResetDragVisual()
    {
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = 1.0f;
        _canvasGroup.blocksRaycasts = true;
    }
}
