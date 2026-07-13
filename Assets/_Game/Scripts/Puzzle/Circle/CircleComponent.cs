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
    private CirclePuzzleManager ownerManager;

    private float startMouseAngle;
    private float startRingAngle;

    private float targetInitialAngle = 0f;
    private bool isInitialized = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void InitRingState(int totalSegments, GameObject patternPrefab, CirclePuzzleManager manager)
    {
        segments = totalSegments;
        ownerManager = manager;
        isInitialized = false;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

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

        targetInitialAngle = currentSegmentOffset * AnglePerSegment;
        isInitialized = true;

        if (gameObject.activeInHierarchy)
        {
            ApplyInitialRotation();
        }
    }

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
    }

    public void SetVisualRotation(float angle)
    {
        if (rectTransform != null)
        {
            rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

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
            ownerManager.CheckClear();
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