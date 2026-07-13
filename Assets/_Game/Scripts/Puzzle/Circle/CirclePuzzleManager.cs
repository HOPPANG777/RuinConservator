using UnityEngine;
using System.Collections.Generic;

public class UiCirclePuzzleManager : BasePuzzleManager
{
    [Header("UI Grid Parent Settings")]
    [SerializeField] private RectTransform gridParent;

    [Header("Single Base UI Prefab")]
    [SerializeField] private GameObject singleRingPrefab;

    [Header("Ring Prefabs Pool (문양 프리팹 리스트)")]
    [SerializeField] private List<GameObject> ringPrefabs = new List<GameObject>();

    [Header("Text File Path Settings")]
    [SerializeField] private string stageFileName = "Stage01";

    [Header("Size Settings (원 크기 계단식 확장용)")]
    [Tooltip("가장 안쪽(첫 번째) 원의 시작 스케일 배율")]
    [SerializeField] private float startScale = 1.0f;
    [Tooltip("바깥쪽 원으로 갈수록 추가될 스케일 가중치")]
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
            Debug.LogError($"[UiCirclePuzzleManager] 파일을 찾을 수 없습니다: Resources/{fullPath}.txt");
            return;
        }

        List<CirclePuzzleData> ringDataList = CirclePuzzleParser.Parse(stageTextAsset.text);
        if (ringDataList == null) return;

        // 작은 원부터 차례대로 큰 원 순서로 생성
        for (int i = 0; i < ringDataList.Count; i++)
        {
            CirclePuzzleData data = ringDataList[i];

            if (data.prefabIndex < 0 || data.prefabIndex >= ringPrefabs.Count)
            {
                Debug.LogWarning($"[UiCirclePuzzleManager] 프리팹 인덱스 범위 초과: {data.prefabIndex}");
                continue;
            }

            GameObject patternPrefab = ringPrefabs[data.prefabIndex];
            if (singleRingPrefab == null || gridParent == null) continue;

            // 1. 공용 베이스 틀 생성
            GameObject ringObj = Instantiate(singleRingPrefab, gridParent);
            ringObj.name = $"Ui_Circle_Ring_[Level_{i}]";

            // 2. [핵심] 작은 순서에서 바깥으로 갈수록 크기를 크게 세팅 (Scale 제어)
            RectTransform ringRect = ringObj.GetComponent<RectTransform>();
            if (ringRect != null)
            {
                float currentScale = startScale + (i * scaleStep);
                ringRect.localScale = new Vector3(currentScale, currentScale, 1f);
            }

            UiCircleComponent ringComp = ringObj.GetComponent<UiCircleComponent>() ?? ringObj.AddComponent<UiCircleComponent>();

            // 3. 문양 조립 및 세그먼트 주입
            ringComp.InitRingState(data.segments, patternPrefab, this);

            // 4. 큰 원이 아래로 깔리고 작은 원이 위로 와서 마우스를 클릭할 수 있게 UI 계층 구조 정렬
            // 작은 원(첫 번째 연산)일수록 맨 앞으로 뺍니다.
            ringObj.transform.SetAsFirstSibling();

            spawnedRings.Add(ringComp);
        }

        Debug.Log($"[UiCirclePuzzleManager] 서클 퍼즐 세팅 완료! 총 {spawnedRings.Count}개의 원이 크기별로 중첩 배치되었습니다.");
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
        Debug.Log("🎯 [UI 원형 퍼즐 클리어] 모든 크기의 원 문양이 올바르게 정렬되었습니다!");
        TriggerClearEvent();
    }
}