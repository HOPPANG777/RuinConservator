using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UiLightTileComponent : MonoBehaviour, IPointerClickHandler
{
    public int Id { get; private set; }
    public bool IsOn { get; private set; }

    private Image tileImage;
    private UiLightPuzzleManager ownerManager;

    public void Init(int tileId, bool initialState, UiLightPuzzleManager manager)
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

    // 유니티 UI 전용 클릭 이벤트 인터페이스 구현
    public void OnPointerClick(PointerEventData eventData)
    {
        // 좌클릭 시에만 작동하며, 이미 클리어된 상태면 조작 차단
        if (eventData.button == PointerEventData.InputButton.Left && ownerManager != null && !ownerManager.IsClear)
        {
            ownerManager.OnTileClicked(Id);
        }
    }

    private void UpdateVisual()
    {
        if (tileImage != null)
        {
            // 불이 켜지면 노란색, 꺼지면 어두운 회색
            tileImage.color = IsOn ? Color.yellow : new Color(0.2f, 0.2f, 0.2f);
        }
    }
}