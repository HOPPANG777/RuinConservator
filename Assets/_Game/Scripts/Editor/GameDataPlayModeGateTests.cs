using NUnit.Framework;

//==============================================================================
// 기능: class::GameDataPlayModeGateTests
// 설명: 플레이 모드 진입 전 게임 데이터 검증의 차단 정책을 검증하는 에디터 테스트
//==============================================================================
[TestFixture]
public sealed class GameDataPlayModeGateTests
{
    //==============================================================================
    // 차단 정책 테스트
    //==============================================================================

    /// <summary>
    /// 검증 보고서를 생성하지 못한 경우 안전하게 플레이 모드 진입을 차단하는지 검증합니다.
    /// </summary>
    [Test]
    public void MissingReportBlocksPlayModeEntry()
    {
        bool shouldBlock = GameDataPlayModeGate.ShouldBlockEntry(null);

        Assert.That(shouldBlock, Is.True);
    }

    /// <summary>
    /// 검증 오류가 하나 이상 있으면 플레이 모드 진입을 차단하는지 검증합니다.
    /// </summary>
    [Test]
    public void ErrorReportBlocksPlayModeEntry()
    {
        GameDataValidationReport report = new GameDataValidationReport();
        report.AddError("dialogue_nodes.csv", "테스트 오류입니다.");

        bool shouldBlock = GameDataPlayModeGate.ShouldBlockEntry(report);

        Assert.That(shouldBlock, Is.True);
    }

    /// <summary>
    /// 경고만 있는 경우 반복 작업을 위해 플레이 모드 진입을 허용하는지 검증합니다.
    /// </summary>
    [Test]
    public void WarningOnlyReportAllowsPlayModeEntry()
    {
        GameDataValidationReport report = new GameDataValidationReport();
        report.AddWarning("BG_MUSEUM_HALL", "테스트 경고입니다.");

        bool shouldBlock = GameDataPlayModeGate.ShouldBlockEntry(report);

        Assert.That(shouldBlock, Is.False);
    }
}
