using System.Collections.Generic;
using System.Globalization;

//==============================================================================
// 기능: class::CsvDefinitionReadUtility
// 설명: Definition CSV의 헤더와 행 값을 일관된 규칙으로 검증하는 내부 유틸리티
//==============================================================================
internal static class CsvDefinitionReadUtility
{
    /// <summary>
    /// 필수 CSV 원본을 파싱하고 헤더 이름과 순서를 검증합니다.
    /// </summary>
    /// <param name="csvFile">파싱할 필수 CSV 원본입니다.</param>
    /// <param name="expectedHeaders">스키마에 정의된 헤더 목록입니다.</param>
    /// <param name="document">파싱과 헤더 검증이 끝난 CSV 문서입니다.</param>
    /// <param name="loadError">파싱 또는 헤더 검증을 중단시킨 오류입니다.</param>
    /// <returns>CSV 문서를 정상적으로 읽었으면 true입니다.</returns>
    internal static bool TryParseRequiredDocument(
        GameDataCsvFile csvFile,
        IReadOnlyList<string> expectedHeaders,
        out CsvDocument document,
        out GameDataCsvLoadError loadError)
    {
        document = null;
        loadError = null;

        if (csvFile == null)
        {
            loadError = new GameDataCsvLoadError(
                "(missing)",
                0,
                0,
                string.Empty,
                "필수 CSV 원본이 전달되지 않았습니다.");
            return false;
        }

        if (!CsvParser.TryParse(
            csvFile.SourceName,
            csvFile.Text,
            out document,
            out CsvParseError parseError))
        {
            loadError = new GameDataCsvLoadError(
                parseError.SourceName,
                parseError.RecordNumber,
                parseError.LineNumber,
                parseError.ColumnNumber.ToString(CultureInfo.InvariantCulture),
                parseError.Message);
            return false;
        }

        return TryValidateHeaders(document, expectedHeaders, out loadError);
    }

    /// <summary>
    /// 선택 CSV가 존재하면 파싱하고, 없거나 비어 있으면 빈 입력으로 처리합니다.
    /// </summary>
    /// <param name="csvFile">파싱할 선택 CSV 원본입니다.</param>
    /// <param name="expectedHeaders">스키마에 정의된 헤더 목록입니다.</param>
    /// <param name="document">파싱된 CSV 문서이며 입력이 없으면 null입니다.</param>
    /// <param name="loadError">파싱 또는 헤더 검증을 중단시킨 오류입니다.</param>
    /// <returns>입력이 없거나 CSV 문서를 정상적으로 읽었으면 true입니다.</returns>
    internal static bool TryParseOptionalDocument(
        GameDataCsvFile csvFile,
        IReadOnlyList<string> expectedHeaders,
        out CsvDocument document,
        out GameDataCsvLoadError loadError)
    {
        document = null;
        loadError = null;

        if (csvFile == null || string.IsNullOrWhiteSpace(csvFile.Text))
        {
            return true;
        }

        return TryParseRequiredDocument(csvFile, expectedHeaders, out document, out loadError);
    }

    /// <summary>
    /// CSV 헤더의 개수, 이름, 순서가 확정 스키마와 정확히 같은지 확인합니다.
    /// </summary>
    /// <param name="document">검증할 CSV 문서입니다.</param>
    /// <param name="expectedHeaders">스키마에 정의된 헤더 목록입니다.</param>
    /// <param name="loadError">헤더 검증을 중단시킨 오류입니다.</param>
    /// <returns>헤더가 확정 스키마와 정확히 같으면 true입니다.</returns>
    internal static bool TryValidateHeaders(
        CsvDocument document,
        IReadOnlyList<string> expectedHeaders,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        if (document == null || expectedHeaders == null)
        {
            loadError = new GameDataCsvLoadError(
                document?.SourceName ?? "(unknown)",
                1,
                1,
                string.Empty,
                "CSV 문서 또는 예상 헤더 목록이 없습니다.");
            return false;
        }

        if (document.Headers.Count != expectedHeaders.Count)
        {
            loadError = new GameDataCsvLoadError(
                document.SourceName,
                1,
                1,
                string.Empty,
                $"CSV 헤더 개수가 스키마와 다릅니다. expected={expectedHeaders.Count}, actual={document.Headers.Count}");
            return false;
        }

        for (int i = 0; i < expectedHeaders.Count; i++)
        {
            if (document.Headers[i] == expectedHeaders[i])
            {
                continue;
            }

            loadError = new GameDataCsvLoadError(
                document.SourceName,
                1,
                1,
                document.Headers[i],
                $"CSV 헤더 이름 또는 순서가 다릅니다. expected={expectedHeaders[i]}, actual={document.Headers[i]}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 필수 시스템 ID를 읽고 영문 대문자, 숫자, 밑줄 규칙을 검증합니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">검증된 시스템 ID입니다.</param>
    /// <param name="loadError">값 검증을 중단시킨 오류입니다.</param>
    /// <returns>필수 시스템 ID가 유효하면 true입니다.</returns>
    internal static bool TryGetRequiredSystemId(
        CsvRow row,
        string sourceName,
        string columnName,
        out string value,
        out GameDataCsvLoadError loadError)
    {
        if (!TryGetRequiredValue(row, sourceName, columnName, out value, out loadError))
        {
            return false;
        }

        if (IsSystemIdValid(value))
        {
            return true;
        }

        loadError = CreateRowError(
            sourceName,
            row,
            columnName,
            "ID는 영문 대문자로 시작하고 영문 대문자, 숫자, 밑줄만 사용할 수 있습니다.");
        return false;
    }

    /// <summary>
    /// 선택 시스템 ID를 읽고 값이 있을 때만 ID 규칙을 검증합니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">검증된 선택 시스템 ID입니다.</param>
    /// <param name="loadError">값 검증을 중단시킨 오류입니다.</param>
    /// <returns>값이 비어 있거나 시스템 ID가 유효하면 true입니다.</returns>
    internal static bool TryGetOptionalSystemId(
        CsvRow row,
        string sourceName,
        string columnName,
        out string value,
        out GameDataCsvLoadError loadError)
    {
        if (!TryGetOptionalValue(row, sourceName, columnName, out value, out loadError))
        {
            return false;
        }

        if (string.IsNullOrEmpty(value) || IsSystemIdValid(value))
        {
            return true;
        }

        loadError = CreateRowError(
            sourceName,
            row,
            columnName,
            "ID는 영문 대문자로 시작하고 영문 대문자, 숫자, 밑줄만 사용할 수 있습니다.");
        return false;
    }

    /// <summary>
    /// 필수 로컬라이징 키를 읽고 영문 소문자 기반 키 규칙을 검증합니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">검증된 로컬라이징 키입니다.</param>
    /// <param name="loadError">값 검증을 중단시킨 오류입니다.</param>
    /// <returns>필수 로컬라이징 키가 유효하면 true입니다.</returns>
    internal static bool TryGetRequiredLocalizationKey(
        CsvRow row,
        string sourceName,
        string columnName,
        out string value,
        out GameDataCsvLoadError loadError)
    {
        if (!TryGetRequiredValue(row, sourceName, columnName, out value, out loadError))
        {
            return false;
        }

        if (IsLocalizationKeyValid(value))
        {
            return true;
        }

        loadError = CreateRowError(
            sourceName,
            row,
            columnName,
            "Localization Key는 영문 소문자로 시작하고 소문자, 숫자, 마침표, 밑줄만 사용할 수 있습니다.");
        return false;
    }

    /// <summary>
    /// 선택 로컬라이징 키를 읽고 값이 있을 때만 키 규칙을 검증합니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">검증된 선택 로컬라이징 키입니다.</param>
    /// <param name="loadError">값 검증을 중단시킨 오류입니다.</param>
    /// <returns>값이 비어 있거나 로컬라이징 키가 유효하면 true입니다.</returns>
    internal static bool TryGetOptionalLocalizationKey(
        CsvRow row,
        string sourceName,
        string columnName,
        out string value,
        out GameDataCsvLoadError loadError)
    {
        if (!TryGetOptionalValue(row, sourceName, columnName, out value, out loadError))
        {
            return false;
        }

        if (string.IsNullOrEmpty(value) || IsLocalizationKeyValid(value))
        {
            return true;
        }

        loadError = CreateRowError(
            sourceName,
            row,
            columnName,
            "Localization Key는 영문 소문자로 시작하고 소문자, 숫자, 마침표, 밑줄만 사용할 수 있습니다.");
        return false;
    }

    /// <summary>
    /// 필수 문자열 값을 읽고 빈 문자열이나 공백 입력을 거부합니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">읽은 필수 문자열 값입니다.</param>
    /// <param name="loadError">값 검증을 중단시킨 오류입니다.</param>
    /// <returns>값이 비어 있지 않으면 true입니다.</returns>
    internal static bool TryGetRequiredValue(
        CsvRow row,
        string sourceName,
        string columnName,
        out string value,
        out GameDataCsvLoadError loadError)
    {
        value = string.Empty;
        loadError = null;

        if (row == null || !row.TryGetValue(columnName, out value))
        {
            loadError = CreateRowError(sourceName, row, columnName, "필수 컬럼을 읽을 수 없습니다.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        loadError = CreateRowError(sourceName, row, columnName, "필수 값이 비어 있습니다.");
        return false;
    }

    /// <summary>
    /// 선택 문자열 값을 읽으며 빈 셀은 빈 문자열로 반환합니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">읽은 선택 문자열 값입니다.</param>
    /// <param name="loadError">값 조회를 중단시킨 오류입니다.</param>
    /// <returns>컬럼 값을 읽었으면 true입니다.</returns>
    internal static bool TryGetOptionalValue(
        CsvRow row,
        string sourceName,
        string columnName,
        out string value,
        out GameDataCsvLoadError loadError)
    {
        value = string.Empty;
        loadError = null;

        if (row != null && row.TryGetValue(columnName, out value))
        {
            return true;
        }

        loadError = CreateRowError(sourceName, row, columnName, "선택 컬럼을 읽을 수 없습니다.");
        return false;
    }

    /// <summary>
    /// 0 이상의 필수 정수를 읽습니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">읽은 0 이상의 정수입니다.</param>
    /// <param name="loadError">정수 변환을 중단시킨 오류입니다.</param>
    /// <returns>0 이상의 정수를 읽었으면 true입니다.</returns>
    internal static bool TryGetNonNegativeInt(
        CsvRow row,
        string sourceName,
        string columnName,
        out int value,
        out GameDataCsvLoadError loadError)
    {
        value = 0;

        if (!TryGetRequiredValue(row, sourceName, columnName, out string rawValue, out loadError))
        {
            return false;
        }

        if (int.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        loadError = CreateRowError(sourceName, row, columnName, "0 이상의 정수만 입력할 수 있습니다.");
        return false;
    }

    /// <summary>
    /// 1 이상의 필수 정수를 읽습니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">읽은 1 이상의 정수입니다.</param>
    /// <param name="loadError">정수 변환을 중단시킨 오류입니다.</param>
    /// <returns>1 이상의 정수를 읽었으면 true입니다.</returns>
    internal static bool TryGetPositiveInt(
        CsvRow row,
        string sourceName,
        string columnName,
        out int value,
        out GameDataCsvLoadError loadError)
    {
        if (!TryGetNonNegativeInt(row, sourceName, columnName, out value, out loadError))
        {
            return false;
        }

        if (value > 0)
        {
            return true;
        }

        loadError = CreateRowError(sourceName, row, columnName, "1 이상의 정수만 입력할 수 있습니다.");
        return false;
    }

    /// <summary>
    /// 소문자 true 또는 false로 작성된 필수 Boolean 값을 읽습니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">읽은 Boolean 값입니다.</param>
    /// <param name="loadError">Boolean 변환을 중단시킨 오류입니다.</param>
    /// <returns>정확한 소문자 Boolean 값을 읽었으면 true입니다.</returns>
    internal static bool TryGetRequiredBool(
        CsvRow row,
        string sourceName,
        string columnName,
        out bool value,
        out GameDataCsvLoadError loadError)
    {
        value = false;

        if (!TryGetRequiredValue(row, sourceName, columnName, out string rawValue, out loadError))
        {
            return false;
        }

        if (rawValue == "true")
        {
            value = true;
            return true;
        }

        if (rawValue == "false")
        {
            return true;
        }

        loadError = CreateRowError(sourceName, row, columnName, "Boolean은 소문자 true 또는 false만 사용할 수 있습니다.");
        return false;
    }

    /// <summary>
    /// 비어 있거나 소문자 true 또는 false로 작성된 선택 Boolean 값을 읽습니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">읽은 선택 Boolean 값입니다.</param>
    /// <param name="loadError">Boolean 변환을 중단시킨 오류입니다.</param>
    /// <returns>값이 비어 있거나 정확한 소문자 Boolean이면 true입니다.</returns>
    internal static bool TryGetOptionalBool(
        CsvRow row,
        string sourceName,
        string columnName,
        out bool? value,
        out GameDataCsvLoadError loadError)
    {
        value = null;

        if (!TryGetOptionalValue(row, sourceName, columnName, out string rawValue, out loadError))
        {
            return false;
        }

        if (string.IsNullOrEmpty(rawValue))
        {
            return true;
        }

        if (rawValue == "true")
        {
            value = true;
            return true;
        }

        if (rawValue == "false")
        {
            value = false;
            return true;
        }

        loadError = CreateRowError(sourceName, row, columnName, "Boolean은 빈 셀 또는 소문자 true, false만 사용할 수 있습니다.");
        return false;
    }

    /// <summary>
    /// 비어 있거나 1 이상의 선택 정수를 읽습니다.
    /// </summary>
    /// <param name="row">값을 읽을 CSV 행입니다.</param>
    /// <param name="sourceName">오류에 표시할 CSV 원본 이름입니다.</param>
    /// <param name="columnName">값을 읽을 컬럼 이름입니다.</param>
    /// <param name="value">읽은 선택 양의 정수입니다.</param>
    /// <param name="loadError">정수 변환을 중단시킨 오류입니다.</param>
    /// <returns>값이 비어 있거나 1 이상의 정수이면 true입니다.</returns>
    internal static bool TryGetOptionalPositiveInt(
        CsvRow row,
        string sourceName,
        string columnName,
        out int? value,
        out GameDataCsvLoadError loadError)
    {
        value = null;

        if (!TryGetOptionalValue(row, sourceName, columnName, out string rawValue, out loadError))
        {
            return false;
        }

        if (string.IsNullOrEmpty(rawValue))
        {
            return true;
        }

        if (int.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedValue)
            && parsedValue > 0)
        {
            value = parsedValue;
            return true;
        }

        loadError = CreateRowError(sourceName, row, columnName, "빈 셀 또는 1 이상의 정수만 입력할 수 있습니다.");
        return false;
    }

    /// <summary>
    /// 현재 CSV 행 위치를 포함하는 로드 오류를 생성합니다.
    /// </summary>
    /// <param name="sourceName">오류가 발생한 CSV 원본 이름입니다.</param>
    /// <param name="row">오류가 발생한 CSV 행입니다.</param>
    /// <param name="columnName">오류가 발생한 컬럼 이름입니다.</param>
    /// <param name="message">오류 원인을 설명하는 메시지입니다.</param>
    /// <returns>행 위치가 포함된 CSV 게임 데이터 로드 오류입니다.</returns>
    internal static GameDataCsvLoadError CreateRowError(
        string sourceName,
        CsvRow row,
        string columnName,
        string message)
    {
        return new GameDataCsvLoadError(
            sourceName,
            row?.RecordNumber ?? 0,
            row?.LineNumber ?? 0,
            columnName,
            message);
    }

    /// <summary>
    /// 문자열이 시스템 ID 작성 규칙을 만족하는지 확인합니다.
    /// </summary>
    /// <param name="value">검증할 시스템 ID입니다.</param>
    /// <returns>시스템 ID 작성 규칙을 만족하면 true입니다.</returns>
    private static bool IsSystemIdValid(string value)
    {
        if (string.IsNullOrEmpty(value) || value[0] < 'A' || value[0] > 'Z')
        {
            return false;
        }

        for (int i = 1; i < value.Length; i++)
        {
            char character = value[i];
            bool isUppercaseLetter = character >= 'A' && character <= 'Z';
            bool isNumber = character >= '0' && character <= '9';

            if (!isUppercaseLetter && !isNumber && character != '_')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 문자열이 로컬라이징 키 작성 규칙을 만족하는지 확인합니다.
    /// </summary>
    /// <param name="value">검증할 로컬라이징 키입니다.</param>
    /// <returns>로컬라이징 키 작성 규칙을 만족하면 true입니다.</returns>
    private static bool IsLocalizationKeyValid(string value)
    {
        if (string.IsNullOrEmpty(value) || value[0] < 'a' || value[0] > 'z')
        {
            return false;
        }

        for (int i = 1; i < value.Length; i++)
        {
            char character = value[i];
            bool isLowercaseLetter = character >= 'a' && character <= 'z';
            bool isNumber = character >= '0' && character <= '9';

            if (!isLowercaseLetter && !isNumber && character != '.' && character != '_')
            {
                return false;
            }
        }

        return true;
    }
}
