//==============================================================================
// 기능: class::GameDataCsvLoadError
// 설명: CSV 게임 데이터 변환 실패 위치와 원인을 보관하는 오류 데이터
//==============================================================================
public sealed class GameDataCsvLoadError
{
    // 오류가 발생한 CSV 원본 이름입니다.
    private readonly string _sourceName;

    // 헤더를 포함해 오류가 발생한 CSV 레코드 번호입니다.
    private readonly int _recordNumber;

    // 오류가 발생한 물리적 줄 번호입니다.
    private readonly int _lineNumber;

    // 오류가 발생한 CSV 컬럼 이름입니다.
    private readonly string _columnName;

    // 오류 원인을 설명하는 메시지입니다.
    private readonly string _message;

    /// <summary>오류가 발생한 CSV 원본 이름입니다.</summary>
    public string SourceName => _sourceName;

    /// <summary>헤더를 포함해 오류가 발생한 CSV 레코드 번호입니다.</summary>
    public int RecordNumber => _recordNumber;

    /// <summary>오류가 발생한 물리적 줄 번호입니다.</summary>
    public int LineNumber => _lineNumber;

    /// <summary>오류가 발생한 CSV 컬럼 이름입니다.</summary>
    public string ColumnName => _columnName;

    /// <summary>오류 원인을 설명하는 메시지입니다.</summary>
    public string Message => _message;

    /// <summary>
    /// CSV 게임 데이터 변환 오류를 생성합니다.
    /// </summary>
    /// <param name="sourceName">오류가 발생한 CSV 원본 이름입니다.</param>
    /// <param name="recordNumber">헤더를 포함한 CSV 레코드 번호입니다.</param>
    /// <param name="lineNumber">오류가 발생한 물리적 줄 번호입니다.</param>
    /// <param name="columnName">오류가 발생한 CSV 컬럼 이름입니다.</param>
    /// <param name="message">오류 원인을 설명하는 메시지입니다.</param>
    internal GameDataCsvLoadError(
        string sourceName,
        int recordNumber,
        int lineNumber,
        string columnName,
        string message)
    {
        _sourceName = sourceName ?? string.Empty;
        _recordNumber = recordNumber;
        _lineNumber = lineNumber;
        _columnName = columnName ?? string.Empty;
        _message = message ?? string.Empty;
    }

    /// <summary>
    /// 로그에 사용할 CSV 게임 데이터 오류 문자열을 반환합니다.
    /// </summary>
    /// <returns>원본과 위치 및 오류 원인이 포함된 문자열입니다.</returns>
    public override string ToString()
    {
        return $"[GameDataCsvLoader] source={_sourceName}, record={_recordNumber}, "
            + $"line={_lineNumber}, column={_columnName}, message={_message}";
    }
}
