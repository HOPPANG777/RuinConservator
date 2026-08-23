using System.Collections.Generic;

//==============================================================================
// 기능: class::CsvRow
// 설명: CSV 데이터 한 행과 헤더 기반 값 조회 기능을 제공하는 읽기 전용 데이터
//==============================================================================
public sealed class CsvRow
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 헤더를 포함한 CSV 레코드 번호입니다.
    private readonly int _recordNumber;

    // 이 레코드가 시작된 물리적 줄 번호입니다.
    private readonly int _lineNumber;

    // 이 레코드에 포함된 열 값 목록입니다.
    private readonly IReadOnlyList<string> _values;

    // 헤더 이름으로 열 위치를 찾기 위한 읽기 전용 인덱스입니다.
    private readonly IReadOnlyDictionary<string, int> _headerIndices;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>헤더를 포함한 CSV 레코드 번호입니다.</summary>
    public int RecordNumber => _recordNumber;

    /// <summary>이 레코드가 시작된 물리적 줄 번호입니다.</summary>
    public int LineNumber => _lineNumber;

    /// <summary>이 레코드에 포함된 열 값 목록입니다.</summary>
    public IReadOnlyList<string> Values => _values;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// CSV 데이터 행을 생성합니다.
    /// </summary>
    /// <param name="recordNumber">헤더를 포함한 CSV 레코드 번호입니다.</param>
    /// <param name="lineNumber">이 레코드가 시작된 물리적 줄 번호입니다.</param>
    /// <param name="values">이 레코드에 포함된 열 값 목록입니다.</param>
    /// <param name="headerIndices">헤더 이름으로 열 위치를 찾기 위한 인덱스입니다.</param>
    internal CsvRow(
        int recordNumber,
        int lineNumber,
        IReadOnlyList<string> values,
        IReadOnlyDictionary<string, int> headerIndices)
    {
        _recordNumber = recordNumber;
        _lineNumber = lineNumber;
        _values = values != null
            ? new List<string>(values).AsReadOnly()
            : new List<string>().AsReadOnly();
        _headerIndices = headerIndices;
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 지정한 헤더 이름에 해당하는 원본 셀 값을 조회합니다.
    /// </summary>
    /// <param name="headerName">조회할 CSV 헤더 이름입니다.</param>
    /// <param name="value">조회된 원본 셀 값입니다.</param>
    /// <returns>헤더와 열 값이 존재하면 true입니다.</returns>
    public bool TryGetValue(string headerName, out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(headerName) || _headerIndices == null)
        {
            return false;
        }

        if (!_headerIndices.TryGetValue(headerName, out int columnIndex))
        {
            return false;
        }

        if (columnIndex < 0 || columnIndex >= _values.Count)
        {
            return false;
        }

        value = _values[columnIndex];
        return true;
    }

    /// <summary>
    /// 지정한 헤더의 셀 값을 조회하고 명시적 줄바꿈 표기를 실제 줄바꿈으로 변환합니다.
    /// </summary>
    /// <param name="headerName">조회할 CSV 헤더 이름입니다.</param>
    /// <param name="value">줄바꿈 표기가 변환된 셀 값입니다.</param>
    /// <returns>헤더와 열 값이 존재하면 true입니다.</returns>
    public bool TryGetDecodedText(string headerName, out string value)
    {
        value = string.Empty;

        if (!TryGetValue(headerName, out string rawValue))
        {
            return false;
        }

        value = CsvTextUtility.DecodeEscapedLineBreaks(rawValue);
        return true;
    }
}
