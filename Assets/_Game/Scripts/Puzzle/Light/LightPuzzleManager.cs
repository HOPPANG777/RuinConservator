using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LightPuzzleManager : BasePuzzleManager
{
    [Header("UI Grid Parent Settings")]
    [SerializeField] private RectTransform gridParent;

    [Header("Prefabs & Settings")]
    [SerializeField] private GameObject tilePrefab;

    [Header("Text File Path Settings")]
    [SerializeField] private string stageFileName = "Stage01";

    private Dictionary<int, LightPuzzleComponent> spawnedTiles = new Dictionary<int, LightPuzzleComponent>();
    private LightPuzzleStageData currentRuntimeData;

    public override void InitPuzzle()
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
            Debug.LogError($"텍스트 파일을 찾을 수 없습니다 경로: Resources/{fullPath}.txt");
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

                LightPuzzleData tileData = currentRuntimeData.tileDataList[dataIndex];

                if (tilePrefab != null && gridParent != null)
                {
                    GameObject tileObj = Instantiate(tilePrefab, gridParent);
                    tileObj.name = $"UI_LightTile_{tileData.id}";

                    RectTransform tileRect = tileObj.GetComponent<RectTransform>();
                    tileRect.localPosition = startLocalPos + new Vector3(c * itemSpacing, -r * itemSpacing, 0);

                    LightPuzzleComponent lightTile = tileObj.GetComponent<LightPuzzleComponent>() ?? tileObj.AddComponent<LightPuzzleComponent>();
                    lightTile.Init(tileData.id, tileData.initialState == 1, this);

                    spawnedTiles.Add(tileData.id, lightTile);
                }
                dataIndex++;
            }
        }
    }

    public void OnTileClicked(int clickedId)
    {
        if (currentRuntimeData == null || IsClear) return;

        LightPuzzleData clickedData = currentRuntimeData.tileDataList.Find(x => x.id == clickedId);
        if (clickedData == null) return;

        foreach (int targetId in clickedData.targetIds)
        {
            if (spawnedTiles.TryGetValue(targetId, out LightPuzzleComponent targetTile))
            {
                targetTile.ToggleState();
            }
        }

        CheckClear();
    }

    public override void CheckClear()
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
        TriggerClearEvent();
    }
}