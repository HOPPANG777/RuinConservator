using System;
using System.Collections.Generic;
using System.Text;

//==============================================================================
// 기능: class::CsvParser
// 설명: Excel에서 내보낸 RFC 4180 기반 CSV 문자열을 읽기 전용 문서로 변환하는 파서
//==============================================================================
public static class CsvParser
{
    //==============================================================================
    // 상수
    //==============================================================================

    // CSV 열 값을 구분하는 문자입니다.
    private const char CSV_SEPARATOR = ',';

    // CSV 인용 필드의 시작과 끝을 표시하는 문자입니다.
    private const char CSV_QUOTE = '"';

    // UTF-8 CSV 첫 글자에 포함될 수 있는 Byte Order Mark 문자입니다.
    private const char CSV_BYTE_ORDER_MARK = '\uFEFF';

    // 원본 이름이 전달되지 않았을 때 오류에 표시할 기본 이름입니다.
    private const string CSV_UNKNOWN_SOURCE_NAME = "(unknown)";

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// CSV 문자열을 헤더와 데이터 행이 포함된 읽기 전용 문서로 변환합니다.
    /// </summary>
    /// <param name="sourceName">오류와 문서에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="csvText">파싱할 전체 CSV 문자열입니다.</param>
    /// <param name="csvDocument">파싱에 성공한 CSV 문서입니다.</param>
    /// <param name="parseError">파싱에 실패한 첫 번째 오류입니다.</param>
    /// <returns>CSV 문서를 생성했으면 true입니다.</returns>
    public static bool TryParse(
        string sourceName,
        string csvText,
        out CsvDocument csvDocument,
        out CsvParseError parseError)
    {
        csvDocument = null;
        parseError = null;

        string resolvedSourceName = string.IsNullOrWhiteSpace(sourceName)
            ? CSV_UNKNOWN_SOURCE_NAME
            : sourceName.Trim();

        if (string.IsNullOrEmpty(csvText))
        {
            parseError = CreateError(
                resolvedSourceName,
                1,
                1,
                1,
                "CSV 내용이 비어 있습니다.");
            return false;
        }

        if (!TryReadRecords(
            resolvedSourceName,
            csvText,
            out List<List<string>> records,
            out List<int> recordLineNumbers,
            out parseError))
        {
            return false;
        }

        if (records.Count == 0)
        {
            parseError = CreateError(
                resolvedSourceName,
                1,
                1,
                1,
                "CSV 헤더 레코드를 찾을 수 없습니다.");
            return false;
        }

        List<string> headers = records[0];

        if (!TryValidateHeaders(resolvedSourceName, headers, out parseError))
        {
            return false;
        }

        if (!TryBuildDataRows(
            resolvedSourceName,
            headers.Count,
            records,
            recordLineNumbers,
            out List<List<string>> dataRows,
            out List<int> dataLineNumbers,
            out parseError))
        {
            return false;
        }

        csvDocument = new CsvDocument(
            resolvedSourceName,
            headers,
            dataRows,
            dataLineNumbers);
        return true;
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// CSV 문자열을 인용 필드 규칙에 따라 레코드와 열 값 목록으로 분리합니다.
    /// </summary>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="csvText">파싱할 전체 CSV 문자열입니다.</param>
    /// <param name="records">분리된 CSV 레코드 목록입니다.</param>
    /// <param name="recordLineNumbers">각 레코드가 시작된 물리적 줄 번호 목록입니다.</param>
    /// <param name="parseError">파싱에 실패한 첫 번째 오류입니다.</param>
    /// <returns>모든 레코드를 정상적으로 분리했으면 true입니다.</returns>
    private static bool TryReadRecords(
        string sourceName,
        string csvText,
        out List<List<string>> records,
        out List<int> recordLineNumbers,
        out CsvParseError parseError)
    {
        records = new List<List<string>>();
        recordLineNumbers = new List<int>();
        parseError = null;

        List<string> currentFields = new List<string>();
        StringBuilder fieldBuilder = new StringBuilder();

        int currentIndex = csvText[0] == CSV_BYTE_ORDER_MARK ? 1 : 0;
        int lineNumber = 1;
        int columnNumber = 1;
        int recordStartLineNumber = 1;

        bool isInQuotedField = false;
        bool isAfterClosingQuote = false;
        bool recordHadQuotedField = false;
        bool recordHadSeparator = false;
        bool recordHadNonWhitespaceCharacter = false;

        while (currentIndex < csvText.Length)
        {
            char currentCharacter = csvText[currentIndex];

            if (isInQuotedField)
            {
                if (currentCharacter == CSV_QUOTE)
                {
                    if (currentIndex + 1 < csvText.Length
                        && csvText[currentIndex + 1] == CSV_QUOTE)
                    {
                        fieldBuilder.Append(CSV_QUOTE);
                        currentIndex += 2;
                        columnNumber += 2;
                        continue;
                    }

                    isInQuotedField = false;
                    isAfterClosingQuote = true;
                    currentIndex++;
                    columnNumber++;
                    continue;
                }

                if (IsLineBreak(currentCharacter))
                {
                    fieldBuilder.Append('\n');
                    ConsumeLineBreak(csvText, ref currentIndex, ref lineNumber, ref columnNumber);
                    continue;
                }

                fieldBuilder.Append(currentCharacter);
                currentIndex++;
                columnNumber++;
                continue;
            }

            if (isAfterClosingQuote)
            {
                if (currentCharacter == CSV_SEPARATOR)
                {
                    AddCurrentField(currentFields, fieldBuilder);
                    recordHadSeparator = true;
                    isAfterClosingQuote = false;
                    currentIndex++;
                    columnNumber++;
                    continue;
                }

                if (IsLineBreak(currentCharacter))
                {
                    AddCurrentField(currentFields, fieldBuilder);
                    AddCurrentRecord(
                        records,
                        recordLineNumbers,
                        currentFields,
                        recordStartLineNumber);
                    ResetRecord(
                        ref currentFields,
                        fieldBuilder,
                        ref isAfterClosingQuote,
                        ref recordHadQuotedField,
                        ref recordHadSeparator,
                        ref recordHadNonWhitespaceCharacter);
                    ConsumeLineBreak(csvText, ref currentIndex, ref lineNumber, ref columnNumber);
                    recordStartLineNumber = lineNumber;
                    continue;
                }

                parseError = CreateError(
                    sourceName,
                    records.Count + 1,
                    lineNumber,
                    columnNumber,
                    "닫힌 인용 필드 뒤에는 쉼표 또는 줄바꿈만 올 수 있습니다.");
                return false;
            }

            if (currentCharacter == CSV_SEPARATOR)
            {
                AddCurrentField(currentFields, fieldBuilder);
                recordHadSeparator = true;
                currentIndex++;
                columnNumber++;
                continue;
            }

            if (IsLineBreak(currentCharacter))
            {
                bool isBlankLine = currentFields.Count == 0
                    && !recordHadQuotedField
                    && !recordHadSeparator
                    && !recordHadNonWhitespaceCharacter;

                if (!isBlankLine)
                {
                    AddCurrentField(currentFields, fieldBuilder);
                    AddCurrentRecord(
                        records,
                        recordLineNumbers,
                        currentFields,
                        recordStartLineNumber);
                }

                ResetRecord(
                    ref currentFields,
                    fieldBuilder,
                    ref isAfterClosingQuote,
                    ref recordHadQuotedField,
                    ref recordHadSeparator,
                    ref recordHadNonWhitespaceCharacter);
                ConsumeLineBreak(csvText, ref currentIndex, ref lineNumber, ref columnNumber);
                recordStartLineNumber = lineNumber;
                continue;
            }

            if (currentCharacter == CSV_QUOTE)
            {
                if (fieldBuilder.Length > 0)
                {
                    parseError = CreateError(
                        sourceName,
                        records.Count + 1,
                        lineNumber,
                        columnNumber,
                        "인용 부호는 빈 필드의 첫 문자에서만 시작할 수 있습니다.");
                    return false;
                }

                isInQuotedField = true;
                recordHadQuotedField = true;
                currentIndex++;
                columnNumber++;
                continue;
            }

            fieldBuilder.Append(currentCharacter);

            if (!char.IsWhiteSpace(currentCharacter))
            {
                recordHadNonWhitespaceCharacter = true;
            }

            currentIndex++;
            columnNumber++;
        }

        if (isInQuotedField)
        {
            parseError = CreateError(
                sourceName,
                records.Count + 1,
                lineNumber,
                columnNumber,
                "닫히지 않은 인용 필드가 있습니다.");
            return false;
        }

        bool hasPendingRecord = currentFields.Count > 0
            || recordHadQuotedField
            || recordHadSeparator
            || recordHadNonWhitespaceCharacter;

        if (hasPendingRecord)
        {
            AddCurrentField(currentFields, fieldBuilder);
            AddCurrentRecord(
                records,
                recordLineNumbers,
                currentFields,
                recordStartLineNumber);
        }

        return true;
    }

    /// <summary>
    /// CSV 헤더가 비어 있거나 중복되지 않았는지 검사합니다.
    /// </summary>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="headers">검사할 CSV 헤더 목록입니다.</param>
    /// <param name="parseError">검사에 실패한 첫 번째 오류입니다.</param>
    /// <returns>모든 헤더가 유효하고 고유하면 true입니다.</returns>
    private static bool TryValidateHeaders(
        string sourceName,
        IReadOnlyList<string> headers,
        out CsvParseError parseError)
    {
        parseError = null;
        HashSet<string> uniqueHeaders = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < headers.Count; i++)
        {
            string header = headers[i];

            if (string.IsNullOrWhiteSpace(header))
            {
                parseError = CreateError(
                    sourceName,
                    1,
                    1,
                    i + 1,
                    $"{i + 1}번째 CSV 헤더가 비어 있습니다.");
                return false;
            }

            if (!uniqueHeaders.Add(header))
            {
                parseError = CreateError(
                    sourceName,
                    1,
                    1,
                    i + 1,
                    $"중복 CSV 헤더가 있습니다. header={header}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 헤더를 제외한 레코드의 열 개수를 검사하고 문서 생성용 데이터 행을 분리합니다.
    /// </summary>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="headerCount">CSV 헤더 열 개수입니다.</param>
    /// <param name="records">헤더를 포함한 전체 CSV 레코드 목록입니다.</param>
    /// <param name="recordLineNumbers">각 레코드가 시작된 물리적 줄 번호 목록입니다.</param>
    /// <param name="dataRows">검사가 끝난 CSV 데이터 행 목록입니다.</param>
    /// <param name="dataLineNumbers">각 데이터 행이 시작된 물리적 줄 번호 목록입니다.</param>
    /// <param name="parseError">검사에 실패한 첫 번째 오류입니다.</param>
    /// <returns>모든 데이터 행의 열 개수가 헤더와 같으면 true입니다.</returns>
    private static bool TryBuildDataRows(
        string sourceName,
        int headerCount,
        IReadOnlyList<List<string>> records,
        IReadOnlyList<int> recordLineNumbers,
        out List<List<string>> dataRows,
        out List<int> dataLineNumbers,
        out CsvParseError parseError)
    {
        dataRows = new List<List<string>>();
        dataLineNumbers = new List<int>();
        parseError = null;

        for (int i = 1; i < records.Count; i++)
        {
            List<string> record = records[i];

            if (record.Count != headerCount)
            {
                int lineNumber = i < recordLineNumbers.Count
                    ? recordLineNumbers[i]
                    : 1;
                parseError = CreateError(
                    sourceName,
                    i + 1,
                    lineNumber,
                    1,
                    $"데이터 열 개수가 헤더와 다릅니다. expected={headerCount}, actual={record.Count}");
                return false;
            }

            dataRows.Add(record);
            dataLineNumbers.Add(recordLineNumbers[i]);
        }

        return true;
    }

    /// <summary>
    /// 현재 필드 문자열을 레코드의 열 값 목록에 추가하고 문자열 버퍼를 비웁니다.
    /// </summary>
    /// <param name="fields">현재 레코드의 열 값 목록입니다.</param>
    /// <param name="fieldBuilder">현재 열 값을 조립한 문자열 버퍼입니다.</param>
    private static void AddCurrentField(List<string> fields, StringBuilder fieldBuilder)
    {
        fields.Add(fieldBuilder.ToString());
        fieldBuilder.Clear();
    }

    /// <summary>
    /// 완성된 레코드와 시작 줄 번호를 전체 레코드 목록에 추가합니다.
    /// </summary>
    /// <param name="records">전체 CSV 레코드 목록입니다.</param>
    /// <param name="recordLineNumbers">각 레코드가 시작된 물리적 줄 번호 목록입니다.</param>
    /// <param name="fields">추가할 레코드의 열 값 목록입니다.</param>
    /// <param name="recordStartLineNumber">추가할 레코드가 시작된 물리적 줄 번호입니다.</param>
    private static void AddCurrentRecord(
        List<List<string>> records,
        List<int> recordLineNumbers,
        List<string> fields,
        int recordStartLineNumber)
    {
        records.Add(fields);
        recordLineNumbers.Add(recordStartLineNumber);
    }

    /// <summary>
    /// 다음 CSV 레코드를 읽을 수 있도록 현재 레코드 버퍼와 상태를 초기화합니다.
    /// </summary>
    /// <param name="currentFields">새로 생성할 현재 레코드 열 값 목록입니다.</param>
    /// <param name="fieldBuilder">비울 현재 필드 문자열 버퍼입니다.</param>
    /// <param name="isAfterClosingQuote">닫힌 인용 필드 직후 상태입니다.</param>
    /// <param name="recordHadQuotedField">현재 레코드에 인용 필드가 있었는지 여부입니다.</param>
    /// <param name="recordHadSeparator">현재 레코드에 구분자가 있었는지 여부입니다.</param>
    /// <param name="recordHadNonWhitespaceCharacter">현재 레코드에 공백이 아닌 문자가 있었는지 여부입니다.</param>
    private static void ResetRecord(
        ref List<string> currentFields,
        StringBuilder fieldBuilder,
        ref bool isAfterClosingQuote,
        ref bool recordHadQuotedField,
        ref bool recordHadSeparator,
        ref bool recordHadNonWhitespaceCharacter)
    {
        currentFields = new List<string>();
        fieldBuilder.Clear();
        isAfterClosingQuote = false;
        recordHadQuotedField = false;
        recordHadSeparator = false;
        recordHadNonWhitespaceCharacter = false;
    }

    /// <summary>
    /// 지정한 문자가 CR 또는 LF 줄바꿈 문자인지 확인합니다.
    /// </summary>
    /// <param name="character">확인할 문자입니다.</param>
    /// <returns>CR 또는 LF 문자이면 true입니다.</returns>
    private static bool IsLineBreak(char character)
    {
        return character == '\r' || character == '\n';
    }

    /// <summary>
    /// CRLF, CR, LF 줄바꿈을 하나의 물리적 줄바꿈으로 소비하고 위치를 갱신합니다.
    /// </summary>
    /// <param name="csvText">현재 파싱 중인 전체 CSV 문자열입니다.</param>
    /// <param name="currentIndex">갱신할 현재 문자열 위치입니다.</param>
    /// <param name="lineNumber">갱신할 현재 물리적 줄 번호입니다.</param>
    /// <param name="columnNumber">갱신할 현재 물리적 열 번호입니다.</param>
    private static void ConsumeLineBreak(
        string csvText,
        ref int currentIndex,
        ref int lineNumber,
        ref int columnNumber)
    {
        if (csvText[currentIndex] == '\r'
            && currentIndex + 1 < csvText.Length
            && csvText[currentIndex + 1] == '\n')
        {
            currentIndex += 2;
        }
        else
        {
            currentIndex++;
        }

        lineNumber++;
        columnNumber = 1;
    }

    /// <summary>
    /// CSV 원본과 위치 및 원인이 포함된 파싱 오류 데이터를 생성합니다.
    /// </summary>
    /// <param name="sourceName">오류가 발생한 CSV 원본 이름입니다.</param>
    /// <param name="recordNumber">헤더를 포함한 CSV 레코드 번호입니다.</param>
    /// <param name="lineNumber">오류가 발생한 물리적 줄 번호입니다.</param>
    /// <param name="columnNumber">오류가 발생한 물리적 열 번호입니다.</param>
    /// <param name="message">오류 원인을 설명하는 메시지입니다.</param>
    /// <returns>CSV 파싱 오류 데이터입니다.</returns>
    private static CsvParseError CreateError(
        string sourceName,
        int recordNumber,
        int lineNumber,
        int columnNumber,
        string message)
    {
        return new CsvParseError(
            sourceName,
            recordNumber,
            lineNumber,
            columnNumber,
            message);
    }
}
