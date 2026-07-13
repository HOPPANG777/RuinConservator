using UnityEngine;
using System.Collections.Generic;

public class CirclePuzzleManager : BasePuzzleManager
{
    [Header("Layer Settings")]
    [SerializeField] private LayerMask ringLayer;

    [Header("Ring Prefabs Pool")]
    [SerializeField] private List<GameObject> ringPrefabs = new List<GameObject>();

    [Header("Text File Path Settings")]
    [SerializeField] private string stageFileName = "Stage01";

    private List<CircleComponent> spawnedRings = new List<CircleComponent>();
    private CircleComponent selectedRing;
    private bool isDragging = false;
    private float startMouseAngle;
    private float startRingAngle;

    public override void InitializePuzzle()
    {
        LoadAndGeneratePuzzle();
    }

    private void LoadAndGeneratePuzzle()
    {
        // 기존 생성되어 있던 링 오브젝트들 지우기
        foreach (var ring in spawnedRings) if (ring != null) Destroy(ring.gameObject);
        spawnedRings.Clear();
        IsClear = false;

        // Resources 폴더에서 텍스트 파일 불러오기
        string fullPath = $"PuzzleData/Circle/{stageFileName}";
        TextAsset stageTextAsset = Resources.Load<TextAsset>(fullPath);

        if (stageTextAsset == null)
        {
            Debug.LogError($"[CirclePuzzleManager] 파일을 찾을 수 없습니다: Resources/{fullPath}.txt");
            return;
        }

        // 외부 원형 퍼즐 파서를 통해 데이터 가공받기
        List<CircleRingRawData> ringDataList = CirclePuzzleParser.Parse(stageTextAsset.text);
        if (ringDataList == null) return;

        // 가공된 데이터를 기반으로 화면에 링 실시간 배치
        for (int i = 0; i < ringDataList.Count; i++)
        {
            CircleRingRawData data = ringDataList[i];

            if (data.prefabIndex < 0 || data.prefabIndex >= ringPrefabs.Count)
            {
                Debug.LogWarning($"[CirclePuzzleManager] 프리팹 인덱스 범위 초과: {data.prefabIndex}");
                continue;
            }

            GameObject prefab = ringPrefabs[data.prefabIndex];
            if (prefab == null) continue;

            // Z축 오프셋을 주어 링 레이어가 카메라 겹침 현상으로 깜빡이지 않도록 정렬
            Vector3 spawnPos = transform.position;
            spawnPos.z -= (i * 0.1f);

            GameObject ringObj = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            ringObj.name = data.ringName;

            CircleComponent ringComp = ringObj.GetComponent<CircleComponent>() ?? ringObj.AddComponent<CircleComponent>();
            ringComp.segments = data.segments;
            ringComp.InitRingState();

            var spriteRenderer = ringObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.sortingOrder = i + 1;

            spawnedRings.Add(ringComp);
        }

        Debug.Log($"[CirclePuzzleManager] 원형 퍼즐 파일('{stageFileName}.txt') 로드 완료! 배치된 링 개수: {spawnedRings.Count}개");
    }

    private void Update()
    {
        if (IsClear) return;

        HandleMouseInteraction();
    }

    private void HandleMouseInteraction()
    {
        // 마우스 왼쪽 버튼을 눌렀을 때
        if (PuzzleInputUtil.IsLeftButtonTriggered() && PuzzleInputUtil.TryGetMouseWorldPosition(out Vector2 mousePos))
        {
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, ringLayer);
            if (hit.collider != null)
            {
                CircleComponent ring = hit.collider.GetComponent<CircleComponent>();
                if (ring != null)
                {
                    selectedRing = ring;
                    isDragging = true;

                    Vector2 center = (Vector2)transform.position;
                    startMouseAngle = Mathf.Atan2(mousePos.y - center.y, mousePos.x - center.x) * Mathf.Rad2Deg;
                    startRingAngle = selectedRing.transform.eulerAngles.z;
                }
            }
        }

        // 마우스를 드래그하고 있을 때 회전 반영
        if (isDragging && selectedRing != null && PuzzleInputUtil.TryGetMouseWorldPosition(out Vector2 currentMousePos))
        {
            Vector2 center = (Vector2)transform.position;
            float currentMouseAngle = Mathf.Atan2(currentMousePos.y - center.y, currentMousePos.x - center.x) * Mathf.Rad2Deg;

            float angleDifference = currentMouseAngle - startMouseAngle;
            float targetAngle = startRingAngle + angleDifference;

            selectedRing.transform.eulerAngles = new Vector3(0, 0, targetAngle);
        }

        // 마우스 버튼을 뗐을 때
        if (PuzzleInputUtil.IsLeftButtonReleased() && isDragging)
        {
            isDragging = false;
            if (selectedRing != null)
            {
                selectedRing.SnapToSegment();
                selectedRing = null;
                CheckClearCondition();
            }
        }
    }

    public override void CheckClearCondition()
    {
        if (spawnedRings == null || spawnedRings.Count == 0) return;

        foreach (var ring in spawnedRings)
        {
            if (!ring.IsAligned()) return; // 링 하나라도 정렬각도가 안 맞으면 탈출
        }

        IsClear = true;
        OnPuzzleClear();
    }

    protected override void OnPuzzleClear()
    {
        // 이벤트
    }
}