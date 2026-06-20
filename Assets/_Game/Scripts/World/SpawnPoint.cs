using UnityEngine;

//==============================================================================
// 기능: class::SpawnPoint
// 설명: 씬 전환 후 플레이어(또는 카메라)를 배치할 스폰 지점 마커
//==============================================================================
// NOTE: targetSpawnPointId와 일치하는 ID를 인스펙터에 설정합니다.
//==============================================================================
[DisallowMultipleComponent]
public class SpawnPoint : MonoBehaviour
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Spawn")]
    [Tooltip("씬 전환 시 SceneTransferData.targetSpawnPointId와 매칭되는 고유 ID입니다.")]
    [SerializeField]
    private string _spawnPointId;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>스폰 지점 고유 ID입니다.</summary>
    public string SpawnPointId
    {
        get => _spawnPointId;
    }

    /// <summary>스폰 위치입니다.</summary>
    public Vector3 Position
    {
        get => transform.position;
    }

    /// <summary>스폰 방향(회전)입니다.</summary>
    public Quaternion Rotation
    {
        get => transform.rotation;
    }
}
