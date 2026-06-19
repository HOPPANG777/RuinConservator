using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

//==============================================================================
// 기능: class::StageFlowController
// 설명: 씬 전환 흐름과 페이드 연출만 오케스트레이션하는 독립 컨트롤러
//==============================================================================
// NOTE:
//  - 싱글톤이 아닙니다. 외부 접근은 반드시 GameRoot.Instance.StageFlow(IStageFlowService) 경유.
//  - 코루틴 금지. 모든 비동기는 UniTask + GetCancellationTokenOnDestroy() 사용.
//  - 책임 한정: 퍼즐 연산/UI 렌더/플레이어 세부 제어는 본 클래스에 두지 않습니다.
//    실제 데이터 복구는 이벤트(OnStageRestoreRequested)로 외부에 위임합니다.
//==============================================================================
[DisallowMultipleComponent]
public class StageFlowController : MonoBehaviour, IStageFlowService
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 스테이지 씬 이름 접두사입니다. (실제 이름 예: Stage_001)
    private const string STAGE_NAME_PREFIX = "Stage_";

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

    [Header("Scene")]
    [Tooltip("시스템 매니저가 상주하는 상시 씬의 이름입니다. (언로드 대상에서 제외)")]
    [SerializeField]
    private string _persistentSceneName = "Scene_Persistent";

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 현재 전환 파이프라인이 진행 중인지 여부를 담는 내부 플래그입니다.
    private bool _isTransitioning;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>현재 스테이지 전환이 진행 중인지 여부입니다.</summary>
    public bool IsTransitioning
    {
        get => _isTransitioning;
        private set => _isTransitioning = value;
    }

    //==============================================================================
    // 이벤트 (데이터 복구 위임)
    //==============================================================================

    /// <summary>
    /// 새 스테이지 로드 직후 데이터 복구가 필요할 때 발생하는 이벤트입니다.
    /// 실제 스폰/플레이어 배치 로직은 이 이벤트를 구독한 외부에서 처리합니다.
    /// </summary>
    public event Action<SceneTransferData> OnStageRestoreRequested;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출하는 초기화 함수입니다. 페이드 캔버스를 투명 상태로 정렬합니다.
    /// </summary>
    public void Init()
    {
        // 페이드 캔버스가 연결되지 않았다면 초기화할 대상이 없으므로 종료합니다.
        if (_fadeCanvasGroup == null)
        {
            return;
        }

        // 시작 시점에는 화면이 보여야 하므로 완전 투명 + 입력 통과 상태로 둡니다.
        _fadeCanvasGroup.alpha = 0.0f;
        _fadeCanvasGroup.blocksRaycasts = false;
    }

    //==============================================================================
    // 공개 API (IStageFlowService)
    //==============================================================================

    /// <summary>
    /// 지정한 스테이지 번호로 전환합니다. (페이드 인 → 언로드 → Additive 로드 → 데이터 복구 → 페이드 아웃)
    /// </summary>
    /// <param name="stageNumber">전환할 스테이지 번호입니다. (예: 1 → "Stage_001")</param>
    /// <param name="data">다음 씬으로 전달/보존할 데이터입니다.</param>
    /// <returns>전환 파이프라인 완료를 await 할 수 있는 UniTask입니다.</returns>
    public async UniTask ChangeStageAsync(int stageNumber, SceneTransferData data)
    {
        // 전환이 이미 진행 중이면 중복 호출을 막아 경쟁 상태를 방지합니다.
        if (_isTransitioning)
        {
            return;
        }

        // 음수 스테이지 번호는 잘못된 입력이므로 안전하게 차단합니다.
        if (stageNumber < 0)
        {
            Debug.LogWarning($"[StageFlowController] 잘못된 스테이지 번호입니다: {stageNumber}");
            return;
        }

        // 오브젝트 파괴 시 비동기 흐름이 함께 취소되도록 토큰을 연동합니다.
        CancellationToken token = this.GetCancellationTokenOnDestroy();

        IsTransitioning = true;

        try
        {
            // 1) 화면 암전: alpha 0 → 1 보간 대기.
            await FadeAsync(0.0f, 1.0f, token);

            // 2) 이전 씬 해제: 현재 액티브 씬이 상시 씬이 아니라면 언로드합니다.
            await UnloadActiveStageAsync(token);

            // 3) 새 씬 로드: 규칙으로 빌드된 Stage_XXX 이름을 Additive 로드 후 액티브로 지정합니다.
            string stageSceneName = BuildStageSceneName(stageNumber);
            await LoadStageAdditiveAsync(stageSceneName, token);

            // 4) 데이터 복구: 새 씬이 완전히 로드된 직후 복구 절차를 실행합니다.
            ExecuteDataRestoration(data);

            // 5) 화면 복귀: alpha 1 → 0 보간 대기.
            await FadeAsync(1.0f, 0.0f, token);
        }
        finally
        {
            // 예외/취소 여부와 무관하게 전환 플래그를 반드시 해제합니다.
            IsTransitioning = false;
        }
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 스테이지 번호로부터 안전하게 씬 이름을 생성합니다. (예: 1 → "Stage_001")
    /// </summary>
    /// <param name="stageNumber">스테이지 번호입니다.</param>
    /// <returns>"Stage_XXX" 형식의 씬 이름 문자열입니다.</returns>
    private string BuildStageSceneName(int stageNumber)
    {
        // 개발자가 문자열을 직접 입력하다 발생하는 오타를 원천 차단하기 위해 코드로 생성합니다.
        return $"{STAGE_NAME_PREFIX}{stageNumber:D3}";
    }

    /// <summary>
    /// 현재 액티브 씬이 상시 씬이 아니라면 비동기로 언로드합니다.
    /// </summary>
    /// <param name="token">취소 토큰입니다.</param>
    private async UniTask UnloadActiveStageAsync(CancellationToken token)
    {
        Scene activeScene = SceneManager.GetActiveScene();

        // 상시 씬(Scene_Persistent)은 시스템 매니저가 머무는 곳이므로 절대 언로드하지 않습니다.
        if (activeScene.name == _persistentSceneName)
        {
            return;
        }

        // 유효하게 로드된 씬일 때만 언로드를 시도합니다.
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return;
        }

        // 이전 스테이지 씬을 비동기로 내리고 완료를 대기합니다.
        await SceneManager.UnloadSceneAsync(activeScene).ToUniTask(cancellationToken: token);
    }

    /// <summary>
    /// 지정한 스테이지 씬을 Additive로 로드하고 액티브 씬으로 설정합니다.
    /// </summary>
    /// <param name="stageSceneName">로드할 스테이지 씬 이름입니다.</param>
    /// <param name="token">취소 토큰입니다.</param>
    private async UniTask LoadStageAdditiveAsync(string stageSceneName, CancellationToken token)
    {
        // 상시 씬을 유지한 채 스테이지 씬만 덧붙이도록 Additive 모드로 로드합니다.
        await SceneManager.LoadSceneAsync(stageSceneName, LoadSceneMode.Additive).ToUniTask(cancellationToken: token);

        // 방금 로드한 씬을 참조로 가져옵니다.
        Scene loadedScene = SceneManager.GetSceneByName(stageSceneName);

        // 정상적으로 로드되었을 때만 액티브 씬으로 전환합니다. (라이팅/인스턴스 기준 씬)
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
        }
    }

    /// <summary>
    /// 새 스테이지 로드 직후 데이터 복구 절차를 실행하는 구조적 뼈대입니다.
    /// </summary>
    /// <param name="data">복구에 사용할 전달 데이터입니다.</param>
    private void ExecuteDataRestoration(SceneTransferData data)
    {
        // 전달 데이터가 없으면 복구할 내용이 없으므로 종료합니다.
        if (data == null)
        {
            return;
        }

        // 실제 복구 로직(새 씬의 스폰 포인트를 targetSpawnPointId로 찾아 플레이어를 배치하고,
        // currentKeyCount 등 퍼즐 상태를 적용하는 작업)은 본 컨트롤러의 책임이 아닙니다.
        // 결합도를 낮추기 위해 구독자(외부 시스템)에게 이벤트로 위임합니다.
        OnStageRestoreRequested?.Invoke(data);
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
            float elapsed = 0.0f;

            // Update 메시지 대신 UniTask 루프로 매 프레임 alpha를 보간합니다.
            while (elapsed < _fadeDuration)
            {
                // 현재 프레임에서 흐른 시간을 누적합니다.
                elapsed += Time.deltaTime;

                // 0~1 진행도로 정규화한 뒤 선형 보간으로 alpha를 갱신합니다.
                float t = Mathf.Clamp01(elapsed / _fadeDuration);
                _fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);

                // 다음 Update 타이밍까지 양보합니다. (네이티브-매니지드 전환 오버헤드 없음)
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
