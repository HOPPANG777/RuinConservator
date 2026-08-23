//==============================================================================
// 기능: interface::ILocalizationService
// 설명: CSV 문장 키를 현재 언어의 화면 표시 문자열로 변환하는 계약
//==============================================================================
public interface ILocalizationService
{
    /// <summary>현재 화면 표시 언어입니다.</summary>
    GameLanguage Language { get; }

    /// <summary>
    /// Localization Key를 현재 언어의 화면 표시 문자열로 변환합니다.
    /// </summary>
    /// <param name="textKey">변환할 Localization Key입니다.</param>
    /// <returns>현재 언어 문자열이며 찾지 못하면 입력 Key입니다.</returns>
    string GetText(string textKey);

    /// <summary>
    /// 화면 표시 언어를 변경합니다.
    /// </summary>
    /// <param name="language">변경할 언어입니다.</param>
    /// <returns>지원하는 언어로 변경했으면 true입니다.</returns>
    bool TrySetLanguage(GameLanguage language);
}
