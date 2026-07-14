//==============================================================================
// 기능: class::GameDataRepositoryBuildError
// 설명: 게임 데이터 저장소 생성 실패 대상과 원인을 보관하는 오류 데이터
//==============================================================================
public sealed class GameDataRepositoryBuildError
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 오류가 발생한 데이터 종류입니다.
    private readonly string _dataType;

    // 오류가 발생한 데이터 ID입니다.
    private readonly string _dataId;

    // 저장소를 생성할 수 없는 원인을 설명하는 메시지입니다.
    private readonly string _message;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>오류가 발생한 데이터 종류입니다.</summary>
    public string DataType => _dataType;

    /// <summary>오류가 발생한 데이터 ID입니다.</summary>
    public string DataId => _dataId;

    /// <summary>저장소를 생성할 수 없는 원인을 설명하는 메시지입니다.</summary>
    public string Message => _message;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 게임 데이터 저장소 생성 오류를 생성합니다.
    /// </summary>
    /// <param name="dataType">오류가 발생한 데이터 종류입니다.</param>
    /// <param name="dataId">오류가 발생한 데이터 ID입니다.</param>
    /// <param name="message">저장소를 생성할 수 없는 원인입니다.</param>
    internal GameDataRepositoryBuildError(string dataType, string dataId, string message)
    {
        _dataType = dataType ?? string.Empty;
        _dataId = dataId ?? string.Empty;
        _message = message ?? string.Empty;
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 로그에 사용할 저장소 생성 오류 문자열을 반환합니다.
    /// </summary>
    /// <returns>데이터 종류와 ID 및 오류 원인이 포함된 문자열입니다.</returns>
    public override string ToString()
    {
        return $"[GameDataRepository] type={_dataType}, id={_dataId}, message={_message}";
    }
}
