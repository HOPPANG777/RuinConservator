using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UiCircleComponent : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Ring Settings")]
    public int segments = 8;
    public int currentSegmentOffset = 0;

    public float AnglePerSegment => 360f / segments;

    private RectTransform rectTransform;
    private UiCirclePuzzleManager ownerManager;

    private float startMouseAngle;
    private float startRingAngle;

    // 오브젝트가 꺼져있을 때도 각도를 기억할 변수
    private float targetInitialAngle = 0f;
    private bool isInitialized = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // 매니저가 원을 생성할 때 호출하는 초기화 함수
    public void InitRingState(int totalSegments, GameObject patternPrefab, UiCirclePuzzleManager manager)
    {
        segments = totalSegments;
        ownerManager = manager;
        isInitialized = false;

        // 1. 기존 문양 오브젝트 청소
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // 2. 자식 문양 프리팹 생성 및 조립
        if (patternPrefab != null)
        {
            GameObject patternObj = Instantiate(patternPrefab, transform);
            RectTransform patternRect = patternObj.GetComponent<RectTransform>();
            if (patternRect != null)
            {
                patternRect.anchoredPosition = Vector2.zero;
                patternRect.localRotation = Quaternion.identity;
                patternRect.localScale = Vector3.one;
            }
        }

        // 3. 각 원마다 독립적인 랜덤 회전 상태 연산
        int halfSegments = segments / 2;
        int randomOffset = 0;
        int safetyNet = 0;

        while (randomOffset == 0 && safetyNet < 100)
        {
            randomOffset = Random.Range(-halfSegments, halfSegments + 1);
            safetyNet++;
        }

        if (randomOffset == 0) randomOffset = 1;
        currentSegmentOffset = randomOffset;

        // 4. 목표 회전 각도 미리 저장
        targetInitialAngle = currentSegmentOffset * AnglePerSegment;
        isInitialized = true;

        // 만약 오브젝트가 이미 활성화되어 있다면 즉시 회전 적용
        if (gameObject.activeInHierarchy)
        {
            ApplyInitialRotation();
        }
    }

    // ⭐ [핵심 해결책] 게임 오브젝트가 켜지는 시점(OnEnable)에 유니티 UI 리셋을 무시하고 최종 회전을 먹입니다.
    private void OnEnable()
    {
        if (isInitialized)
        {
            ApplyInitialRotation();
        }
    }

    private void ApplyInitialRotation()
    {
        SetVisualRotation(targetInitialAngle);
        Debug.Log($"[{gameObject.name}] 부모 원 자체 회전 완료 (OnEnable 안전제어) - 오프셋: {currentSegmentOffset}, 회전각도: {targetInitialAngle}°");
    }

    // 부모 원(rectTransform) 자체를 회전시킵니다.
    public void SetVisualRotation(float angle)
    {
        if (rectTransform != null)
        {
            rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // --- 마우스 드래그 핸들러 시스템 (부모 원 기준 회전 연산) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ownerManager != null && ownerManager.IsClear) return;

        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localMousePos);

        startMouseAngle = Mathf.Atan2(localMousePos.y, localMousePos.x) * Mathf.Rad2Deg;
        startRingAngle = rectTransform.localEulerAngles.z;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ownerManager != null && ownerManager.IsClear) return;

        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localMousePos);

        float currentMouseAngle = Mathf.Atan2(localMousePos.y, localMousePos.x) * Mathf.Rad2Deg;
        float angleDifference = currentMouseAngle - startMouseAngle;

        SetVisualRotation(startRingAngle + angleDifference);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToSegment();

        if (ownerManager != null)
        {
            ownerManager.CheckClearCondition();
        }
    }

    public void SnapToSegment()
    {
        if (rectTransform == null) return;

        float currentAngle = rectTransform.localEulerAngles.z % 360f;
        if (currentAngle < 0) currentAngle += 360f;

        int closestSegment = Mathf.RoundToInt(currentAngle / AnglePerSegment);
        if (closestSegment >= segments) closestSegment = 0;

        currentSegmentOffset = closestSegment;

        if (currentSegmentOffset > segments / 2)
        {
            currentSegmentOffset -= segments;
        }

        SetVisualRotation(closestSegment * AnglePerSegment);
    }

    public bool IsAligned()
    {
        return currentSegmentOffset == 0;
    }
}