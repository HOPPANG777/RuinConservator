using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

public class UiPuzzlePopup : MonoBehaviour
{
    [Header("UI Windows")]
    [SerializeField] private GameObject popupContainer; // 팝업 전체 UI
    [SerializeField] private Button closeButton;        // 닫기 버튼

    [Header("Puzzle Managers Inside UI")]
    [SerializeField] private BasePuzzleManager uiPuzzleManager;

    [Header("Clear Settings")]
    [SerializeField] private float closeDelaySeconds = 3.0f;

    private Coroutine closeDelayCoroutine;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);

        // 클리어 이벤트 발생 시 바로 닫지 않고 지연 함수 호출
        if (uiPuzzleManager != null)
        {
            uiPuzzleManager.OnPuzzleClearedEvent += OnPuzzleCleared;
        }

        ClosePopup();
    }

    private void OnDestroy()
    {
        if (uiPuzzleManager != null)
        {
            uiPuzzleManager.OnPuzzleClearedEvent -= OnPuzzleCleared;
        }
    }

    public void OpenPuzzlePopup()
    {
        // 새로운 오픈 시 혹시 돌고 있을지 모를 코루틴 초기화
        if (closeDelayCoroutine != null)
        {
            StopCoroutine(closeDelayCoroutine);
            closeDelayCoroutine = null;
        }

        if (popupContainer != null)
            popupContainer.SetActive(true);

        if (uiPuzzleManager != null)
        {
            uiPuzzleManager.enabled = true;
            uiPuzzleManager.InitializePuzzle();
        }
    }

    // 수동으로 닫기 버튼을 누르거나 강제 종료할 때
    public void ClosePopup()
    {
        if (closeDelayCoroutine != null)
        {
            StopCoroutine(closeDelayCoroutine);
            closeDelayCoroutine = null;
        }

        if (popupContainer != null)
            popupContainer.SetActive(false);

        if (uiPuzzleManager != null)
        {
            uiPuzzleManager.enabled = false;
        }
    }

    // 클리어 이벤트를 전달받는 함수
    private void OnPuzzleCleared()
    {
        // 퍼즐 로직만 먼저 정지
        if (uiPuzzleManager != null)
        {
            uiPuzzleManager.enabled = false;
        }

        // 3초 지연 후 닫히는 코루틴 시작
        if (gameObject.activeInHierarchy)
        {
            closeDelayCoroutine = StartCoroutine(Co_DelayedClose());
        }
    }

    private IEnumerator Co_DelayedClose()
    {
        // 설정한 시간 만큼 대기
        yield return new WaitForSeconds(closeDelaySeconds);

        // 팝업 컨테이너만 비활성화
        if (popupContainer != null)
            popupContainer.SetActive(false);

        closeDelayCoroutine = null;
        Debug.Log($"[UiPuzzlePopup] 퍼즐 클리어 후 {closeDelaySeconds}초가 지나 팝업이 자동으로 닫혔습니다.");
    }
}