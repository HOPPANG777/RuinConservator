using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스프라이트 배열을 활용하여 단순한 2D UI 애니메이션을 재생하는 범용 컨트롤러입니다.
/// 프로젝트 컨벤션에 따라 코루틴을 사용하지 않고 Update 타이머로 구동됩니다.
/// </summary>
public class SimpleAnimationController : MonoBehaviour
{
    //==============================================================================
    // 직렬화 필드 (Settings)
    //==============================================================================
    [Header("Animation Settings")]
    [Tooltip("애니메이션에 사용될 스프라이트 배열 클립")]
    [SerializeField] private Sprite[] _animationClip;

    [Tooltip("애니메이션을 렌더링할 UI Image 컴포넌트")]
    [SerializeField] private Image _targetImage;

    [Tooltip("한 프레임이 유지되는 시간 (초 단위)")]
    [SerializeField] private float _animationTime = 0.1f;

    [Tooltip("체크 시 오브젝트가 활성화될 때 자동으로 애니메이션을 재생합니다.")]
    [SerializeField] private bool _playOnAwake = false;

    [Tooltip("체크 시 애니메이션을 무한 반복 재생합니다. 체크 해제 시 1회만 재생 후 정지합니다.")]
    [SerializeField] private bool _isLoop = true;

    //==============================================================================
    // 런타임 상태 변수 (Debug Info)
    //==============================================================================
    [Header("Debug Info")]
    [Tooltip("현재 프레임 누적 타이머 시간")]
    [SerializeField] private float _frameTime;

    [Tooltip("현재 재생 중인 프레임 인덱스 카운트")]
    [SerializeField] private int _frameCount;

    [Tooltip("현재 이미지에 적용된 스프라이트 클립")]
    [SerializeField] private Sprite _currentClip;

    // 재생 상태 제어용 플래그
    private bool _isPlaying;

    private void Start()
    {
        // 첫 번째 프레임 초기화
        if (_animationClip != null && _animationClip.Length > 0)
        {
            _currentClip = _animationClip[0];
            if (_targetImage != null)
            {
                _targetImage.sprite = _currentClip;
            }
        }

        // Play On Awake 설정에 따른 초기 재생 여부 결정
        if (_playOnAwake)
        {
            PlayAnimation();
        }
        else
        {
            _isPlaying = false;
        }
    }

    private void Update()
    {
        // 재생 상태가 아닐 때는 타이머 로직을 수행하지 않음
        if (!_isPlaying) return;
        if (_animationClip == null || _animationClip.Length == 0) return;

        _frameTime += Time.deltaTime;

        // _frameTime이 지정된 프레임 타임을 넘었을 때 다음 프레임 처리 (Update 타이머 방식 준수)
        if (_frameTime >= _animationTime)
        {
            _frameTime = 0.0f;
            int nextFrame = _frameCount + 1;

            // 마지막 프레임에 도달했을 때의 처리
            if (nextFrame >= _animationClip.Length)
            {
                if (_isLoop)
                {
                    _frameCount = 0;
                }
                else
                {
                    // 루프가 아니면 마지막 프레임 상태를 유지하고 정지
                    _isPlaying = false;
                    return;
                }
            }
            else
            {
                _frameCount = nextFrame;
            }

            _currentClip = _animationClip[_frameCount];
            if (_targetImage != null)
            {
                _targetImage.sprite = _currentClip;
            }
        }
    }

    //==============================================================================
    // 외부 제어 API
    //==============================================================================

    /// <summary>
    /// 애니메이션을 처음부터 재생합니다.
    /// </summary>
    public void PlayAnimation()
    {
        _isPlaying = true;
        _frameTime = 0.0f;
        _frameCount = 0;

        if (_animationClip != null && _animationClip.Length > 0)
        {
            _currentClip = _animationClip[0];
            if (_targetImage != null)
            {
                _targetImage.sprite = _currentClip;
                _targetImage.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 애니메이션 재생을 일시 정지합니다. (현재 프레임 유지)
    /// </summary>
    public void PauseAnimation()
    {
        _isPlaying = false;
    }

    /// <summary>
    /// 애니메이션을 정지하고 컴포넌트/오브젝트를 숨깁니다.
    /// </summary>
    public void StopAnimation()
    {
        _isPlaying = false;
        if (_targetImage != null)
        {
            _targetImage.gameObject.SetActive(false);
        }
    }
}