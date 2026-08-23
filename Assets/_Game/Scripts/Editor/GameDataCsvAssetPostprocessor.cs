using System;
using UnityEditor;

//==============================================================================
// 기능: class::GameDataCsvAssetPostprocessor
// 설명: CSV와 연결 에셋이 변경되면 통합 검증을 한 번 예약하는 에디터 후처리기
//==============================================================================
internal sealed class GameDataCsvAssetPostprocessor : AssetPostprocessor
{
    // 자동 검증 대상 CSV 디렉터리 경로입니다.
    private const string CSV_DIRECTORY_PATH = "Assets/_Game/Data/Csv/";

    // 비주얼 카탈로그와 아이템 데이터를 포함하는 게임 데이터 디렉터리 경로입니다.
    private const string GAME_DATA_DIRECTORY_PATH = "Assets/_Game/Data/";

    // 실제 런타임 연결이 저장되는 GameRoot 프리팹 경로입니다.
    private const string GAME_ROOT_PREFAB_PATH = "Assets/_Game/Prefabs/GameRoot.prefab";

    // 같은 에셋 처리 주기에 자동 검증이 중복 예약되었는지 여부입니다.
    private static bool _isValidationScheduled;

    //==============================================================================
    // Unity 에셋 후처리
    //==============================================================================

    /// <summary>
    /// CSV, 게임 데이터 에셋, GameRoot 연결이 바뀌면 에셋 처리 종료 뒤 통합 검증을 예약합니다.
    /// </summary>
    /// <param name="importedAssets">가져오거나 갱신된 에셋 경로 목록입니다.</param>
    /// <param name="deletedAssets">삭제된 에셋 경로 목록입니다.</param>
    /// <param name="movedAssets">이동된 에셋의 새 경로 목록입니다.</param>
    /// <param name="movedFromAssetPaths">이동된 에셋의 이전 경로 목록입니다.</param>
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!ShouldScheduleValidation(importedAssets)
            && !ShouldScheduleValidation(deletedAssets)
            && !ShouldScheduleValidation(movedAssets)
            && !ShouldScheduleValidation(movedFromAssetPaths))
        {
            return;
        }

        if (_isValidationScheduled)
        {
            return;
        }

        _isValidationScheduled = true;
        EditorApplication.delayCall += ValidateAfterAssetImport;
    }

    //==============================================================================
    // 자동 검증 예약
    //==============================================================================

    /// <summary>
    /// 변경된 에셋 경로 목록에 자동 검증 대상이 포함되어 있는지 확인합니다.
    /// </summary>
    /// <param name="assetPaths">검사할 에셋 경로 목록입니다.</param>
    /// <returns>CSV, 게임 데이터 에셋 또는 GameRoot 프리팹이 포함되어 있으면 true입니다.</returns>
    private static bool ShouldScheduleValidation(string[] assetPaths)
    {
        if (assetPaths == null)
        {
            return false;
        }

        for (int i = 0; i < assetPaths.Length; i++)
        {
            string assetPath = assetPaths[i];

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                continue;
            }

            if (string.Equals(assetPath, GAME_ROOT_PREFAB_PATH, StringComparison.Ordinal)
                || assetPath.StartsWith(CSV_DIRECTORY_PATH, StringComparison.Ordinal)
                || assetPath.StartsWith(GAME_DATA_DIRECTORY_PATH, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 에셋 가져오기가 끝난 뒤 통합 검증을 실행하고 문제가 있을 때만 Console에 출력합니다.
    /// </summary>
    private static void ValidateAfterAssetImport()
    {
        EditorApplication.delayCall -= ValidateAfterAssetImport;
        _isValidationScheduled = false;

        GameDataValidationReport report = GameDataEditorValidator.Validate();

        if (report.HasIssues)
        {
            GameDataEditorValidator.LogReport(report, false);
        }
    }
}
