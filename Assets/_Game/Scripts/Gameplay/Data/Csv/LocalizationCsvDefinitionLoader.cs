using System;
using System.Collections.Generic;

//==============================================================================
// 기능: class::LocalizationCsvDefinitionLoader
// 설명: Localization CSV를 로컬라이징 Definition 목록으로 변환하는 도메인 로더
//==============================================================================
internal static class LocalizationCsvDefinitionLoader
{
    // Localization CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] HEADERS = { "text_key", "ko", "en", "ja" };

    /// <summary>
    /// Localization CSV를 로컬라이징 Definition 목록으로 변환합니다.
    /// </summary>
    /// <param name="csvFile">변환할 Localization CSV 원본입니다.</param>
    /// <param name="definitions">변환된 로컬라이징 Definition 목록입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 행을 정상적으로 변환했으면 true입니다.</returns>
    internal static bool TryLoad(
        GameDataCsvFile csvFile,
        out List<LocalizationEntryDefinition> definitions,
        out GameDataCsvLoadError loadError)
    {
        definitions = new List<LocalizationEntryDefinition>();
        loadError = null;

        if (!CsvDefinitionReadUtility.TryParseRequiredDocument(
            csvFile,
            HEADERS,
            out CsvDocument document,
            out loadError))
        {
            return false;
        }

        HashSet<string> textKeys = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                row,
                document.SourceName,
                "text_key",
                out string textKey,
                out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredValue(
                    row,
                    document.SourceName,
                    "ko",
                    out string koreanText,
                    out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalValue(
                    row,
                    document.SourceName,
                    "en",
                    out string englishText,
                    out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalValue(
                    row,
                    document.SourceName,
                    "ja",
                    out string japaneseText,
                    out loadError))
            {
                return false;
            }

            if (!textKeys.Add(textKey))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName,
                    row,
                    "text_key",
                    "중복 Localization Key가 있습니다.");
                return false;
            }

            definitions.Add(new LocalizationEntryDefinition(
                textKey,
                CsvTextUtility.DecodeEscapedLineBreaks(koreanText),
                CsvTextUtility.DecodeEscapedLineBreaks(englishText),
                CsvTextUtility.DecodeEscapedLineBreaks(japaneseText)));
        }

        return true;
    }
}
