using Unity.Cinemachine;
using UnityEngine;

//==============================================================================
// 기능: class::CameraController
// 설명: Cinemachine을 이용해 카메라 추적 및 포커스 전환을 처리하는 컨트롤러
//==============================================================================
// TODO: 카메라 흔들림 및 경계 제한 기능 추가 필요.
//
//==============================================================================
public class CameraController : MonoBehaviour
{
    //==============================================================================
    // 변수
    //============================================================================== 
    [Header("Camera")]
    [Tooltip("플레이어를 기본으로 추적하는 Cinemachine 카메라입니다.")]
    [SerializeField]
    private CinemachineCamera _followCamera;

    [Header("Camera")]
    [Tooltip("연출이나 퍼즐 대상을 임시로 포커스하는 Cinemachine 카메라입니다.")]
    [SerializeField]
    private CinemachineCamera _focusCamera;

    [Header("Camera")]
    [Tooltip("카메라가 기본으로 추적할 대상입니다.")]
    [SerializeField]
    private GameObject _defaultTarget;

    [Header("Camera")]
    [Tooltip("기본 추적 카메라의 우선순위입니다.")]
    [SerializeField]
    private int _followPriority = 10;

    [Header("Camera")]
    [Tooltip("임시 포커스 카메라의 우선순위입니다.")]
    [SerializeField]
    private int _focusPriority = 20;

    // 임시 포커스 유지 시간
    private float _focusElapsed;

    // 임시 포커스 목표 시간
    private float _focusDuration;

    // 임시 포커스 진행 여부
    private bool _isFocusing;

    private bool _isInit;

    //==============================================================================
    // 함수
    //============================================================================== 
    private void Update()
    {
        if (_isInit == false) return;

        // 임시 포커스가 진행 중이면 남은 시간을 매 프레임 갱신합니다.
        UpdateFocusTimer();
    }

    public void Init()
    {
        ResolveDefaultTargetIfNeeded();

        // 게임 시작 시 카메라가 기본 추적 대상을 따라가도록 초기화합니다.
        RestoreDefaultTarget();
        _isInit = true;
    }

    public void End()
    {
        _isInit = false;
    }

    // 카메라가 지정한 대상을 기본 추적 대상으로 설정합니다.
    public void Follow(ICameraTarget target)
    {
        // 전달받은 타겟이 없거나, 타겟 오브젝트가 없으면 추적할 수 없으므로 종료합니다.
        if (target == null || target.CameraTarget == null)
        {
            return;
        }

        // 전달받은 카메라 타겟을 기본 추적 대상으로 저장합니다.
        _defaultTarget = target.CameraTarget;

        // 새로 지정한 기본 추적 대상으로 카메라를 되돌립니다.
        RestoreDefaultTarget();
    }

    // 카메라가 지정한 대상을 임시로 포커스합니다.
    public void Focus(ICameraTarget target)
    {
        // 전달받은 타겟이 없거나, 타겟 오브젝트가 없으면 포커스할 수 없으므로 종료합니다.
        if (target == null || target.CameraTarget == null || _focusCamera == null || _followCamera == null)
        {
            return;
        }

        // Cinemachine 카메라가 사용할 Transform을 가져옵니다.
        Transform targetTransform = target.CameraTarget.transform;

        // 포커스 카메라가 지정한 대상을 따라가도록 설정합니다.
        _focusCamera.Follow = targetTransform;

        // 기본 추적 카메라의 우선순위를 기본값으로 유지합니다.
        _followCamera.Priority = _followPriority;

        // 포커스 카메라의 우선순위를 높여 현재 활성 카메라가 되도록 합니다.
        _focusCamera.Priority = _focusPriority;
    }

    // 카메라가 지정한 대상을 일정 시간 동안 포커스합니다.
    public void FocusForSeconds(ICameraTarget target, float duration)
    {
        // 포커스 시간이 0 이하이면 의미가 없으므로 실행하지 않습니다.
        if (duration <= 0.0f)
        {
            return;
        }

        // 전달받은 대상을 즉시 포커스합니다.
        Focus(target);

        // 임시 포커스 경과 시간을 초기화합니다.
        _focusElapsed = 0.0f;

        // 임시 포커스를 유지할 목표 시간을 저장합니다.
        _focusDuration = duration;

        // Update에서 포커스 시간을 갱신하도록 상태를 켭니다.
        _isFocusing = true;
    }

    // 카메라를 기본 추적 대상으로 되돌립니다.
    public void RestoreDefaultTarget()
    {
        ResolveDefaultTargetIfNeeded();

        // 기본 추적 대상이 없으면 되돌릴 대상이 없으므로 종료합니다.
        if (_defaultTarget == null || _followCamera == null)
        {
            return;
        }

        // 기본 추적 대상의 Transform을 가져옵니다.
        Transform targetTransform = _defaultTarget.transform;

        // 기본 추적 카메라가 기본 대상을 따라가도록 설정합니다.
        _followCamera.Follow = targetTransform;

        // 포커스 카메라의 우선순위를 낮춰 비활성 카메라처럼 동작하게 합니다.
        if (_focusCamera != null)
        {
            _focusCamera.Priority = 0;
        }

        // 기본 추적 카메라의 우선순위를 복구해 활성 카메라가 되도록 합니다.
        _followCamera.Priority = _followPriority;

        // 임시 포커스 상태를 종료합니다.
        _isFocusing = false;
    }

    // 기본 추적 대상이 비어 있으면 현재 씬의 플레이어를 찾아 자동으로 연결합니다.
    private void ResolveDefaultTargetIfNeeded()
    {
        if (_defaultTarget != null)
        {
            return;
        }

        UnitPlayer player = Object.FindFirstObjectByType<UnitPlayer>();

        if (player == null || player.CameraTarget == null)
        {
            return;
        }

        _defaultTarget = player.CameraTarget;
    }

    // 임시 포커스 시간을 갱신합니다.
    private void UpdateFocusTimer()
    {
        // 임시 포커스 중이 아니면 시간을 갱신할 필요가 없으므로 종료합니다.
        if (!_isFocusing)
        {
            return;
        }

        // 현재 프레임에서 흐른 시간을 임시 포커스 경과 시간에 더합니다.
        _focusElapsed += Time.deltaTime;

        // 아직 목표 포커스 시간에 도달하지 않았으면 포커스를 유지합니다.
        if (_focusElapsed < _focusDuration)
        {
            return;
        }

        // 목표 시간이 끝났으므로 카메라를 기본 추적 대상으로 되돌립니다.
        RestoreDefaultTarget();
    }
}
