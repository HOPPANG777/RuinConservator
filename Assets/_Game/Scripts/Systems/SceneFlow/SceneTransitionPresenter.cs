using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// 기능: class::SceneTransitionPresenter
// 설명: 씬 전환 시 시각적 연출(페이드, 로딩 애니메이션, 프로그레스 바)을 전담하는 Presenter
//==============================================================================
// NOTE:
//  - 싱글톤이 아니며 Update()를 사용하지 않습니다.
//  - StageFlowController가 ISceneTransitionService 멤버로 소유합니다.
//  - 코루틴 금지. UniTask + Time.realtimeSinceStartup 기반 순수 비동기 타이머만 사용합니다.
//==============================================================================
[DisallowMultipleComponent]
public class SceneTransitionPresenter : MonoBehaviour, ISceneTransitionService
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 로딩 애니메이션 UI를 켜기 시작하는 경과 시간(초)입니다.
    private const float ANIMATION_SHOW_DELAY = 0.5f;

    // 프로그레스 바 UI를 추가로 켜기 시작하는 경과 시간(초)입니다.
    private const float PROGRESS_SHOW_DELAY = 2.0f;

    // Unity AsyncOperation.progress가 0.9에 도달하면 로드가 완료된 것으로 간주하는 보정 분모입니다.
    private const float ASYNC_PROGRESS_MAX = 0.9f;

    //==============================================================================
    // 직렬화 필드 (인스펙터 노출)
    //==============================================================================

    [Header("Fade")]
    [Tooltip("화면 암전/복귀에 사용하는 페이드용 CanvasGroup입니다.")]
    [SerializeField]
    private CanvasGroup _fadeCanvasGroup;

    [Header("Fade")]
    [Tooltip("페이드 인/아웃 한 방향에 걸리는 시간(초)입니다.")]
    [SerializeField]
    private float _fadeDuration = 0.5f;

    [Header("Loading")]
    [Tooltip("로딩 연출에 사용할 애니메이션 비주얼 오브젝트입니다. (SimpleAnimationController 포함)")]
    [SerializeField]
    private GameObject _animationVisual;

    [Header("Loading")]
    [Tooltip("로딩 진행도를 표시할 UI Slider입니다.")]
    [SerializeField]
    private Slider _progressBarSlider;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 로딩 애니메이션 UI가 현재 활성화되어 있는지 여부입니다.
    private bool _isAnimationVisible;

    // 프로그레스 바 UI가 현재 활성화되어 있는지 여부입니다.
    private bool _isProgressVisible;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>페이드 인/아웃 한 방향에 걸리는 시간(초)입니다.</summary>
    public float FadeDuration
    {
        get => _fadeDuration;
    }

    //==============================================================================
    // 공개 API (ISceneTransitionService)
    //==============================================================================

    /// <summary>
    /// 페이드 캔버스와 로딩 UI를 초기 숨김/투명 상태로 정렬합니다.
    /// </summary>
    public void Init()
    {
        ResetLoadingUi();

        // 페이드 캔버스가 연결되지 않았다면 초기화할 대상이 없으므로 종료합니다.
        if (_fadeCanvasGroup == null)
        {
            return;
        }

        // 시작 시점에는 화면이 보여야 하므로 완전 투명 + 입력 통과 상태로 둡니다.
        _fadeCanvasGroup.alpha = 0.0f;
        _fadeCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 화면을 암전시킵니다. (CanvasGroup alpha 0 → 1, 로딩 UI는 비활성 상태 유지)
    /// </summary>
    /// <param name="token">취소 토큰입니다.</param>
    /// <returns>페이드 인 완료를 await 할 수 있는 UniTask입니다.</returns>
    public async UniTask FadeInAsync(CancellationToken token)
    {
        // 페이드 인 직전에는 로딩 UI를 반드시 꺼 둡니다.
        ResetLoadingUi();

        await FadeAsync(0.0f, 1.0f, token);
    }

    /// <summary>
    /// 비동기 씬 작업(로드/언로드)을 모니터링하며, 경과 시간에 따라 로딩 UI를 단계적으로 노출합니다.
    /// </summary>
    /// <param name="asyncOp">모니터링할 Unity 비동기 작업입니다.</param>
    /// <param name="token">취소 토큰입니다.</param>
    /// <returns>작업 완료 및 로딩 UI 정리를 await 할 수 있는 UniTask입니다.</returns>
    public async UniTask MonitorTransitionAsync(AsyncOperation asyncOp, CancellationToken token)
    {
        // 모니터링 대상이 없으면 할 일이 없으므로 종료합니다.
        if (asyncOp == null)
        {
            return;
        }

        // 페이드 인 완료 시점부터 실제 로딩 소요 시간을 측정합니다.
        float loadStartTime = Time.realtimeSinceStartup;

        // 작업이 완료될 때까지 매 프레임 경과 시간과 progress를 갱신합니다.
        while (!asyncOp.isDone)
        {
            float elapsed = Time.realtimeSinceStartup - loadStartTime;

            UpdateLoadingUiByElapsedTime(elapsed, asyncOp);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // 로딩이 끝나면 켜져 있던 로딩 UI를 모두 비활성화합니다.
        ResetLoadingUi();
    }

    /// <summary>
    /// 화면을 복귀시킵니다. (CanvasGroup alpha 1 → 0)
    /// </summary>
    /// <param name="token">취소 토큰입니다.</param>
    /// <returns>페이드 아웃 완료를 await 할 수 있는 UniTask입니다.</returns>
    public async UniTask FadeOutAsync(CancellationToken token)
    {
        await FadeAsync(1.0f, 0.0f, token);
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 경과 시간과 AsyncOperation progress에 따라 로딩 UI 노출 단계를 갱신합니다.
    /// </summary>
    /// <param name="elapsed">페이드 인 완료 후 경과 시간(초)입니다.</param>
    /// <param name="asyncOp">진행도를 읽을 비동기 작업입니다.</param>
    private void UpdateLoadingUiByElapsedTime(float elapsed, AsyncOperation asyncOp)
    {
        // 0.5초 이내에 로딩이 끝나면 UI를 켜는 오버헤드 없이 검은 화면만 유지합니다.
        if (elapsed >= ANIMATION_SHOW_DELAY && !_isAnimationVisible)
        {
            ShowAnimationVisual();
        }

        // 2.0초 이상 로딩이 길어지면 프로그레스 바를 추가로 노출합니다.
        if (elapsed >= PROGRESS_SHOW_DELAY && !_isProgressVisible)
        {
            ShowProgressBar();
        }

        // 프로그레스 바가 켜져 있을 때만 asyncOp.progress(0~0.9)를 0~1로 보정해 반영합니다.
        if (_isProgressVisible && _progressBarSlider != null)
        {
            _progressBarSlider.value = Mathf.Clamp01(asyncOp.progress / ASYNC_PROGRESS_MAX);
        }
    }

    /// <summary>
    /// 로딩 애니메이션 비주얼을 활성화하고 SimpleAnimationController 재생을 시작합니다.
    /// </summary>
    private void ShowAnimationVisual()
    {
        // 애니메이션 비주얼이 연결되지 않았으면 표시할 수 없으므로 종료합니다.
        if (_animationVisual == null)
        {
            return;
        }

        _animationVisual.SetActive(true);
        _isAnimationVisible = true;

        SimpleAnimationController animationController = _animationVisual.GetComponent<SimpleAnimationController>();

        // SimpleAnimationController가 있으면 정적 로딩 연출을 자연스럽게 가동합니다.
        if (animationController != null)
        {
            animationController.PlayAnimation();
        }
    }

    /// <summary>
    /// 프로그레스 바 UI를 활성화하고 초기값을 0으로 설정합니다.
    /// </summary>
    private void ShowProgressBar()
    {
        // 프로그레스 바가 연결되지 않았으면 표시할 수 없으므로 종료합니다.
        if (_progressBarSlider == null)
        {
            return;
        }

        _progressBarSlider.gameObject.SetActive(true);
        _progressBarSlider.value = 0.0f;
        _isProgressVisible = true;
    }

    /// <summary>
    /// 로딩 애니메이션과 프로그레스 바 UI를 모두 비활성화하고 내부 플래그를 초기화합니다.
    /// </summary>
    private void ResetLoadingUi()
    {
        _isAnimationVisible = false;
        _isProgressVisible = false;

        if (_animationVisual != null)
        {
            SimpleAnimationController animationController = _animationVisual.GetComponent<SimpleAnimationController>();

            if (animationController != null)
            {
                animationController.StopAnimation();
            }

            _animationVisual.SetActive(false);
        }

        if (_progressBarSlider != null)
        {
            _progressBarSlider.value = 0.0f;
            _progressBarSlider.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// CanvasGroup의 alpha를 from에서 to로 보간하는 순수 비동기 페이드 타이머입니다. (Update 미사용)
    /// </summary>
    /// <param name="from">시작 alpha 값입니다.</param>
    /// <param name="to">목표 alpha 값입니다.</param>
    /// <param name="token">취소 토큰입니다.</param>
    private async UniTask FadeAsync(float from, float to, CancellationToken token)
    {
        // 페이드 캔버스가 없으면 연출을 건너뜁니다.
        if (_fadeCanvasGroup == null)
        {
            return;
        }

        // 페이드 중에는 뒤쪽 UI 입력을 가로채도록 레이캐스트를 켭니다.
        _fadeCanvasGroup.blocksRaycasts = true;
        _fadeCanvasGroup.alpha = from;

        // 지속 시간이 0 이하이면 즉시 목표 값으로 스냅 처리합니다.
        if (_fadeDuration <= 0.0f)
        {
            _fadeCanvasGroup.alpha = to;
        }
        else
        {
            float fadeStartTime = Time.realtimeSinceStartup;

            // Time.realtimeSinceStartup 기반으로 페이드 진행도를 계산합니다.
            while (Time.realtimeSinceStartup - fadeStartTime < _fadeDuration)
            {
                float elapsed = Time.realtimeSinceStartup - fadeStartTime;
                float t = Mathf.Clamp01(elapsed / _fadeDuration);
                _fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            // 누적 오차를 제거하기 위해 마지막에 목표 값으로 확정합니다.
            _fadeCanvasGroup.alpha = to;
        }

        // 완전 투명(화면 복귀)일 때는 입력을 다시 통과시킵니다.
        if (Mathf.Approximately(to, 0.0f))
        {
            _fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}
