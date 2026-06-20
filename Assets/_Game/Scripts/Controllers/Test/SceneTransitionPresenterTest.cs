using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

//==============================================================================
// 기능: class::SceneTransitionPresenterTest
// 설명: SceneTransitionPresenter 연출을 수동/강제로 확인하기 위한 임시 테스트 스크립트
//==============================================================================
// NOTE: 검증이 끝나면 본 스크립트와 씬/프리팹에 붙인 컴포넌트를 제거하세요.
//
//==============================================================================
public class SceneTransitionPresenterTest : MonoBehaviour
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Target")]
    [Tooltip("직접 테스트할 SceneTransitionPresenter입니다. 비어 있으면 GameRoot 하위에서 찾습니다.")]
    [SerializeField]
    private SceneTransitionPresenter _presenter;

    [Header("Presenter Test")]
    [Tooltip("Play 시작 시 Presenter 단독 테스트를 자동 실행할지 여부입니다.")]
    [SerializeField]
    private bool _runPresenterTestOnStart;

    [Tooltip("로딩 UI(0.5s/2s) 확인을 위해 인위적으로 대기할 시간(초)입니다.")]
    [SerializeField]
    private float _fakeLoadDelaySeconds = 3.0f;

    [Header("StageFlow Test")]
    [Tooltip("Play 시작 시 StageFlow ChangeSceneAsync 통합 테스트를 자동 실행할지 여부입니다.")]
    [SerializeField]
    private bool _runStageFlowTestOnStart;

    [Tooltip("StageFlow 통합 테스트에 사용할 SceneCatalog sceneId입니다.")]
    [SerializeField]
    private string _testSceneId = "TITLE";

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// Play 시작 시 인스펙터 옵션에 따라 테스트를 자동 실행합니다.
    /// </summary>
    private void Start()
    {
        if (_runPresenterTestOnStart)
        {
            RunPresenterTestAsync().Forget();
        }

        if (_runStageFlowTestOnStart)
        {
            RunStageFlowTestAsync().Forget();
        }
    }

    //==============================================================================
    // 공개 API (버튼 / ContextMenu)
    //==============================================================================

    /// <summary>
    /// Presenter만 단독으로 테스트합니다. (페이드 인 → 지연 로딩 → 페이드 아웃)
    /// </summary>
    [ContextMenu("Test/Run Presenter Test")]
    public void RunPresenterTest()
    {
        RunPresenterTestAsync().Forget();
    }

    /// <summary>
    /// GameRoot.StageFlow.ChangeSceneAsync 경로로 통합 테스트합니다.
    /// </summary>
    [ContextMenu("Test/Run StageFlow Test")]
    public void RunStageFlowTest()
    {
        RunStageFlowTestAsync().Forget();
    }

    //==============================================================================
    // 내부 테스트 로직
    //==============================================================================

    /// <summary>
    /// Presenter 단독 테스트 시퀀스를 실행합니다.
    /// </summary>
    private async UniTaskVoid RunPresenterTestAsync()
    {
        SceneTransitionPresenter presenter = ResolvePresenter();

        // Presenter를 찾지 못하면 테스트를 진행할 수 없으므로 종료합니다.
        if (presenter == null)
        {
            Debug.LogWarning("[SceneTransitionPresenterTest] SceneTransitionPresenter를 찾을 수 없습니다.");
            return;
        }

        CancellationToken token = this.GetCancellationTokenOnDestroy();

        Debug.Log("[SceneTransitionPresenterTest] Presenter 단독 테스트 시작");

        presenter.Init();
        await presenter.FadeInAsync(token);

        // allowSceneActivation=false로 로드를 붙잡아 0.5s/2s UI 단계를 강제로 확인합니다.
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        loadOperation.allowSceneActivation = false;

        UniTask monitorTask = presenter.MonitorTransitionAsync(loadOperation, token);

        await UniTask.Delay(
            System.TimeSpan.FromSeconds(_fakeLoadDelaySeconds),
            cancellationToken: token);

        loadOperation.allowSceneActivation = true;
        await monitorTask;

        await presenter.FadeOutAsync(token);

        Debug.Log("[SceneTransitionPresenterTest] Presenter 단독 테스트 완료");
    }

    /// <summary>
    /// StageFlowController 경유 통합 테스트 시퀀스를 실행합니다.
    /// </summary>
    private async UniTaskVoid RunStageFlowTestAsync()
    {
        // GameRoot가 없으면 StageFlow API를 호출할 수 없으므로 종료합니다.
        if (GameRoot.Instance == null || GameRoot.Instance.StageFlow == null)
        {
            Debug.LogWarning("[SceneTransitionPresenterTest] GameRoot 또는 StageFlow를 찾을 수 없습니다.");
            return;
        }

        CancellationToken token = this.GetCancellationTokenOnDestroy();

        Debug.Log($"[SceneTransitionPresenterTest] StageFlow 통합 테스트 시작: {_testSceneId}");

        await GameRoot.Instance.StageFlow.ChangeSceneAsync(_testSceneId, null)
            .AttachExternalCancellation(token);

        Debug.Log("[SceneTransitionPresenterTest] StageFlow 통합 테스트 완료");
    }

    /// <summary>
    /// 직렬화 필드 또는 GameRoot 하위에서 Presenter 참조를 해석합니다.
    /// </summary>
    /// <returns>사용 가능한 SceneTransitionPresenter 참조입니다.</returns>
    private SceneTransitionPresenter ResolvePresenter()
    {
        if (_presenter != null)
        {
            return _presenter;
        }

        if (GameRoot.Instance == null)
        {
            return null;
        }

        return GameRoot.Instance.GetComponentInChildren<SceneTransitionPresenter>(true);
    }
}
