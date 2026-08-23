using System.Collections.Generic;
using UnityEngine;

//==============================================================================
// 기능: enum::GameDataValidationSeverity
// 설명: 게임 데이터 검증 문제의 심각도를 구분하는 열거형
//==============================================================================
internal enum GameDataValidationSeverity
{
    // 게임 진행을 중단시킬 수 있어 반드시 수정해야 하는 문제입니다.
    ERROR,

    // 폴백으로 실행할 수 있지만 출시 전 확인해야 하는 문제입니다.
    WARNING
}

//==============================================================================
// 기능: class::GameDataValidationIssue
// 설명: 게임 데이터 검증에서 발견한 문제 한 건과 관련 Unity 에셋을 보관하는 데이터
//==============================================================================
internal sealed class GameDataValidationIssue
{
    // 문제의 심각도입니다.
    private readonly GameDataValidationSeverity _severity;

    // 문제가 발생한 CSV 또는 Unity 에셋 이름입니다.
    private readonly string _sourceName;

    // 문제의 원인과 해결 대상을 설명하는 메시지입니다.
    private readonly string _message;

    // 에디터에서 선택할 수 있는 관련 Unity 오브젝트입니다.
    private readonly Object _context;

    /// <summary>문제의 심각도입니다.</summary>
    public GameDataValidationSeverity Severity => _severity;

    /// <summary>문제가 발생한 CSV 또는 Unity 에셋 이름입니다.</summary>
    public string SourceName => _sourceName;

    /// <summary>문제의 원인과 해결 대상을 설명하는 메시지입니다.</summary>
    public string Message => _message;

    /// <summary>에디터에서 선택할 수 있는 관련 Unity 오브젝트입니다.</summary>
    public Object Context => _context;

    /// <summary>
    /// 게임 데이터 검증 문제 한 건을 생성합니다.
    /// </summary>
    /// <param name="severity">문제의 심각도입니다.</param>
    /// <param name="sourceName">문제가 발생한 원본 이름입니다.</param>
    /// <param name="message">문제 원인을 설명하는 메시지입니다.</param>
    /// <param name="context">관련 Unity 오브젝트입니다.</param>
    public GameDataValidationIssue(
        GameDataValidationSeverity severity,
        string sourceName,
        string message,
        Object context)
    {
        _severity = severity;
        _sourceName = sourceName ?? string.Empty;
        _message = message ?? string.Empty;
        _context = context;
    }
}

//==============================================================================
// 기능: class::GameDataValidationReport
// 설명: 한 번의 CSV 및 Unity 에셋 통합 검증 결과를 읽기 전용으로 제공하는 보고서
//==============================================================================
internal sealed class GameDataValidationReport
{
    // 검증에서 발견한 문제 목록입니다.
    private readonly List<GameDataValidationIssue> _issues = new List<GameDataValidationIssue>();

    // 검증에서 발견한 오류 개수입니다.
    private int _errorCount;

    // 검증에서 발견한 경고 개수입니다.
    private int _warningCount;

    /// <summary>검증에서 발견한 문제 목록입니다.</summary>
    public IReadOnlyList<GameDataValidationIssue> Issues => _issues;

    /// <summary>검증에서 발견한 오류 개수입니다.</summary>
    public int ErrorCount => _errorCount;

    /// <summary>검증에서 발견한 경고 개수입니다.</summary>
    public int WarningCount => _warningCount;

    /// <summary>게임 진행을 중단시킬 수 있는 오류가 있는지 여부입니다.</summary>
    public bool HasErrors => _errorCount > 0;

    /// <summary>오류 또는 경고가 하나 이상 있는지 여부입니다.</summary>
    public bool HasIssues => _issues.Count > 0;

    /// <summary>
    /// 반드시 수정해야 하는 검증 오류를 추가합니다.
    /// </summary>
    /// <param name="sourceName">문제가 발생한 원본 이름입니다.</param>
    /// <param name="message">문제 원인을 설명하는 메시지입니다.</param>
    /// <param name="context">관련 Unity 오브젝트입니다.</param>
    public void AddError(string sourceName, string message, Object context = null)
    {
        _issues.Add(new GameDataValidationIssue(
            GameDataValidationSeverity.ERROR,
            sourceName,
            message,
            context));
        _errorCount++;
    }

    /// <summary>
    /// 폴백으로 실행할 수 있지만 확인이 필요한 검증 경고를 추가합니다.
    /// </summary>
    /// <param name="sourceName">문제가 발생한 원본 이름입니다.</param>
    /// <param name="message">문제 원인을 설명하는 메시지입니다.</param>
    /// <param name="context">관련 Unity 오브젝트입니다.</param>
    public void AddWarning(string sourceName, string message, Object context = null)
    {
        _issues.Add(new GameDataValidationIssue(
            GameDataValidationSeverity.WARNING,
            sourceName,
            message,
            context));
        _warningCount++;
    }
}
