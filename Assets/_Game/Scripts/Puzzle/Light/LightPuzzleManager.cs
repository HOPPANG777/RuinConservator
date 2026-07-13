using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UiLightPuzzleManager : BasePuzzleManager
{
    [Header("UI Grid Parent Settings")]
    [Tooltip("UI 타일들이 생성되어 배치될 부모 Panel(RectTransform)을 연결하세요.")]
    [SerializeField] private RectTransform gridParent;

    [Header("Prefabs & Settings")]
    [Tooltip("Image와 UiLightTileComponent가 붙어있는 UI 프리팹을 연결하세요.")]
    [SerializeField] private GameObject tilePrefab;

    [Header("Text File Path Settings")]
    [SerializeField] private string stageFileName = "Stage01";

    private Dictionary<int, UiLightTileComponent> spawnedTiles = new Dictionary<int, UiLightTileComponent>();
    private LightStageParsedData currentRuntimeData;

    public override void InitializePuzzle()
    {
        GenerateLightPuzzle();
    }

    private void GenerateLightPuzzle()
    {
        foreach (var tile in spawnedTiles.Values) if (tile != null) Destroy(tile.gameObject);
        spawnedTiles.Clear();
        IsClear = false;

        string fullPath = $"PuzzleData/Light/{stageFileName}";
        TextAsset stageTextAsset = Resources.Load<TextAsset>(fullPath);

        if (stageTextAsset == null)
        {
            Debug.LogError($"[UiLightPuzzleManager] 텍스트 파일을 찾을 수 없습니다! 경로: Resources/{fullPath}.txt");
            return;
        }

        currentRuntimeData = LightPuzzleParser.Parse(stageTextAsset.text);
        if (currentRuntimeData == null) return;

        int rows = currentRuntimeData.rows;
        int cols = currentRuntimeData.cols;

        // UI 캔버스 스페이스 기준 정중앙 정렬 좌표 연산
        Vector3 startLocalPos = new Vector3(-(cols - 1) * itemSpacing / 2f, (rows - 1) * itemSpacing / 2f, 0);

        int dataIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (dataIndex >= currentRuntimeData.tileDataList.Count) break;

                LightTileRawData tileData = currentRuntimeData.tileDataList[dataIndex];

                if (tilePrefab != null && gridParent != null)
                {
                    GameObject tileObj = Instantiate(tilePrefab, gridParent);
                    tileObj.name = $"UI_LightTile_{tileData.id}";

                    // RectTransform 좌표 설정
                    RectTransform tileRect = tileObj.GetComponent<RectTransform>();
                    tileRect.localPosition = startLocalPos + new Vector3(c * itemSpacing, -r * itemSpacing, 0);

                    UiLightTileComponent lightTile = tileObj.GetComponent<UiLightTileComponent>() ?? tileObj.AddComponent<UiLightTileComponent>();
                    lightTile.Init(tileData.id, tileData.initialState == 1, this);

                    spawnedTiles.Add(tileData.id, lightTile);
                }
                dataIndex++;
            }
        }

        Debug.Log($"[UiLightPuzzleManager] UI 퍼즐 파일('{stageFileName}.txt') 배치 완료: {spawnedTiles.Count}개 타일 세팅됨.");
    }

    // 코드가 직접 마우스를 감지하는 Update() 레이캐스트 연산 전면 제거 (타일이 직접 이벤트를 보내옴)

    public void OnTileClicked(int clickedId)
    {
        if (currentRuntimeData == null || IsClear) return;

        LightTileRawData clickedData = currentRuntimeData.tileDataList.Find(x => x.id == clickedId);
        if (clickedData == null) return;

        // 클릭된 타일과 연동된 모든 타일 토글 연산
        foreach (int targetId in clickedData.targetIds)
        {
            if (spawnedTiles.TryGetValue(targetId, out UiLightTileComponent targetTile))
            {
                targetTile.ToggleState();
            }
        }

        CheckClearCondition();
    }

    public override void CheckClearCondition()
    {
        foreach (var tile in spawnedTiles.Values)
        {
            if (!tile.IsOn) return;
        }

        IsClear = true;
        OnPuzzleClear();
    }

    protected override void OnPuzzleClear()
    {
        Debug.Log("🎯 [UI 라이트 퍼즐 클리어] 모든 불이 켜졌습니다!");

        // 부모 UiPuzzlePopup에게 알림 -> 3초 뒤에 자동으로 팝업이 닫힙니다.
        TriggerClearEvent();
    }
}