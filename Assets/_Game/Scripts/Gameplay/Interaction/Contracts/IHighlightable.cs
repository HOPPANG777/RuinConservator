//==============================================================================
// 기능: interface::IHighlightable
// 설명: 상호작용 포커스 시 시각적 강조를 적용하는 오브젝트 규약
//==============================================================================
// NOTE: PlayerInteractionController가 대상 전환 시 SetHighlight(true/false)를 호출합니다.
//==============================================================================

/// <summary>
/// 상호작용 포커스 여부에 따라 시각적 강조를 토글하는 오브젝트의 규약입니다.
/// </summary>
public interface IHighlightable
{
    /// <summary>
    /// 하이라이트 표시 여부를 설정합니다.
    /// </summary>
    /// <param name="isActive">true면 강조, false면 기본 상태로 복귀합니다.</param>
    void SetHighlight(bool isActive);
}
