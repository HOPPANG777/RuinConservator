//==============================================================================
// 기능: interface::ISceneStateContributor
// 설명: 씬 오브젝트(퍼즐, 문 등)가 자신의 상태를 Capture/Apply하는 규약
//==============================================================================
// NOTE: 씬 언로드 전 Capture, 로드 후 Apply 시점에 ProgressController와 연동됩니다.
//       구현체는 MonoBehaviour로 씬에 배치하고 본 인터페이스를 구현합니다.
//==============================================================================

/// <summary>
/// 씬 오브젝트가 진행 상태 저장/복원에 참여하기 위한 규약입니다.
/// </summary>
public interface ISceneStateContributor
{
    /// <summary>씬 내에서 고유한 오브젝트 ID입니다.</summary>
    string ObjectId { get; }

    /// <summary>현재 상태를 JSON 문자열로 직렬화해 반환합니다.</summary>
    string CaptureState();

    /// <summary>저장된 JSON 상태를 역직렬화해 오브젝트에 적용합니다.</summary>
    /// <param name="stateJson">복원할 상태 JSON입니다.</param>
    void ApplyState(string stateJson);
}
