//==============================================================================
// 기능: class::SceneTransferData
// 설명: 씬 전환 시 다음 씬으로 넘겨 보존할 데이터 컨테이너(POCO)
//==============================================================================
// NOTE: MonoBehaviour가 아닌 순수 데이터 클래스입니다.
//       퍼즐/전역 진행 상태는 ProgressController에 저장하고,
//       본 클래스는 전환 handoff(스폰, 목적지 스테이지)만 담습니다.
//==============================================================================

/// <summary>
/// 스테이지(씬) 전환 시 다음 씬으로 전달/보존할 handoff 데이터를 담는 컨테이너입니다.
/// </summary>
[System.Serializable]
public class SceneTransferData
{
    // 목적지 씬에서 플레이어를 배치할 스폰 지점의 고유 ID입니다.
    public string targetSpawnPointId;

    // Restore 시 어느 스테이지의 오브젝트 상태를 적용할지 나타내는 스테이지 번호입니다.
    public int enteringStageNumber = -1;

    /// <summary>
    /// 기본 생성자입니다. (필드는 기본값으로 초기화됩니다.)
    /// </summary>
    public SceneTransferData()
    {
    }

    /// <summary>
    /// 스폰 지점과 목적지 스테이지 번호를 지정해 handoff 데이터를 생성합니다.
    /// </summary>
    /// <param name="targetSpawnPointId">목적지 스폰 지점 고유 ID입니다.</param>
    /// <param name="enteringStageNumber">들어가는 스테이지 번호입니다.</param>
    public SceneTransferData(string targetSpawnPointId, int enteringStageNumber)
    {
        this.targetSpawnPointId = targetSpawnPointId;
        this.enteringStageNumber = enteringStageNumber;
    }
}
