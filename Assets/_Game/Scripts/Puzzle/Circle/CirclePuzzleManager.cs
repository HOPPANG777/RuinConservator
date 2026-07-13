using UnityEngine;
using System.Collections.Generic;

public class UiCirclePuzzleManager : BasePuzzleManager
{
    [Header("UI Grid Parent Settings")]
    [SerializeField] private RectTransform gridParent;

    [Header("Single Base UI Prefab")]
    [SerializeField] private GameObject singleRingPrefab;

    [Header("Circle Pattern")]
    [SerializeField] private List<GameObject> ringPrefabs = new List<GameObject>();

    [Header("Text File Path Settings")]
    [SerializeField] private string stageFileName = "Stage01";

    [Header("Size Settings")]
    [SerializeField] private float startScale = 1.0f;
    [SerializeField] private float scaleStep = 0.4f;

    private List<UiCircleComponent> spawnedRings = new List<UiCircleComponent>();

    public override void InitializePuzzle()
    {
        LoadAndGeneratePuzzle();
    }

    private void LoadAndGeneratePuzzle()
    {
        foreach (var ring in spawnedRings) if (ring != null) Destroy(ring.gameObject);
        spawnedRings.Clear();
        IsClear = false;

        string fullPath = $"PuzzleData/Circle/{stageFileName}";
        TextAsset stageTextAsset = Resources.Load<TextAsset>(fullPath);

        if (stageTextAsset == null)
        {
            Debug.LogError($"파일을 찾을 수 없습니다: Resources/{fullPath}.txt");
            return;
        }

        List<CirclePuzzleData> ringDataList = CirclePuzzleParser.Parse(stageTextAsset.text);
        if (ringDataList == null) return;

        for (int i = 0; i < ringDataList.Count; i++)
        {
            CirclePuzzleData data = ringDataList[i];

            if (data.prefabIndex < 0 || data.prefabIndex >= ringPrefabs.Count)
            {
                Debug.LogWarning($"프리팹 인덱스 범위 초과: {data.prefabIndex}");
                continue;
            }

            GameObject patternPrefab = ringPrefabs[data.prefabIndex];
            if (singleRingPrefab == null || gridParent == null) continue;

            GameObject ringObj = Instantiate(singleRingPrefab, gridParent);
            ringObj.name = $"Ui_Circle_Ring_[Level_{i}]";

            RectTransform ringRect = ringObj.GetComponent<RectTransform>();
            if (ringRect != null)
            {
                float currentScale = startScale + (i * scaleStep);
                ringRect.localScale = new Vector3(currentScale, currentScale, 1f);
            }

            UiCircleComponent ringComp = ringObj.GetComponent<UiCircleComponent>() ?? ringObj.AddComponent<UiCircleComponent>();

            ringComp.InitRingState(data.segments, patternPrefab, this);

            ringObj.transform.SetAsFirstSibling();

            spawnedRings.Add(ringComp);
        }
    }

    public override void CheckClearCondition()
    {
        if (spawnedRings == null || spawnedRings.Count == 0) return;

        foreach (var ring in spawnedRings)
        {
            if (!ring.IsAligned()) return;
        }

        IsClear = true;
        OnPuzzleClear();
    }

    protected override void OnPuzzleClear()
    {
        TriggerClearEvent();
    }
}