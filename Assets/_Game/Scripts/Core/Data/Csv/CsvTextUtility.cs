using System.Text;

//==============================================================================
// 기능: class::CsvTextUtility
// 설명: CSV 화면 표시 문장에 사용하는 명시적 이스케이프 표기 변환 유틸리티
//==============================================================================
public static class CsvTextUtility
{
    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 문자열의 \n, \r, \r\n 표기를 실제 줄바꿈 문자로 변환합니다.
    /// 두 개의 연속된 역슬래시는 하나의 리터럴 역슬래시로 유지합니다.
    /// </summary>
    /// <param name="value">줄바꿈 표기를 변환할 원본 문자열입니다.</param>
    /// <returns>명시적 줄바꿈 표기가 실제 줄바꿈으로 변환된 문자열입니다.</returns>
    public static string DecodeEscapedLineBreaks(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder decodedBuilder = new StringBuilder(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            char currentCharacter = value[i];

            if (currentCharacter != '\\' || i + 1 >= value.Length)
            {
                decodedBuilder.Append(currentCharacter);
                continue;
            }

            char escapedCharacter = value[i + 1];

            if (escapedCharacter == '\\')
            {
                decodedBuilder.Append('\\');
                i++;
                continue;
            }

            if (escapedCharacter == 'n')
            {
                decodedBuilder.Append('\n');
                i++;
                continue;
            }

            if (escapedCharacter == 'r')
            {
                if (i + 3 < value.Length && value[i + 2] == '\\' && value[i + 3] == 'n')
                {
                    decodedBuilder.Append('\n');
                    i += 3;
                    continue;
                }

                decodedBuilder.Append('\r');
                i++;
                continue;
            }

            decodedBuilder.Append(currentCharacter);
        }

        return decodedBuilder.ToString();
    }
}
