using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

//==============================================================================
// 기능: class::GameDataBuildPreprocessor
// 설명: 플레이어 빌드 전에 CSV와 Unity 에셋 참조를 검증하고 오류가 있으면 빌드를 중단하는 전처리기
//==============================================================================
public sealed class GameDataBuildPreprocessor : IPreprocessBuildWithReport
{
    // 다른 빌드 전처리보다 먼저 게임 데이터 검증을 실행하는 순서입니다.
    private const int BUILD_CALLBACK_ORDER = -1000;

    // 빌드 검증 로그를 다른 시스템 로그와 구분하는 접두사입니다.
    private const string LOG_PREFIX = "[GameDataBuildPreprocessor]";

    // 빌드 대상 정보를 찾지 못했을 때 사용하는 이름입니다.
    private const string UNKNOWN_BUILD_TARGET = "Unknown";

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>게임 데이터 검증 빌드 전처리 순서입니다.</summary>
    public int callbackOrder => BUILD_CALLBACK_ORDER;

    //==============================================================================
    // 빌드 전처리 API
    //==============================================================================

    /// <summary>
    /// 플레이어 빌드 직전에 실제 GameRoot 기준 게임 데이터를 검증하고 오류가 있으면 빌드를 중단합니다.
    /// </summary>
    /// <param name="report">Unity가 생성 중인 플레이어 빌드 보고서입니다.</param>
    /// <exception cref="BuildFailedException">게임 데이터 검증 오류가 하나 이상 있으면 발생합니다.</exception>
    public void OnPreprocessBuild(BuildReport report)
    {
        string buildTargetName = GetBuildTargetName(report);
        GameDataValidationReport validationReport = GameDataEditorValidator.Validate();

        GameDataEditorValidator.LogReport(validationReport, false);

        if (validationReport == null)
        {
            throw new BuildFailedException(
                $"{LOG_PREFIX} 게임 데이터 검증 결과를 생성하지 못해 빌드를 중단했습니다. "
                + $"target={buildTargetName}");
        }

        if (!validationReport.HasErrors)
        {
            Debug.Log(
                $"{LOG_PREFIX} 게임 데이터 빌드 검증을 통과했습니다. "
                + $"target={buildTargetName}, warnings={validationReport.WarningCount}");
            return;
        }

        throw new BuildFailedException(
            $"{LOG_PREFIX} 게임 데이터 오류로 빌드를 중단했습니다. "
            + $"target={buildTargetName}, errors={validationReport.ErrorCount}, "
            + $"warnings={validationReport.WarningCount}. "
            + "Tools > Ruin Conservator > CSV 데이터 검증기에서 오류를 확인하세요.");
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 빌드 보고서에서 로그에 표시할 빌드 대상 이름을 안전하게 조회합니다.
    /// </summary>
    /// <param name="report">Unity 플레이어 빌드 보고서입니다.</param>
    /// <returns>빌드 대상 이름이며 보고서가 없으면 Unknown입니다.</returns>
    private static string GetBuildTargetName(BuildReport report)
    {
        return report != null
            ? report.summary.platform.ToString()
            : UNKNOWN_BUILD_TARGET;
    }
}
