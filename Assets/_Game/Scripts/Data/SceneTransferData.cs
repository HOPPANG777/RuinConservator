//==============================================================================
// 기능: class::SceneTransferData
// 설명: 씬 전환 시 다음 씬으로 넘겨 보존할 데이터 컨테이너(POCO)
//==============================================================================
// NOTE: MonoBehaviour가 아닌 순수 데이터 클래스입니다.
//       전환 흐름과 데이터를 분리해 결합도를 낮추기 위해 별도 타입으로 둡니다.
//==============================================================================

/// <summary>
/// 스테이지(씬) 전환 시 다음 씬으로 전달/보존할 데이터를 담는 컨테이너입니다.
/// </summary>
[System.Serializable]
public class SceneTransferData
{
    // 목적지 씬에서 플레이어를 배치할 스폰 지점의 고유 ID입니다.
    public string targetSpawnPointId;

    // 퍼즐 진행 상태 예시: 현재 보유한 열쇠 개수입니다.
    public int currentKeyCount;

    /// <summary>
    /// 기본 생성자입니다. (필드는 기본값으로 초기화됩니다.)
    /// </summary>
    public SceneTransferData()
    {
    }

    /// <summary>
    /// 스폰 지점과 퍼즐 데이터를 지정해 전달 데이터를 생성합니다.
    /// </summary>
    /// <param name="targetSpawnPointId">목적지 스폰 지점 고유 ID입니다.</param>
    /// <param name="currentKeyCount">전달할 열쇠 개수입니다.</param>
    public SceneTransferData(string targetSpawnPointId, int currentKeyCount)
    {
        this.targetSpawnPointId = targetSpawnPointId;
        this.currentKeyCount = currentKeyCount;
    }
}
