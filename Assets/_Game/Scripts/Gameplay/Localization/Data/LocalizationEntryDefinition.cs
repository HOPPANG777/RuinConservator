//==============================================================================
// 기능: class::LocalizationEntryDefinition
// 설명: CSV에서 읽은 언어별 화면 표시 문장 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class LocalizationEntryDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 다른 CSV 데이터에서 문장을 참조할 때 사용하는 Localization Key입니다.
    private readonly string _textKey;

    // 화면에 표시할 한국어 문장입니다.
    private readonly string _koreanText;

    // 화면에 표시할 영어 문장입니다.
    private readonly string _englishText;

    // 화면에 표시할 일본어 문장입니다.
    private readonly string _japaneseText;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>다른 CSV 데이터에서 문장을 참조할 때 사용하는 Localization Key입니다.</summary>
    public string TextKey => _textKey;

    /// <summary>화면에 표시할 한국어 문장입니다.</summary>
    public string KoreanText => _koreanText;

    /// <summary>화면에 표시할 영어 문장입니다.</summary>
    public string EnglishText => _englishText;

    /// <summary>화면에 표시할 일본어 문장입니다.</summary>
    public string JapaneseText => _japaneseText;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 언어별 화면 표시 문장 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="textKey">문장을 참조할 때 사용하는 Localization Key입니다.</param>
    /// <param name="koreanText">화면에 표시할 한국어 문장입니다.</param>
    /// <param name="englishText">화면에 표시할 영어 문장입니다.</param>
    /// <param name="japaneseText">화면에 표시할 일본어 문장입니다.</param>
    public LocalizationEntryDefinition(
        string textKey,
        string koreanText,
        string englishText,
        string japaneseText)
    {
        _textKey = textKey ?? string.Empty;
        _koreanText = koreanText ?? string.Empty;
        _englishText = englishText ?? string.Empty;
        _japaneseText = japaneseText ?? string.Empty;
    }
}
