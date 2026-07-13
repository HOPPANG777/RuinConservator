using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PipePuzzleComponent : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PipePuzzleData Data { get; private set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public bool IsWatered { get; set; } = false;

    private Image tileImage;
    private PipePuzzleManager ownerSpawner;
    private RectTransform rectTransform;

    private void Awake()
    {
        tileImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(PipePuzzleData tileData, int r, int c, PipePuzzleManager spawner)
    {
        Data = tileData;
        Row = r;
        Col = c;
        ownerSpawner = spawner;
        UpdateVisualRotation();
    }

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
        if (Data.type == PipePuzzleType.Empty || Data.type == PipePuzzleType.Start || Data.type == PipePuzzleType.End)
            return;

        PipePuzzleData updatedData = Data;
        updatedData.rotationIndex = (updatedData.rotationIndex + 1) % 4;
        Data = updatedData;

        UpdateVisualRotation();
        ownerSpawner.OnTileStateChanged();
    }

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

        if (!IsWatered && Data.type != PipePuzzleType.End)
            tileImage.color = Color.white;
        else
            tileImage.color = Data.type == PipePuzzleType.Start ? Color.green : Color.cyan;
    }

    private void UpdateVisualRotation()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        rectTransform.localRotation = Quaternion.Euler(0, 0, -Data.rotationIndex * 90f);
    }
}