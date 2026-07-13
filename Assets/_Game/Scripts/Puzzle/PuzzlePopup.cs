using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

// 퍼즐 UI 열림, 닿힘
public class PuzzlePopup : MonoBehaviour
{
    [Header("UI Windows")]
    [SerializeField] private GameObject popupContainer; 
    [SerializeField] private Button closeBtn;      

    [Header("Puzzle Managers Inside UI")]
    [SerializeField] private BasePuzzleManager puzzleManager;

    [Header("Clear Settings")]
    [SerializeField] private float closeDelay = 1.0f;

    private Coroutine closeDelayCoroutine;

    private void Awake()
    {
        if (closeBtn != null)
            closeBtn.onClick.AddListener(ClosePopup);

        if (puzzleManager != null)
            puzzleManager.OnPuzzleCleared += OnPuzzleCleared;

        ClosePopup();
    }

    private void OnDestroy()
    {
        if (puzzleManager != null)
            puzzleManager.OnPuzzleCleared -= OnPuzzleCleared;
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

        if (puzzleManager != null)
        {
            puzzleManager.enabled = true;
            puzzleManager.InitPuzzle();
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

        if (puzzleManager != null)
        {
            puzzleManager.enabled = false;
        }
    }

    private void OnPuzzleCleared()
    {
        if (puzzleManager != null)
        {
            puzzleManager.enabled = false;
        }

        if (gameObject.activeInHierarchy)
        {
            closeDelayCoroutine = StartCoroutine(Co_DelayedClose());
        }
    }

    private IEnumerator Co_DelayedClose()
    {
        yield return new WaitForSeconds(closeDelay);

        if (popupContainer != null)
            popupContainer.SetActive(false);

        closeDelayCoroutine = null;
    }
}