using UnityEngine;
using System.Collections.Generic;

public class LightPuzzleManager : BasePuzzleManager
{
    [Header("Prefabs & Settings")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private LayerMask lightTileLayer;

    [Header("Text File Path Settings")]
    [SerializeField] private string stageFileName = "Stage01";

    private Dictionary<int, LightTileComponent> spawnedTiles = new Dictionary<int, LightTileComponent>();
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
            Debug.LogError($"[LightPuzzleManager] 텍스트 파일을 찾을 수 없습니다! 경로를 확인하세요: Resources/{fullPath}.txt");
            return;
        }

        currentRuntimeData = LightPuzzleParser.Parse(stageTextAsset.text);
        if (currentRuntimeData == null) return;

        int rows = currentRuntimeData.rows;
        int cols = currentRuntimeData.cols;
        Vector3 startPos = new Vector3(-(cols - 1) * itemSpacing / 2f, (rows - 1) * itemSpacing / 2f, 0);

        int dataIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (dataIndex >= currentRuntimeData.tileDataList.Count) break;

                LightTileRawData tileData = currentRuntimeData.tileDataList[dataIndex];

                Vector3 spawnPos = startPos + new Vector3(c * itemSpacing, -r * itemSpacing, 0);
                GameObject tileObj = Instantiate(tilePrefab, spawnPos, Quaternion.identity, transform);
                tileObj.name = $"LightTile_{tileData.id}";

                LightTileComponent lightTile = tileObj.GetComponent<LightTileComponent>();
                lightTile.Init(tileData.id, tileData.initialState == 1);

                spawnedTiles.Add(tileData.id, lightTile);
                dataIndex++;
            }
        }

        Debug.Log($"[LightPuzzleManager] 퍼즐 파일('{stageFileName}.txt') 로드 및 배치 완료: {spawnedTiles.Count}개 타일 세팅됨.");
    }

    private void Update()
    {
        if (IsClear) return;

        if (PuzzleInputUtil.IsLeftButtonTriggered() && PuzzleInputUtil.TryGetMouseWorldPosition(out Vector2 mousePos))
        {
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, lightTileLayer);
            if (hit.collider != null)
            {
                LightTileComponent clickedTile = hit.collider.GetComponent<LightTileComponent>();
                if (clickedTile != null) OnTileClicked(clickedTile.Id);
            }
        }
    }

    private void OnTileClicked(int clickedId)
    {
        if (currentRuntimeData == null) return;

        LightTileRawData clickedData = currentRuntimeData.tileDataList.Find(x => x.id == clickedId);
        if (clickedData == null) return;

        foreach (int targetId in clickedData.targetIds)
        {
            if (spawnedTiles.TryGetValue(targetId, out LightTileComponent targetTile))
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
        // 이벤트
    }
}