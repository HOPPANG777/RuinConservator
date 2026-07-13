using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PipeComponent : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PipeData Data { get; private set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public bool IsWatered { get; set; } = false;

    private Image tileImage;
    private PipeManager ownerSpawner;
    private RectTransform rectTransform;

    private void Awake()
    {
        tileImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(PipeData tileData, int r, int c, PipeManager spawner)
    {
        Data = tileData;
        Row = r;
        Col = c;
        ownerSpawner = spawner;
        UpdateVisualRotation();
    }

    // 우클릭 분기 (인터페이스를 통해 독립 처리)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (ownerSpawner != null && ownerSpawner.IsClear) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Rotate();
        }
    }

    public void Rotate()
    {
        if (Data.type == PipeType.Empty || Data.type == PipeType.Start || Data.type == PipeType.End)
            return;

        PipeData updatedData = Data;
        updatedData.rotationIndex = (updatedData.rotationIndex + 1) % 4;
        Data = updatedData;

        UpdateVisualRotation();
        ownerSpawner.OnTileStateChanged();
    }

    // 좌클릭 슬라이딩 드래그 분기 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ownerSpawner != null && !ownerSpawner.IsClear && eventData.button == PointerEventData.InputButton.Left)
        {
            ownerSpawner.OnTileBeginDrag(this, eventData.position);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ownerSpawner != null && !ownerSpawner.IsClear && eventData.button == PointerEventData.InputButton.Left)
        {
            ownerSpawner.OnTileDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ownerSpawner != null && eventData.button == PointerEventData.InputButton.Left)
        {
            ownerSpawner.OnTileEndDrag();
        }
    }

    public void UpdateVisualColor()
    {
        if (tileImage == null) return;

        if (!IsWatered && Data.type != PipeType.End)
            tileImage.color = Color.white;
        else
            tileImage.color = Data.type == PipeType.Start ? Color.green : Color.cyan;
    }

    private void UpdateVisualRotation()
    {
        // 혹시 Awake 타이밍 문제로 rectTransform이 null이라면 여기서 직접 할당
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        // 예외 상황 방어
        if (rectTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] RectTransform 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        // UI 회전 처리
        rectTransform.localRotation = Quaternion.Euler(0, 0, -Data.rotationIndex * 90f);
    }
}