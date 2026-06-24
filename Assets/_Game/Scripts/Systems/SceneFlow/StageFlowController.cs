using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

//==============================================================================
// 기능: class::StageFlowController
// 설명: 씬 전환 흐름을 오케스트레이션하는 독립 컨트롤러 (시각 연출은 SceneTransitionPresenter에 위임)
//==============================================================================
// NOTE:
//  - 싱글톤이 아닙니다. 외부 접근은 반드시 GameRoot.Instance.StageFlow(IStageFlowService) 경유.
//  - 코루틴 금지. 모든 비동기는 UniTask + GetCancellationTokenOnDestroy() 사용.
//  - 책임 한정: 퍼즐 연산/UI 렌더/플레이어 세부 상태 제어는 본 클래스에 두지 않습니다.
//    실제 데이터 Capture/복구는 이벤트(OnStageCaptureRequested, OnStageRestoreRequested)로 외부에 위임합니다.
//==============================================================================
[DisallowMultipleComponent]
public class StageFlowController : MonoBehaviour, IStageFlowService
{
    //==============================================================================
    // 직렬화 필드 (인스펙터 노출)
    //==============================================================================

    [Header("Transition")]
    [Tooltip("씬 전환 시 시각적 연출(페이드, 로딩 UI)을 담당하는 Presenter입니다.")]
    [SerializeField]
    private SceneTransitionPresenter _sceneTransitionPresenter;

    [Header("Scene")]
    [Tooltip("시스템 매니저가 상주하는 상시 씬의 이름입니다. (언로드 대상에서 제외)")]
    [SerializeField]
    private string _persistentSceneName = "Scene_Persistent";

    [Header("Scene")]
    [Tooltip("씬 ID/스테이지 번호와 실제 Unity 씬 이름을 매핑하는 카탈로그입니다.")]
    [SerializeField]
    private SceneCatalogSO _sceneCatalog;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 현재 전환 파이프라인이 진행 중인지 여부를 담는 내부 플래그입니다.
    private bool _isTransitioning;

    // 시각 연출 서비스에 대한 캐시 참조입니다. (인터페이스 경유 접근용)
    private ISceneTransitionService _sceneTransition;

    // 현재 로드되어 있는 스테이지 번호입니다. 스테이지가 없으면 -1입니다.
    private int _currentStageNumber = -1;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>현재 스테이지 전환이 진행 중인지 여부입니다.</summary>
    public bool IsTransitioning
    {
        get => _isTransitioning;
        private set => _isTransitioning = value;
    }

    /// <summary>현재 로드되어 있는 스테이지 번호입니다. 없으면 -1입니다.</summary>
    public int CurrentStageNumber
    {
        get => _currentStageNumber;
    }

    //==============================================================================
    // 이벤트 (데이터 Capture/복구 위임)
    //==============================================================================

    /// <summary>
    /// 스테이지 언로드 직전 현재 씬 상태를 저장해야 할 때 발생하는 이벤트입니다.
    /// </summary>
    public event Action<int> OnStageCaptureRequested;

    /// <summary>
    /// 새 스테이지 로드 직후 데이터 복구가 필요할 때 발생하는 이벤트입니다.
    /// 실제 스폰/플레이어 배치 로직은 이 이벤트를 구독한 외부에서 처리합니다.
    /// </summary>
    public event Action<SceneTransferData> OnStageRestoreRequested;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 직렬화된 Presenter 참조를 인터페이스 멤버로 캐시합니다.
    /// </summary>
    private void Awake()
    {
        _sceneTransition = _sceneTransitionPresenter;
    }

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출하는 초기화 함수입니다. Presenter의 페이드/로딩 UI를 초기 상태로 정렬합니다.
    /// </summary>
    public void Init()
    {
        // Presenter가 연결되지 않았으면 초기화할 대상이 없으므로 종료합니다.
        if (_sceneTransition == null)
        {
            return;
        }

        _sceneTransition.Init();
    }

    //==============================================================================
    // 공개 API (IStageFlowService)
    //==============================================================================

    /// <summary>
    /// 지정한 스테이지 번호로 전환합니다. (카탈로그 조회 → 페이드 인 → 언로드 → Additive 로드 → 데이터 복구 → 페이드 아웃)
    /// </summary>
    /// <param name="stageNumber">전환할 스테이지 번호입니다. 카탈로그에서 실제 씬 이름을 조회합니다.</param>
    /// <param name="data">다음 씬으로 전달/보존할 데이터입니다.</param>
    /// <returns>전환 파이프라인 완료를 await 할 수 있는 UniTask입니다.</returns>
    public async UniTask ChangeStageAsync(int stageNumber, SceneTransferData data)
    {
        // 음수 스테이지 번호는 잘못된 입력이므로 안전하게 차단합니다.
        if (stageNumber < 0)
        {
            Debug.LogWarning($"[StageFlowController] 잘못된 스테이지 번호입니다: {stageNumber}");
            return;
        }

        // 씬 카탈로그가 연결되지 않으면 실제 씬 이름을 찾을 수 없으므로 전환을 중단합니다.
        if (_sceneCatalog == null)
        {
            Debug.LogWarning("[StageFlowController] SceneCatalogSO가 연결되지 않았습니다.");
            return;
        }

        // 스테이지 번호로 실제 씬 엔트리를 조회합니다.
        if (!_sceneCatalog.TryGetByStageNumber(stageNumber, out SceneCatalogSO.SceneEntry entry))
        {
            Debug.LogWarning($"[StageFlowController] 등록되지 않은 스테이지 번호입니다: {stageNumber}");
            return;
        }

        // 오브젝트 파괴 시 비동기 흐름이 함께 취소되도록 토큰을 연동합니다.
        CancellationToken token = this.GetCancellationTokenOnDestroy();

        // 스테이지는 상시 씬을 유지한 채 Additive 방식으로 전환합니다.
        await RunTransitionAsync(entry.SceneName, LoadSceneMode.Additive, data, stageNumber, token);
    }

    /// <summary>
    /// 등록된 씬 ID를 기준으로 일반 씬(타이틀/로비/엔딩 등)으로 전환합니다.
    /// </summary>
    /// <param name="sceneId">씬 카탈로그에 등록된 안정적인 씬 ID입니다. (예: "TITLE")</param>
    /// <param name="data">다음 씬으로 전달/보존할 데이터입니다. 필요 없으면 null을 전달합니다.</param>
    /// <returns>전환 파이프라인 완료를 await 할 수 있는 UniTask입니다.</returns>
    public async UniTask ChangeSceneAsync(string sceneId, SceneTransferData data)
    {
        // 비어 있는 씬 ID는 조회할 수 없으므로 안전하게 차단합니다.
        if (string.IsNullOrWhiteSpace(sceneId))
        {
            Debug.LogWarning("[StageFlowController] 비어 있는 씬 ID로 전환할 수 없습니다.");
            return;
        }

        // 씬 카탈로그가 연결되지 않으면 실제 씬 이름을 찾을 수 없으므로 전환을 중단합니다.
        if (_sceneCatalog == null)
        {
            Debug.LogWarning("[StageFlowController] SceneCatalogSO가 연결되지 않았습니다.");
            return;
        }

        // 안정적인 씬 ID로 실제 씬 엔트리를 조회합니다.
        if (!_sceneCatalog.TryGetById(sceneId, out SceneCatalogSO.SceneEntry entry))
        {
            Debug.LogWarning($"[StageFlowController] 등록되지 않은 씬 ID입니다: {sceneId}");
            return;
        }

        // 오브젝트 파괴 시 비동기 흐름이 함께 취소되도록 토큰을 연동합니다.
        CancellationToken token = this.GetCancellationTokenOnDestroy();

        // 타이틀/로비/엔딩 같은 일반 씬은 Single 방식으로 전체 교체합니다.
        await RunTransitionAsync(entry.SceneName, LoadSceneMode.Single, data, -1, token);
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 씬 로드 방식에 맞춰 공통 씬 전환 파이프라인을 실행합니다.
    /// </summary>
    /// <param name="sceneName">Unity에 등록된 실제 씬 이름입니다.</param>
    /// <param name="loadMode">씬 로드 방식입니다. 스테이지는 Additive, 일반 씬은 Single을 사용합니다.</param>
    /// <param name="data">복구에 사용할 전달 데이터입니다.</param>
    /// <param name="destinationStageNumber">Additive 전환 시 목적지 스테이지 번호입니다. 해당 없으면 -1입니다.</param>
    /// <param name="token">취소 토큰입니다.</param>
    private async UniTask RunTransitionAsync(
        string sceneName,
        LoadSceneMode loadMode,
        SceneTransferData data,
        int destinationStageNumber,
        CancellationToken token)
    {
        // 전환이 이미 진행 중이면 중복 호출을 막아 경쟁 상태를 방지합니다.
        if (_isTransitioning)
        {
            return;
        }

        // 비어 있는 씬 이름은 Unity 로드 요청이 실패하므로 사전에 차단합니다.
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[StageFlowController] 비어 있는 씬 이름으로 전환할 수 없습니다.");
            return;
        }

        // Presenter가 연결되지 않았으면 시각 연출 없이 전환할 수 없으므로 중단합니다.
        if (_sceneTransition == null)
        {
            Debug.LogWarning("[StageFlowController] SceneTransitionPresenter가 연결되지 않았습니다.");
            return;
        }

        IsTransitioning = true;

        try
        {
            // 1) 화면 암전: alpha 0 → 1 (로딩 UI는 아직 비활성).
            await _sceneTransition.FadeInAsync(token);

            // 2) Additive 스테이지 전환: 떠나기 전 현재 스테이지 상태를 Capture합니다.
            if (loadMode == LoadSceneMode.Additive)
            {
                ExecuteStageCapture();
                await UnloadActiveSceneIfNeededAsync(token);
            }

            // 3) 새 씬 로드: AsyncOperation을 Presenter가 모니터링하며 로딩 UI를 단계적으로 노출합니다.
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, loadMode);
            await _sceneTransition.MonitorTransitionAsync(loadOperation, token);

            // Additive 모드라면 방금 로드한 씬을 액티브 씬으로 지정합니다.
            if (loadMode == LoadSceneMode.Additive)
            {
                SetActiveSceneIfLoaded(sceneName);
            }

            // 4) Additive 전환 시 현재 스테이지 번호를 갱신합니다.
            if (loadMode == LoadSceneMode.Additive && destinationStageNumber >= 0)
            {
                _currentStageNumber = destinationStageNumber;
            }

            // 5) 데이터 복구: 새 씬이 완전히 로드된 직후 복구 절차를 실행합니다.
            ExecuteDataRestoration(data, destinationStageNumber);

            // 6) 화면 복귀: alpha 1 → 0.
            await _sceneTransition.FadeOutAsync(token);
        }
        finally
        {
            // 예외/취소 여부와 무관하게 전환 플래그를 반드시 해제합니다.
            IsTransitioning = false;
        }
    }

    /// <summary>
    /// 현재 액티브 씬이 상시 씬이 아니라면 비동기로 언로드합니다.
    /// </summary>
    /// <param name="token">취소 토큰입니다.</param>
    private async UniTask UnloadActiveSceneIfNeededAsync(CancellationToken token)
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

        // 이전 액티브 씬을 비동기로 내리고, 긴 언로드 시 Presenter가 로딩 UI를 표시합니다.
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(activeScene);
        await _sceneTransition.MonitorTransitionAsync(unloadOperation, token);
    }

    /// <summary>
    /// 지정한 이름의 씬이 로드되어 있으면 액티브 씬으로 설정합니다.
    /// </summary>
    /// <param name="sceneName">액티브로 지정할 씬 이름입니다.</param>
    private void SetActiveSceneIfLoaded(string sceneName)
    {
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);

        // 정상적으로 로드되었을 때만 액티브 씬으로 전환합니다. (라이팅/인스턴스 기준 씬)
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
        }
    }

    /// <summary>
    /// 스테이지 언로드 직전 현재 스테이지 상태 Capture 절차를 실행합니다.
    /// </summary>
    private void ExecuteStageCapture()
    {
        // 현재 스테이지가 없으면 Capture할 대상이 없습니다.
        if (_currentStageNumber < 0)
        {
            return;
        }

        OnStageCaptureRequested?.Invoke(_currentStageNumber);
    }

    /// <summary>
    /// 새 스테이지 로드 직후 데이터 복구 절차를 실행합니다.
    /// </summary>
    /// <param name="data">복구에 사용할 전달 데이터입니다.</param>
    /// <param name="destinationStageNumber">목적지 스테이지 번호입니다.</param>
    private void ExecuteDataRestoration(SceneTransferData data, int destinationStageNumber)
    {
        // Additive 스테이지 전환이 아니면 Restore 이벤트를 발생시키지 않습니다.
        if (destinationStageNumber < 0)
        {
            return;
        }

        // handoff 데이터가 없으면 목적지 스테이지 번호만 담은 임시 데이터를 생성합니다.
        if (data == null)
        {
            data = new SceneTransferData();
        }

        // enteringStageNumber가 비어 있으면 목적지 스테이지 번호로 채웁니다.
        if (data.enteringStageNumber < 0)
        {
            data.enteringStageNumber = destinationStageNumber;
        }

        // 실제 복구 로직은 본 컨트롤러의 책임이 아닙니다.
        // 결합도를 낮추기 위해 구독자(SceneStateBridge 등)에게 이벤트로 위임합니다.
        OnStageRestoreRequested?.Invoke(data);
    }
}
