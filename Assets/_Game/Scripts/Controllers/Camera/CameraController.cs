using UnityEngine;

//==============================================================================
// 기능: class::CameraController
// 설명: 카메라의 관리를 맞는 카메라 컨트롤러
//==============================================================================
// TODO: 
//
//==============================================================================
public class CameraController : MonoBehaviour
{
    //==============================================================================
    // 함수
    //==============================================================================
    public void Follow(ICameraTarget target)
    {
        // 직접 구현이면 currentTarget 변경
        // Cinemachine이면 virtualCamera.Follow 변경
    }

    public void Focus(ICameraTarget target)
    {
        // Cinemachine이면 focusCamera.Follow / LookAt 변경 후 Priority 증가
    }

    public void FocusForSeconds(ICameraTarget target, float duration)
    {
        // Update 타이머로 일정 시간 후 RestoreDefaultTarget
    }

    public void RestoreDefaultTarget()
    {
        // 기본 플레이어 타겟으로 복귀
    }
}
