using UnityEngine;

public class CircleComponent : MonoBehaviour
{
    [Header("Ring Settings")]
    public int segments = 8;
    public int currentSegmentOffset = 0;

    public float AnglePerSegment => 360f / segments;

    public void InitRingState()
    {
        int halfSegments = segments / 2;
        int randomOffset = 0;

        while (randomOffset == 0)
        {
            randomOffset = Random.Range(-halfSegments, halfSegments + 1);
        }

        currentSegmentOffset = randomOffset;
        SetVisualRotation(currentSegmentOffset * AnglePerSegment);
    }

    public void SetVisualRotation(float angle)
    {
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void SnapToSegment()
    {
        float currentAngle = transform.eulerAngles.z % 360f;
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

    public void SnapToNearestSegment(float finalAngle)
    {
        transform.eulerAngles = new Vector3(0, 0, finalAngle);
        SnapToSegment();
    }
}