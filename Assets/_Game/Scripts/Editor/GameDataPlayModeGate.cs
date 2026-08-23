using UnityEditor;
using UnityEngine;

//==============================================================================
// 기능: class::GameDataPlayModeGate
// 설명: 플레이 모드 진입 전에 CSV와 Unity 에셋 참조를 검증하고 오류 데이터의 실행을 차단하는 게이트
//==============================================================================
[InitializeOnLoad]
internal static class GameDataPlayModeGate
{
    // 플레이 모드 검증 로그를 다른 시스템 로그와 구분하는 접두사입니다.
    private const string LOG_PREFIX = "[GameDataPlayModeGate]";

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// 에디터 도메인이 로드될 때 플레이 모드 상태 변경 이벤트를 한 번만 구독합니다.
    /// </summary>
    static GameDataPlayModeGate()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    //==============================================================================
    // 검증 정책
    //==============================================================================

    /// <summary>
    /// 검증 보고서를 기준으로 플레이 모드 진입을 차단해야 하는지 확인합니다.
    /// </summary>
    /// <param name="report">CSV와 Unity 에셋 참조의 통합 검증 보고서입니다.</param>
    /// <returns>보고서가 없거나 오류가 하나 이상 있으면 true입니다.</returns>
    internal static bool ShouldBlockEntry(GameDataValidationReport report)
    {
        return report == null || report.HasErrors;
    }

    //==============================================================================
    // 에디터 이벤트
    //==============================================================================

    /// <summary>
    /// Edit Mode를 벗어나기 직전에 게임 데이터를 검증하고 오류가 있으면 플레이 전환을 취소합니다.
    /// </summary>
    /// <param name="state">Unity 에디터의 플레이 모드 전환 상태입니다.</param>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }

        GameDataValidationReport report = GameDataEditorValidator.Validate();
        GameDataEditorValidator.LogReport(report, false);

        if (ShouldBlockEntry(report) == false)
        {
            Debug.Log(
                $"{LOG_PREFIX} 게임 데이터 사전 검증을 통과했습니다. "
                + $"warnings={report.WarningCount}");
            return;
        }

        EditorApplication.isPlaying = false;

        int errorCount = report != null ? report.ErrorCount : 1;
        int warningCount = report != null ? report.WarningCount : 0;
        Debug.LogError(
            $"{LOG_PREFIX} 게임 데이터 오류로 플레이 모드 진입을 중단했습니다. "
            + $"errors={errorCount}, warnings={warningCount}. "
            + "Tools > Ruin Conservator > CSV 데이터 검증기에서 오류를 확인하세요.");
    }
}
