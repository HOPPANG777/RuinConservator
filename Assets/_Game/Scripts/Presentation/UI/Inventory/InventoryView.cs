using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//==============================================================================
// 기능: class::InventoryView
// 설명: 인벤토리 1페이지 슬롯 목록과 2페이지 선택 아이템 상세 정보를 출력하는 UI 뷰
//==============================================================================
public class InventoryView : MonoBehaviour
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 문서 기준 한 행에 표시하는 슬롯 수입니다.
    private const int INVENTORY_VIEW_COLUMN_COUNT = 6;

    // 문서 기준 기본 표시 행 수입니다.
    private const int INVENTORY_VIEW_BASE_ROW_COUNT = 5;

    // 문서 기준 1페이지 기본 슬롯 수입니다.
    private const int INVENTORY_VIEW_BASE_SLOT_COUNT = INVENTORY_VIEW_COLUMN_COUNT * INVENTORY_VIEW_BASE_ROW_COUNT;

    // 드래그 중 ScrollRect 가장자리에서 자동 스크롤을 시작하는 거리입니다.
    private const float INVENTORY_VIEW_AUTO_SCROLL_EDGE_SIZE = 48.0f;

    // 드래그 중 ScrollRect 자동 스크롤 속도입니다.
    private const float INVENTORY_VIEW_AUTO_SCROLL_SPEED = 900.0f;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Root")]
    [Tooltip("인벤토리 UI 전체 표시/입력 차단을 제어할 CanvasGroup입니다.")]
    [SerializeField]
    private CanvasGroup _rootCanvasGroup;

    [Header("Page")]
    [Tooltip("1페이지 슬롯 목록 영역의 표시를 제어할 CanvasGroup입니다.")]
    [SerializeField]
    private CanvasGroup _slotPageCanvasGroup;

    [Header("Page")]
    [Tooltip("2페이지 선택 아이템 상세 영역의 표시를 제어할 CanvasGroup입니다.")]
    [SerializeField]
    private CanvasGroup _detailPageCanvasGroup;

    [Header("Slots")]
    [Tooltip("30개 초과 슬롯을 스크롤할 ScrollRect입니다.")]
    [SerializeField]
    private ScrollRect _slotScrollRect;

    [Header("Slots")]
    [Tooltip("슬롯 뷰들이 배치될 콘텐츠 RectTransform입니다.")]
    [SerializeField]
    private RectTransform _slotContentRoot;

    [Header("Slots")]
    [Tooltip("슬롯 6열 배치를 담당할 GridLayoutGroup입니다.")]
    [SerializeField]
    private GridLayoutGroup _slotGridLayout;

    [Header("Slots")]
    [Tooltip("인벤토리 슬롯 한 칸을 출력할 슬롯 뷰 프리팹입니다.")]
    [SerializeField]
    private InventorySlotView _slotViewPrefab;

    [Header("Slots")]
    [Tooltip("슬롯 행 수가 늘어날 때 콘텐츠 높이를 자동 보정할지 여부입니다.")]
    [SerializeField]
    private bool _resizeSlotContentHeight = true;

    [Header("Slots")]
    [Tooltip("슬롯 드래그로 수동 배치 순서를 변경할 수 있는지 여부입니다.")]
    [SerializeField]
    private bool _allowSlotDragMove = true;

    [Header("Selected Item")]
    [Tooltip("선택한 아이템 아이콘을 출력할 Image입니다.")]
    [SerializeField]
    private Image _selectedItemIconImage;

    [Header("Selected Item")]
    [Tooltip("선택한 아이템 이름을 출력할 TMP 텍스트입니다.")]
    [SerializeField]
    private TMP_Text _selectedItemNameText;

    [Header("Selected Item")]
    [Tooltip("선택한 아이템 가격을 출력할 TMP 텍스트입니다.")]
    [SerializeField]
    private TMP_Text _selectedItemPriceText;

    [Header("Selected Item")]
    [Tooltip("선택한 아이템 설명을 출력할 TMP 텍스트입니다.")]
    [SerializeField]
    private TMP_Text _selectedItemDescriptionText;

    [Header("Selected Item")]
    [Tooltip("선택한 아이템이 없을 때 아이콘 Image를 숨길지 여부입니다.")]
    [SerializeField]
    private bool _hideSelectedIconWhenEmpty = true;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 재사용할 슬롯 뷰 목록입니다.
    private readonly List<InventorySlotView> _slotViews = new List<InventorySlotView>();

    // 현재 카테고리에서 표시 중인 슬롯 데이터 스냅샷입니다.
    private readonly List<InventorySlotData> _currentSlots = new List<InventorySlotData>();

    // 현재 카테고리에서 표시 중인 아이템 원본 데이터 스냅샷입니다.
    private readonly List<ItemDataSO> _currentItems = new List<ItemDataSO>();

    // 슬롯 선택 입력 콜백입니다.
    private Action<int> _slotSelected;

    // 슬롯 드롭 이동 입력 콜백입니다.
    private Action<int, int> _slotMoved;

    // 현재 선택된 슬롯 데이터입니다.
    private InventorySlotData _selectedSlotData;

    // 현재 슬롯 드래그 중인지 여부입니다.
    private bool _isDraggingSlot;

    // 드래그 중인 포인터의 화면 좌표입니다.
    private Vector2 _dragPointerScreenPosition;

    // 드래그 포인터 좌표 변환에 사용할 이벤트 카메라입니다.
    private Camera _dragEventCamera;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 드래그 중 ScrollRect 가장자리 자동 스크롤을 갱신합니다.
    /// </summary>
    private void Update()
    {
        UpdateDragAutoScroll();
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// Presenter가 처리할 슬롯 입력 콜백을 연결합니다.
    /// </summary>
    /// <param name="slotSelected">슬롯 선택 입력 콜백입니다.</param>
    /// <param name="slotMoved">슬롯 드롭 이동 입력 콜백입니다.</param>
    public void SetCallbacks(Action<int> slotSelected, Action<int, int> slotMoved)
    {
        _slotSelected = slotSelected;
        _slotMoved = slotMoved;
    }

    /// <summary>
    /// 인벤토리 UI를 열고 슬롯 페이지를 표시합니다.
    /// </summary>
    public void Show()
    {
        SetVisible(true);
        ShowSlotPage();
    }

    /// <summary>
    /// 인벤토리 UI를 숨깁니다.
    /// </summary>
    public void Hide()
    {
        _isDraggingSlot = false;
        SetVisible(false);
    }

    /// <summary>
    /// 표시할 슬롯 목록과 선택 상태를 갱신합니다.
    /// </summary>
    /// <param name="slots">표시할 슬롯 목록입니다.</param>
    /// <param name="items">각 슬롯에 대응하는 아이템 원본 데이터 목록입니다.</param>
    /// <param name="selectedSlotData">현재 선택된 슬롯 데이터입니다.</param>
    public void SetSlots(IReadOnlyList<InventorySlotData> slots, IReadOnlyList<ItemDataSO> items, InventorySlotData selectedSlotData)
    {
        _currentSlots.Clear();
        _currentItems.Clear();
        _selectedSlotData = selectedSlotData;

        if (slots != null)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                _currentSlots.Add(slots[i]);
                _currentItems.Add(items != null && i < items.Count ? items[i] : null);
            }
        }

        RebuildSlotViews();
    }

    /// <summary>
    /// 1페이지 슬롯 목록을 표시합니다.
    /// </summary>
    public void ShowSlotPage()
    {
        SetCanvasGroupVisible(_slotPageCanvasGroup, true);
        SetCanvasGroupVisible(_detailPageCanvasGroup, false);
    }

    /// <summary>
    /// 2페이지 선택 아이템 상세 정보를 표시합니다.
    /// </summary>
    public void ShowSelectedItemPage()
    {
        SetCanvasGroupVisible(_slotPageCanvasGroup, false);
        SetCanvasGroupVisible(_detailPageCanvasGroup, true);
    }

    /// <summary>
    /// 선택한 아이템 상세 정보를 UI에 출력합니다.
    /// </summary>
    /// <param name="itemData">출력할 아이템 원본 데이터입니다.</param>
    public void SetSelectedItem(ItemDataSO itemData)
    {
        if (itemData == null)
        {
            ClearSelectedItem();
            return;
        }

        if (_selectedItemIconImage != null)
        {
            _selectedItemIconImage.sprite = itemData.Icon;
            _selectedItemIconImage.enabled = itemData.Icon != null || _hideSelectedIconWhenEmpty == false;
        }

        if (_selectedItemNameText != null)
        {
            _selectedItemNameText.text = itemData.DisplayName;
        }

        if (_selectedItemPriceText != null)
        {
            _selectedItemPriceText.text = itemData.Price.ToString();
        }

        if (_selectedItemDescriptionText != null)
        {
            _selectedItemDescriptionText.text = itemData.Description;
        }
    }

    /// <summary>
    /// 선택 아이템 상세 UI를 빈 상태로 초기화합니다.
    /// </summary>
    public void ClearSelectedItem()
    {
        if (_selectedItemIconImage != null)
        {
            _selectedItemIconImage.sprite = null;
            _selectedItemIconImage.enabled = _hideSelectedIconWhenEmpty == false;
        }

        if (_selectedItemNameText != null)
        {
            _selectedItemNameText.text = string.Empty;
        }

        if (_selectedItemPriceText != null)
        {
            _selectedItemPriceText.text = string.Empty;
        }

        if (_selectedItemDescriptionText != null)
        {
            _selectedItemDescriptionText.text = string.Empty;
        }
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 인벤토리 UI 전체 표시 상태를 변경합니다.
    /// </summary>
    /// <param name="isVisible">표시 여부입니다.</param>
    private void SetVisible(bool isVisible)
    {
        if (_rootCanvasGroup != null)
        {
            _rootCanvasGroup.gameObject.SetActive(true);
            _rootCanvasGroup.alpha = isVisible ? 1.0f : 0.0f;
            _rootCanvasGroup.interactable = isVisible;
            _rootCanvasGroup.blocksRaycasts = isVisible;
            return;
        }

        gameObject.SetActive(isVisible);
    }

    /// <summary>
    /// 페이지 CanvasGroup 표시 상태를 변경합니다.
    /// </summary>
    /// <param name="canvasGroup">표시 상태를 변경할 CanvasGroup입니다.</param>
    /// <param name="isVisible">표시 여부입니다.</param>
    private void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool isVisible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = isVisible ? 1.0f : 0.0f;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;
    }

    /// <summary>
    /// 현재 슬롯 목록에 맞춰 슬롯 뷰를 재사용 갱신합니다.
    /// </summary>
    private void RebuildSlotViews()
    {
        int visibleSlotViewCount = GetRequiredSlotViewCount(_currentSlots.Count);
        EnsureSlotViews(visibleSlotViewCount);
        ApplyGridSettings(visibleSlotViewCount);

        for (int i = 0; i < _slotViews.Count; i++)
        {
            InventorySlotView slotView = _slotViews[i];

            if (slotView == null)
            {
                continue;
            }

            if (i >= visibleSlotViewCount)
            {
                slotView.Clear();
                continue;
            }

            InventorySlotData slotData = i < _currentSlots.Count ? _currentSlots[i] : null;
            ItemDataSO itemData = i < _currentItems.Count ? _currentItems[i] : null;
            bool isSelected = slotData != null && slotData == _selectedSlotData;

            slotView.Bind(
                i,
                slotData,
                itemData,
                isSelected,
                _allowSlotDragMove,
                HandleSlotClicked,
                HandleSlotDragStarted,
                HandleSlotDragged,
                HandleSlotDragEnded,
                HandleSlotDropped);
        }
    }

    /// <summary>
    /// 필요한 슬롯 뷰 수를 6열 행 단위로 계산합니다.
    /// </summary>
    /// <param name="slotCount">현재 실제 슬롯 수입니다.</param>
    /// <returns>표시해야 하는 슬롯 뷰 수입니다.</returns>
    private int GetRequiredSlotViewCount(int slotCount)
    {
        if (slotCount <= INVENTORY_VIEW_BASE_SLOT_COUNT)
        {
            return INVENTORY_VIEW_BASE_SLOT_COUNT;
        }

        int rowCount = Mathf.CeilToInt((float)slotCount / INVENTORY_VIEW_COLUMN_COUNT);
        return rowCount * INVENTORY_VIEW_COLUMN_COUNT;
    }

    /// <summary>
    /// 필요한 수만큼 슬롯 뷰를 생성하고 이후에는 재사용합니다.
    /// </summary>
    /// <param name="requiredCount">필요한 슬롯 뷰 수입니다.</param>
    private void EnsureSlotViews(int requiredCount)
    {
        if (_slotViewPrefab == null || _slotContentRoot == null)
        {
            return;
        }

        while (_slotViews.Count < requiredCount)
        {
            InventorySlotView createdSlotView = Instantiate(_slotViewPrefab, _slotContentRoot);
            _slotViews.Add(createdSlotView);
        }
    }

    /// <summary>
    /// 슬롯 6열 배치와 행 증가에 따른 콘텐츠 높이를 갱신합니다.
    /// </summary>
    /// <param name="visibleSlotViewCount">현재 표시할 슬롯 뷰 수입니다.</param>
    private void ApplyGridSettings(int visibleSlotViewCount)
    {
        if (_slotGridLayout == null)
        {
            return;
        }

        _slotGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _slotGridLayout.constraintCount = INVENTORY_VIEW_COLUMN_COUNT;

        if (_resizeSlotContentHeight == false || _slotContentRoot == null)
        {
            return;
        }

        int rowCount = Mathf.Max(INVENTORY_VIEW_BASE_ROW_COUNT, visibleSlotViewCount / INVENTORY_VIEW_COLUMN_COUNT);
        RectOffset padding = _slotGridLayout.padding;
        float rowHeight = _slotGridLayout.cellSize.y;
        float spacingHeight = _slotGridLayout.spacing.y;
        float contentHeight = padding.top + padding.bottom + rowHeight * rowCount + spacingHeight * Mathf.Max(0, rowCount - 1);

        _slotContentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
    }

    /// <summary>
    /// 슬롯 클릭 입력을 Presenter 콜백으로 전달합니다.
    /// </summary>
    /// <param name="slotIndex">클릭한 슬롯 인덱스입니다.</param>
    private void HandleSlotClicked(int slotIndex)
    {
        if (_slotSelected != null)
        {
            _slotSelected(slotIndex);
        }
    }

    /// <summary>
    /// 슬롯 드래그 시작 입력을 처리합니다.
    /// </summary>
    /// <param name="slotIndex">드래그를 시작한 슬롯 인덱스입니다.</param>
    /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
    private void HandleSlotDragStarted(int slotIndex, PointerEventData eventData)
    {
        _isDraggingSlot = true;
        UpdateDragPointer(eventData);
    }

    /// <summary>
    /// 슬롯 드래그 중 입력을 처리합니다.
    /// </summary>
    /// <param name="slotIndex">드래그 중인 슬롯 인덱스입니다.</param>
    /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
    private void HandleSlotDragged(int slotIndex, PointerEventData eventData)
    {
        UpdateDragPointer(eventData);
    }

    /// <summary>
    /// 슬롯 드래그 종료 입력을 처리합니다.
    /// </summary>
    /// <param name="slotIndex">드래그를 종료한 슬롯 인덱스입니다.</param>
    /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
    private void HandleSlotDragEnded(int slotIndex, PointerEventData eventData)
    {
        _isDraggingSlot = false;
        UpdateDragPointer(eventData);
    }

    /// <summary>
    /// 슬롯 드롭 입력을 Presenter 콜백으로 전달합니다.
    /// </summary>
    /// <param name="sourceSlotIndex">드래그한 슬롯 인덱스입니다.</param>
    /// <param name="targetSlotIndex">드롭한 슬롯 인덱스입니다.</param>
    private void HandleSlotDropped(int sourceSlotIndex, int targetSlotIndex)
    {
        if (_slotMoved != null)
        {
            _slotMoved(sourceSlotIndex, targetSlotIndex);
        }
    }

    /// <summary>
    /// 포인터 이벤트 데이터에서 드래그 좌표를 갱신합니다.
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
    private void UpdateDragPointer(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        _dragPointerScreenPosition = eventData.position;
        _dragEventCamera = eventData.pressEventCamera;
    }

    /// <summary>
    /// 드래그 중 ScrollRect 가장자리에서 자동 스크롤을 처리합니다.
    /// </summary>
    private void UpdateDragAutoScroll()
    {
        if (_isDraggingSlot == false || _slotScrollRect == null || _slotScrollRect.viewport == null || _slotScrollRect.content == null)
        {
            return;
        }

        RectTransform viewport = _slotScrollRect.viewport;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, _dragPointerScreenPosition, _dragEventCamera, out Vector2 localPoint) == false)
        {
            return;
        }

        float direction = GetAutoScrollDirection(viewport.rect, localPoint);

        if (Mathf.Approximately(direction, 0.0f))
        {
            return;
        }

        float scrollableHeight = _slotScrollRect.content.rect.height - viewport.rect.height;

        if (scrollableHeight <= 0.0f)
        {
            return;
        }

        float normalizedDelta = INVENTORY_VIEW_AUTO_SCROLL_SPEED * Time.unscaledDeltaTime / scrollableHeight;
        _slotScrollRect.verticalNormalizedPosition = Mathf.Clamp01(_slotScrollRect.verticalNormalizedPosition + direction * normalizedDelta);
    }

    /// <summary>
    /// 포인터 위치가 ScrollRect 가장자리 자동 스크롤 영역에 들어왔는지 확인합니다.
    /// </summary>
    /// <param name="viewportRect">ScrollRect viewport 영역입니다.</param>
    /// <param name="localPoint">viewport 기준 포인터 좌표입니다.</param>
    /// <returns>위쪽이면 1, 아래쪽이면 -1, 그 외에는 0입니다.</returns>
    private float GetAutoScrollDirection(Rect viewportRect, Vector2 localPoint)
    {
        if (localPoint.y > viewportRect.yMax - INVENTORY_VIEW_AUTO_SCROLL_EDGE_SIZE)
        {
            return 1.0f;
        }

        if (localPoint.y < viewportRect.yMin + INVENTORY_VIEW_AUTO_SCROLL_EDGE_SIZE)
        {
            return -1.0f;
        }

        return 0.0f;
    }
}
