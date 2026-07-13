using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

public class UiPuzzlePopup : MonoBehaviour
{
    [Header("UI Windows")]
    [SerializeField] private GameObject popupContainer; 
    [SerializeField] private Button closeButton;      

    [Header("Puzzle Managers Inside UI")]
    [SerializeField] private BasePuzzleManager uiPuzzleManager;

    [Header("Clear Settings")]
    [SerializeField] private float closeDelaySeconds = 1.0f;

    private Coroutine closeDelayCoroutine;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);

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

    private void OnPuzzleCleared()
    {
        if (uiPuzzleManager != null)
        {
            uiPuzzleManager.enabled = false;
        }

        if (gameObject.activeInHierarchy)
        {
            closeDelayCoroutine = StartCoroutine(Co_DelayedClose());
        }
    }

    private IEnumerator Co_DelayedClose()
    {
        yield return new WaitForSeconds(closeDelaySeconds);

        if (popupContainer != null)
            popupContainer.SetActive(false);

        closeDelayCoroutine = null;
    }
}