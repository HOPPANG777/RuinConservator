using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class PipePuzzleManager : BasePuzzleManager
{
    [Header("Text File Path Settings")]
    [SerializeField] private string stageFileName = "Stage01";

    [Header("UI Grid Parent Settings")]
    [SerializeField] private RectTransform gridParent;

    [Header("UI Drag Settings")]
    [SerializeField] private float moveThreshold = 20f;

    [Header("UI Tile Prefabs)")]
    [SerializeField] private GameObject emptyTilePrefab;
    [SerializeField] private GameObject startTilePrefab;
    [SerializeField] private GameObject endTilePrefab;
    [SerializeField] private GameObject pipeStraightPrefab;
    [SerializeField] private GameObject pipeCornerPrefab;
    [SerializeField] private GameObject pipeTPrefab;
    [SerializeField] private GameObject pipeCrossPrefab;

    private int rows;
    private int cols;
    private PipePuzzleComponent[,] grid;

    // 드래그 조작 내부 UI 변수들
    private PipePuzzleComponent selectedTile;
    private PipePuzzleComponent targetEmptyTile;
    private Vector3 originalLocalPos;
    private Vector3 emptyTileLocalPos;
    private bool isDragging = false;
    private bool isDirectionSet = false;
    private Vector2 dragStartScreenPos;

    public override void InitPuzzle()
    {
        LoadAndGeneratePuzzle();
    }

    private void LoadAndGeneratePuzzle()
    {
        if (grid != null)
        {
            foreach (var tile in grid) if (tile != null) Destroy(tile.gameObject);
        }

        string finalFileName = stageFileName;
        if (!finalFileName.EndsWith(".txt")) finalFileName += ".txt";

        string fullPath = Path.Combine(Application.dataPath, "Resources", "PuzzleData", "Pipe", finalFileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"경로: {fullPath}");
            return;
        }

        string rawStageText = File.ReadAllText(fullPath);
        PipePuzzleStageData parsedData = PipePuzzleParser.Parse(rawStageText);
        if (parsedData == null) return;

        rows = parsedData.rows;
        cols = parsedData.cols;
        grid = new PipePuzzleComponent[rows, cols];

        Vector3 startLocalPos = new Vector3(-(cols - 1) * itemSpacing / 2f, (rows - 1) * itemSpacing / 2f, 0);

        int dataIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (dataIndex >= parsedData.tileDataList.Count) break;

                PipePuzzleTileData rawData = parsedData.tileDataList[dataIndex];

                PipePuzzleData tileData = new PipePuzzleData();
                tileData.rotationIndex = rawData.rotationIndex;
                tileData.type = rawData.typeStr switch
                {
                    "0" => PipePuzzleType.Empty,
                    "S" => PipePuzzleType.Start,
                    "E" => PipePuzzleType.End,
                    "1S" => PipePuzzleType.Pipe_1_Straight,
                    "1C" => PipePuzzleType.Pipe_1_Corner,
                    "2" => PipePuzzleType.Pipe_2_T,
                    "3" => PipePuzzleType.Pipe_3_Cross,
                    _ => PipePuzzleType.Empty
                };

                GameObject prefab = GetTilePrefab(tileData.type);
                if (prefab != null && gridParent != null)
                {
                    GameObject spawnedObj = Instantiate(prefab, gridParent);
                    spawnedObj.name = $"UI_Tile_[{r},{c}]_{tileData.type}";

                    RectTransform tileRect = spawnedObj.GetComponent<RectTransform>();
                    Vector3 spawnLocalPos = startLocalPos + new Vector3(c * itemSpacing, -r * itemSpacing, 0);
                    tileRect.localPosition = spawnLocalPos;

                    PipePuzzleComponent tileComp = spawnedObj.GetComponent<PipePuzzleComponent>() ?? spawnedObj.AddComponent<PipePuzzleComponent>();
                    tileComp.Setup(tileData, r, c, this);
                    grid[r, c] = tileComp;
                }
                dataIndex++;
            }
        }

        IsClear = false;
        ResetDragState();
        UpdateWaterFlow();
    }

    public void OnTileBeginDrag(PipePuzzleComponent tile, Vector2 screenPos)
    {
        if (tile.Data.type == PipePuzzleType.Empty || tile.Data.type == PipePuzzleType.Start || tile.Data.type == PipePuzzleType.End) return;

        selectedTile = tile;
        originalLocalPos = tile.GetComponent<RectTransform>().localPosition;
        dragStartScreenPos = screenPos;
        isDragging = true;
        isDirectionSet = false;
        targetEmptyTile = null;
    }

    public void OnTileDrag(Vector2 currentScreenPos)
    {
        if (!isDragging || selectedTile == null) return;

        if (!isDirectionSet)
        {
            Vector2 mouseDelta = currentScreenPos - dragStartScreenPos;
            if (mouseDelta.magnitude > moveThreshold)
            {
                int checkRow = selectedTile.Row;
                int checkCol = selectedTile.Col;

                if (Mathf.Abs(mouseDelta.x) > Mathf.Abs(mouseDelta.y))
                    checkCol += (mouseDelta.x > 0) ? 1 : -1;
                else
                    checkRow += (mouseDelta.y > 0) ? -1 : 1;

                PipePuzzleComponent neighbor = GetSpecificTile(checkRow, checkCol);
                if (neighbor != null && neighbor.Data.type == PipePuzzleType.Empty)
                {
                    targetEmptyTile = neighbor;
                    emptyTileLocalPos = targetEmptyTile.GetComponent<RectTransform>().localPosition;
                    isDirectionSet = true;
                }
                else
                {
                    isDragging = false;
                    selectedTile = null;
                }
            }
        }
        else if (targetEmptyTile != null)
        {
            Vector3 dragAxis = emptyTileLocalPos - originalLocalPos;
            float maxDistance = dragAxis.magnitude;
            Vector3 moveDir = dragAxis.normalized;

            Vector3 mouseMoveVector = (Vector3)(currentScreenPos - dragStartScreenPos);
            float dragProgress = Vector3.Dot(mouseMoveVector, moveDir);

            dragProgress = Mathf.Clamp(dragProgress, 0f, maxDistance);
            selectedTile.GetComponent<RectTransform>().localPosition = originalLocalPos + moveDir * dragProgress;
        }
    }

    public void OnTileEndDrag()
    {
        if (!isDragging || selectedTile == null) return;

        isDragging = false;
        RectTransform selectedRect = selectedTile.GetComponent<RectTransform>();

        if (isDirectionSet && targetEmptyTile != null)
        {
            float currentDist = Vector3.Distance(selectedRect.localPosition, originalLocalPos);
            float totalDist = Vector3.Distance(originalLocalPos, emptyTileLocalPos);

            if (currentDist > totalDist * 0.4f)
            {
                ExecuteGridSwap(selectedTile, targetEmptyTile);
            }
            else
            {
                selectedRect.localPosition = originalLocalPos;
            }
        }
        else
        {
            selectedRect.localPosition = originalLocalPos;
        }

        ResetDragState();
    }

    private void ResetDragState()
    {
        selectedTile = null;
        targetEmptyTile = null;
        isDragging = false;
        isDirectionSet = false;
    }

    public void OnTileStateChanged()
    {
        UpdateWaterFlow();
    }

    public void ExecuteGridSwap(PipePuzzleComponent draggedTile, PipePuzzleComponent emptyTile)
    {
        int dRow = draggedTile.Row, dCol = draggedTile.Col;
        int eRow = emptyTile.Row, eCol = emptyTile.Col;

        grid[dRow, dCol] = emptyTile;
        grid[eRow, eCol] = draggedTile;

        draggedTile.Row = eRow; draggedTile.Col = eCol;
        emptyTile.Row = dRow; emptyTile.Col = dCol;

        draggedTile.GetComponent<RectTransform>().localPosition = GetGridLocalPosition(eRow, eCol);
        emptyTile.GetComponent<RectTransform>().localPosition = GetGridLocalPosition(dRow, dCol);

        UpdateWaterFlow();
    }

    private void UpdateWaterFlow()
    {
        if (grid == null) return;

        PipePuzzleComponent startTile = null;
        foreach (var tile in grid)
        {
            if (tile == null) continue;
            tile.IsWatered = false;
            if (tile.Data.type == PipePuzzleType.Start) startTile = tile;
        }

        if (startTile == null) return;

        Queue<PipePuzzleComponent> queue = new Queue<PipePuzzleComponent>();
        HashSet<PipePuzzleComponent> visited = new HashSet<PipePuzzleComponent>();

        queue.Enqueue(startTile);
        visited.Add(startTile);

        int[] dRow = { -1, 1, 0, 0 };
        int[] dCol = { 0, 0, -1, 1 };
        bool stageClearDetected = false;

        while (queue.Count > 0)
        {
            PipePuzzleComponent current = queue.Dequeue();
            current.IsWatered = true;

            if (current.Data.type == PipePuzzleType.End) stageClearDetected = true;

            for (int i = 0; i < 4; i++)
            {
                int nRow = current.Row + dRow[i];
                int nCol = current.Col + dCol[i];

                if (nRow >= 0 && nRow < rows && nCol >= 0 && nCol < cols)
                {
                    PipePuzzleComponent nextTile = grid[nRow, nCol];
                    if (nextTile != null && !visited.Contains(nextTile) && IsConnected(current, nextTile))
                    {
                        visited.Add(nextTile);
                        queue.Enqueue(nextTile);
                    }
                }
            }
        }

        foreach (var tile in grid) if (tile != null) tile.UpdateVisualColor();

        if (stageClearDetected && !IsClear)
        {
            CheckClear();
        }
    }

    public override void CheckClear()
    {
        IsClear = true;
        OnPuzzleClear();
    }

    protected override void OnPuzzleClear()
    {
        TriggerClearEvent();
    }

    public PipePuzzleComponent GetSpecificTile(int row, int col)
    {
        if (row >= 0 && row < rows && col >= 0 && col < cols) return grid[row, col];
        return null;
    }

    public Vector3 GetGridLocalPosition(int r, int c)
    {
        Vector3 startLocalPos = new Vector3(-(cols - 1) * itemSpacing / 2f, (rows - 1) * itemSpacing / 2f, 0);
        return startLocalPos + new Vector3(c * itemSpacing, -r * itemSpacing, 0);
    }

    private GameObject GetTilePrefab(PipePuzzleType type) => type switch
    {
        PipePuzzleType.Empty => emptyTilePrefab,
        PipePuzzleType.Start => startTilePrefab,
        PipePuzzleType.End => endTilePrefab,
        PipePuzzleType.Pipe_1_Straight => pipeStraightPrefab,
        PipePuzzleType.Pipe_1_Corner => pipeCornerPrefab,
        PipePuzzleType.Pipe_2_T => pipeTPrefab,
        PipePuzzleType.Pipe_3_Cross => pipeCrossPrefab,
        _ => null
    };

    private bool IsConnected(PipePuzzleComponent from, PipePuzzleComponent to)
    {
        int rowDiff = to.Row - from.Row;
        int colDiff = to.Col - from.Col;

        PipePuzzleDir fromDir = from.Data.GetCurrentDirections();
        PipePuzzleDir toDir = to.Data.GetCurrentDirections();

        if (rowDiff == 0)
        {
            if (colDiff == 1) return (fromDir & PipePuzzleDir.Right) != 0 && (toDir & PipePuzzleDir.Left) != 0;
            if (colDiff == -1) return (fromDir & PipePuzzleDir.Left) != 0 && (toDir & PipePuzzleDir.Right) != 0;
        }
        else if (colDiff == 0)
        {
            if (rowDiff == 1) return (fromDir & PipePuzzleDir.Bottom) != 0 && (toDir & PipePuzzleDir.Top) != 0;
            if (rowDiff == -1) return (fromDir & PipePuzzleDir.Top) != 0 && (toDir & PipePuzzleDir.Bottom) != 0;
        }
        return false;
    }
}