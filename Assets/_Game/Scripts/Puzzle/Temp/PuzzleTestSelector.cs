using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzleTestSelector : MonoBehaviour
{
    [System.Serializable]
    public struct PuzzleUiEntry
    {
        public string puzzleName;
        public UiPuzzlePopup puzzlePopup;
    }

    [Header("테스트할 UI 팝업 퍼즐 리스트")]
    [SerializeField] private PuzzleUiEntry[] puzzles;

    [Header("시작 시 활성화할 퍼즐 인덱스")]
    [SerializeField] private int startIndex = -1;

    [Header("클리어 시 행동 설정")]
    [SerializeField] private bool hideOnClear = false;

    private int currentIndex = -1;
    private bool isCurrentPuzzleClearedProcessed = false;
    private BasePuzzleManager currentActiveManager;

    private void Start()
    {
        for (int i = 0; i < puzzles.Length; i++)
        {
            if (puzzles[i].puzzlePopup != null)
            {
                puzzles[i].puzzlePopup.ClosePopup();
            }
        }

        if (puzzles.Length > 0 && startIndex >= 0 && startIndex < puzzles.Length)
        {
            SelectPuzzle(startIndex);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // 1번 키: 파이프 퍼즐 (배열의 0번째 항목)
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) SelectPuzzle(0);

        // 2번 키: 라이트 퍼즐 (배열의 1번째 항목)
        if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) SelectPuzzle(1);

        // --- [핵심 추가] 3번 키: 서클(원) 퍼즐 (배열의 2번째 항목) ---
        if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) SelectPuzzle(2);

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetCurrentPuzzle();
        }

        if (currentIndex != -1 && !isCurrentPuzzleClearedProcessed && currentActiveManager != null)
        {
            if (currentActiveManager.IsClear)
            {
                HandlePuzzleClearAutomation();
            }
        }
    }

    public void SelectPuzzle(int index)
    {
        if (index < 0 || index >= puzzles.Length) return;

        var targetPopup = puzzles[index].puzzlePopup;
        if (targetPopup == null) return;

        if (currentIndex == index)
        {
            targetPopup.ClosePopup();
            currentIndex = -1;
            currentActiveManager = null;
            return;
        }

        if (currentIndex != -1 && puzzles[currentIndex].puzzlePopup != null)
        {
            puzzles[currentIndex].puzzlePopup.ClosePopup();
        }

        currentIndex = index;
        isCurrentPuzzleClearedProcessed = false;

        targetPopup.OpenPuzzlePopup();

        currentActiveManager = targetPopup.GetComponentInChildren<BasePuzzleManager>();
        if (currentActiveManager != null)
        {
            currentActiveManager.enabled = true;
        }
    }

    private void ResetCurrentPuzzle()
    {
        if (currentIndex != -1 && puzzles[currentIndex].puzzlePopup != null)
        {
            isCurrentPuzzleClearedProcessed = false;
            puzzles[currentIndex].puzzlePopup.OpenPuzzlePopup();

            if (currentActiveManager != null)
            {
                currentActiveManager.enabled = true;
            }
        }
    }

    private void HandlePuzzleClearAutomation()
    {
        isCurrentPuzzleClearedProcessed = true;
        var entry = puzzles[currentIndex];

        if (hideOnClear)
        {
            if (entry.puzzlePopup != null) entry.puzzlePopup.ClosePopup();
        }
        else
        {
            if (currentActiveManager != null) currentActiveManager.enabled = false;
        }
    }
}