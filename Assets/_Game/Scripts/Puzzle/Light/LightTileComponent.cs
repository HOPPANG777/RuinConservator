using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LightPuzzleComponent : MonoBehaviour, IPointerClickHandler
{
    public int Id { get; private set; }
    public bool IsOn { get; private set; }

    private Image tileImage;
    private LightPuzzleManager ownerManager;

    public void Init(int tileId, bool initialState, LightPuzzleManager manager)
    {
        Id = tileId;
        IsOn = initialState;
        ownerManager = manager;
        tileImage = GetComponent<Image>();
        UpdateVisual();
    }

    public void ToggleState()
    {
        IsOn = !IsOn;
        UpdateVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && ownerManager != null && !ownerManager.IsClear)
        {
            ownerManager.OnTileClicked(Id);
        }
    }

    private void UpdateVisual()
    {
        if (tileImage != null)
        {
            tileImage.color = IsOn ? Color.yellow : new Color(0.2f, 0.2f, 0.2f);
        }
    }
}