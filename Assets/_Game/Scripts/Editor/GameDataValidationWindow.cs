using UnityEditor;
using UnityEngine;

//==============================================================================
// 기능: class::GameDataValidationWindow
// 설명: 기획자와 개발자가 CSV 및 Unity 에셋 검증 결과를 확인하는 에디터 창
//==============================================================================
internal sealed class GameDataValidationWindow : EditorWindow
{
    // Unity 상단 메뉴에서 검증 창을 여는 경로입니다.
    private const string MENU_PATH = "Tools/Ruin Conservator/CSV 데이터 검증기";

    // 검증 창에 표시할 제목입니다.
    private const string WINDOW_TITLE = "CSV 데이터 검증";

    // 마지막으로 실행한 게임 데이터 검증 보고서입니다.
    private GameDataValidationReport _report;

    // 문제 목록의 스크롤 위치입니다.
    private Vector2 _scrollPosition;

    //==============================================================================
    // 메뉴 API
    //==============================================================================

    /// <summary>
    /// CSV 데이터 검증 창을 열고 현재 프로젝트 데이터를 즉시 검증합니다.
    /// </summary>
    [MenuItem(MENU_PATH)]
    private static void OpenWindow()
    {
        GameDataValidationWindow window = GetWindow<GameDataValidationWindow>();
        window.titleContent = new GUIContent(WINDOW_TITLE);
        window.minSize = new Vector2(560.0f, 320.0f);
        window.Show();
    }

    //==============================================================================
    // Unity 생명주기
    //==============================================================================

    /// <summary>
    /// 검증 창이 활성화될 때 현재 GameRoot 기준 검증 결과를 준비합니다.
    /// </summary>
    private void OnEnable()
    {
        _report = GameDataEditorValidator.Validate();
    }

    /// <summary>
    /// 검증 실행 버튼과 마지막 검증 결과를 에디터 창에 그립니다.
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("CSV 및 Unity 에셋 통합 검증", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "GameRoot에 실제 연결된 CSV, 대화 비주얼, 퀘스트 아이템을 검사합니다.",
            EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("전체 데이터 다시 검증", GUILayout.Height(30.0f)))
        {
            _report = GameDataEditorValidator.Validate();
            GameDataEditorValidator.LogReport(_report, true);
        }

        EditorGUILayout.Space();
        DrawSummary();
        EditorGUILayout.Space();
        DrawIssues();
    }

    //==============================================================================
    // 화면 출력
    //==============================================================================

    /// <summary>
    /// 마지막 검증의 오류와 경고 개수를 출력합니다.
    /// </summary>
    private void DrawSummary()
    {
        if (_report == null)
        {
            EditorGUILayout.HelpBox("아직 검증 결과가 없습니다.", MessageType.Info);
            return;
        }

        if (!_report.HasIssues)
        {
            EditorGUILayout.HelpBox("모든 CSV와 Unity 에셋 참조가 유효합니다.", MessageType.Info);
            return;
        }

        MessageType messageType = _report.HasErrors ? MessageType.Error : MessageType.Warning;
        EditorGUILayout.HelpBox(
            $"검증 완료: 오류 {_report.ErrorCount}개, 경고 {_report.WarningCount}개",
            messageType);
    }

    /// <summary>
    /// 마지막 검증에서 발견한 문제 목록과 관련 에셋 선택 버튼을 출력합니다.
    /// </summary>
    private void DrawIssues()
    {
        if (_report == null || !_report.HasIssues)
        {
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        for (int i = 0; i < _report.Issues.Count; i++)
        {
            DrawIssue(_report.Issues[i]);
            EditorGUILayout.Space(4.0f);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 검증 문제 한 건의 원본과 메시지 및 관련 에셋 선택 버튼을 출력합니다.
    /// </summary>
    /// <param name="issue">출력할 검증 문제입니다.</param>
    private static void DrawIssue(GameDataValidationIssue issue)
    {
        if (issue == null)
        {
            return;
        }

        MessageType messageType = issue.Severity == GameDataValidationSeverity.ERROR
            ? MessageType.Error
            : MessageType.Warning;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.HelpBox($"[{issue.SourceName}] {issue.Message}", messageType);

        if (issue.Context != null && GUILayout.Button("관련 에셋 선택", GUILayout.Width(110.0f)))
        {
            Selection.activeObject = issue.Context;
            EditorGUIUtility.PingObject(issue.Context);
        }

        EditorGUILayout.EndVertical();
    }
}
