using System;
using System.Collections.Generic;

//==============================================================================
// 기능: class::CsvDocument
// 설명: 파싱된 CSV 헤더와 데이터 행을 보관하는 읽기 전용 문서 데이터
//==============================================================================
public sealed class CsvDocument
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 파싱한 CSV 원본 이름입니다.
    private readonly string _sourceName;

    // CSV 첫 번째 레코드에서 읽은 헤더 목록입니다.
    private readonly IReadOnlyList<string> _headers;

    // CSV 헤더를 제외한 데이터 행 목록입니다.
    private readonly IReadOnlyList<CsvRow> _rows;

    // 헤더 이름으로 열 위치를 찾기 위한 인덱스입니다.
    private readonly Dictionary<string, int> _headerIndices;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>파싱한 CSV 원본 이름입니다.</summary>
    public string SourceName => _sourceName;

    /// <summary>CSV 첫 번째 레코드에서 읽은 헤더 목록입니다.</summary>
    public IReadOnlyList<string> Headers => _headers;

    /// <summary>CSV 헤더를 제외한 데이터 행 목록입니다.</summary>
    public IReadOnlyList<CsvRow> Rows => _rows;

    /// <summary>CSV 헤더를 제외한 데이터 행 개수입니다.</summary>
    public int RowCount => _rows.Count;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 파싱이 끝난 CSV 문서 데이터를 생성합니다.
    /// </summary>
    /// <param name="sourceName">파싱한 CSV 원본 이름입니다.</param>
    /// <param name="headers">CSV 첫 번째 레코드에서 읽은 헤더 목록입니다.</param>
    /// <param name="dataRows">CSV 헤더를 제외한 데이터 행 값 목록입니다.</param>
    /// <param name="dataLineNumbers">각 데이터 행이 시작된 물리적 줄 번호 목록입니다.</param>
    internal CsvDocument(
        string sourceName,
        IReadOnlyList<string> headers,
        IReadOnlyList<List<string>> dataRows,
        IReadOnlyList<int> dataLineNumbers)
    {
        _sourceName = sourceName ?? string.Empty;
        _headers = headers != null
            ? new List<string>(headers).AsReadOnly()
            : new List<string>().AsReadOnly();
        _headerIndices = BuildHeaderIndices(_headers);
        _rows = BuildRows(dataRows, dataLineNumbers, _headerIndices);
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 지정한 헤더 이름에 해당하는 열 위치를 조회합니다.
    /// </summary>
    /// <param name="headerName">조회할 CSV 헤더 이름입니다.</param>
    /// <param name="columnIndex">조회된 0부터 시작하는 열 위치입니다.</param>
    /// <returns>헤더가 존재하면 true입니다.</returns>
    public bool TryGetHeaderIndex(string headerName, out int columnIndex)
    {
        columnIndex = -1;

        if (string.IsNullOrWhiteSpace(headerName))
        {
            return false;
        }

        return _headerIndices.TryGetValue(headerName, out columnIndex);
    }

    /// <summary>
    /// 0부터 시작하는 데이터 행 위치로 CSV 행을 조회합니다.
    /// </summary>
    /// <param name="rowIndex">조회할 0부터 시작하는 데이터 행 위치입니다.</param>
    /// <param name="row">조회된 CSV 데이터 행입니다.</param>
    /// <returns>행 위치가 유효하면 true입니다.</returns>
    public bool TryGetRow(int rowIndex, out CsvRow row)
    {
        row = null;

        if (rowIndex < 0 || rowIndex >= _rows.Count)
        {
            return false;
        }

        row = _rows[rowIndex];
        return true;
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 헤더 이름을 대소문자 구분 방식으로 조회할 수 있는 인덱스를 생성합니다.
    /// </summary>
    /// <param name="headers">인덱스로 변환할 CSV 헤더 목록입니다.</param>
    /// <returns>헤더 이름별 열 위치가 저장된 인덱스입니다.</returns>
    private static Dictionary<string, int> BuildHeaderIndices(IReadOnlyList<string> headers)
    {
        Dictionary<string, int> headerIndices = new Dictionary<string, int>(StringComparer.Ordinal);

        if (headers == null)
        {
            return headerIndices;
        }

        for (int i = 0; i < headers.Count; i++)
        {
            headerIndices.Add(headers[i], i);
        }

        return headerIndices;
    }

    /// <summary>
    /// 파싱된 값 목록과 줄 번호를 읽기 전용 CSV 행 목록으로 변환합니다.
    /// </summary>
    /// <param name="dataRows">CSV 헤더를 제외한 데이터 행 값 목록입니다.</param>
    /// <param name="dataLineNumbers">각 데이터 행이 시작된 물리적 줄 번호 목록입니다.</param>
    /// <param name="headerIndices">헤더 이름별 열 위치 인덱스입니다.</param>
    /// <returns>읽기 전용 CSV 행 목록입니다.</returns>
    private static IReadOnlyList<CsvRow> BuildRows(
        IReadOnlyList<List<string>> dataRows,
        IReadOnlyList<int> dataLineNumbers,
        IReadOnlyDictionary<string, int> headerIndices)
    {
        List<CsvRow> rows = new List<CsvRow>();

        if (dataRows == null || dataLineNumbers == null)
        {
            return rows.AsReadOnly();
        }

        int rowCount = dataRows.Count < dataLineNumbers.Count
            ? dataRows.Count
            : dataLineNumbers.Count;

        for (int i = 0; i < rowCount; i++)
        {
            int recordNumber = i + 2;
            rows.Add(new CsvRow(recordNumber, dataLineNumbers[i], dataRows[i], headerIndices));
        }

        return rows.AsReadOnly();
    }
}
