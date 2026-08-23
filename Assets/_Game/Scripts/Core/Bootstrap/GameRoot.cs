using UnityEngine;

//==============================================================================
// 기능: class::GameRoot
// 설명: 전역 컨트롤러의 참조와 초기화 순서를 관리하는 프로젝트 유일 싱글턴
//==============================================================================
public sealed class GameRoot : Singleton<GameRoot>
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Controller")]
    [Tooltip("카메라 이동과 추적을 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private CameraController _cameraController;

    [Tooltip("CSV TextAsset을 읽어 게임 데이터 저장소를 생성하는 컨트롤러입니다.")]
    [SerializeField]
    private GameDataController _gameDataController;

    [Tooltip("게임 진행 상태와 플래그를 보관하는 컨트롤러입니다.")]
    [SerializeField]
    private ProgressController _progressController;

    [Tooltip("퀘스트 상태 전환과 목표 및 보상 처리를 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private QuestController _questController;

    [Tooltip("아이템 보관과 중첩 및 소모를 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private InventoryController _inventoryController;

    [Tooltip("인벤토리 UI와 선택 입력을 중개하는 Presenter입니다.")]
    [SerializeField]
    private InventoryPresenter _inventoryPresenter;

    [Tooltip("NPC 대화 그래프 진행과 대화 UI를 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private DialogueController _dialogueController;

    [Tooltip("씬과 스테이지 전환 흐름을 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private StageFlowController _stageFlowController;

    [Tooltip("씬 상태 Capture 및 Restore 이벤트를 진행 상태와 연결하는 브릿지입니다.")]
    [SerializeField]
    private SceneStateBridge _sceneStateBridge;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>카메라 이동과 추적을 관리하는 컨트롤러입니다.</summary>
    public CameraController CameraController => _cameraController;

    /// <summary>CSV에서 초기화된 읽기 전용 게임 데이터 저장소입니다.</summary>
    public IGameDataRepository GameData => _gameDataController != null
        ? _gameDataController.Repository
        : null;

    /// <summary>CSV 문장 Key를 현재 언어 문자열로 변환하는 계약입니다.</summary>
    public ILocalizationService Localization => _gameDataController;

    /// <summary>게임 진행 상태 시스템의 외부 공개 계약입니다.</summary>
    public IProgressService Progress => _progressController;

    /// <summary>퀘스트 시스템의 외부 공개 계약입니다.</summary>
    public IQuestService Quest => _questController;

    /// <summary>인벤토리 시스템의 외부 공개 계약입니다.</summary>
    public IInventoryService Inventory => _inventoryController;

    /// <summary>대화 시스템의 외부 공개 계약입니다.</summary>
    public IDialogueService Dialogue => _dialogueController;

    /// <summary>스테이지 전환 시스템의 외부 공개 계약입니다.</summary>
    public IStageFlowService StageFlow => _stageFlowController;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 게임 데이터와 전역 컨트롤러를 의존 순서에 맞춰 초기화합니다.
    /// </summary>
    private void Start()
    {
        if (!TryInitializeGameData())
        {
            return;
        }

        if (_progressController != null)
        {
            _progressController.Init();
        }

        if (_inventoryController != null)
        {
            _inventoryController.Init();
        }

        if (_inventoryPresenter != null)
        {
            _inventoryPresenter.Init(_inventoryController);
        }

        if (_questController != null)
        {
            _questController.Init(GameData, _progressController, _inventoryController);
        }

        if (_dialogueController != null)
        {
            _dialogueController.Init(
                GameData,
                _gameDataController,
                _progressController,
                _questController);
        }

        if (_stageFlowController == null)
        {
            Debug.LogWarning("[GameRoot] StageFlowController가 연결되지 않았습니다.");
            return;
        }

        _stageFlowController.Init();

        if (_sceneStateBridge != null && _progressController != null)
        {
            _sceneStateBridge.Init(_stageFlowController, _progressController);
        }

        if (_cameraController == null)
        {
            Debug.LogWarning("[GameRoot] CameraController가 연결되지 않았습니다.");
            return;
        }

        _cameraController.Init();
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// CSV 게임 데이터 컨트롤러를 가장 먼저 초기화합니다.
    /// </summary>
    /// <returns>검증된 게임 데이터 저장소를 생성했으면 true입니다.</returns>
    private bool TryInitializeGameData()
    {
        if (_gameDataController == null)
        {
            Debug.LogError("[GameRoot] GameDataController가 연결되지 않았습니다.");
            return false;
        }

        if (_gameDataController.TryInit(out GameDataCsvLoadError loadError))
        {
            return true;
        }

        string errorMessage = loadError != null
            ? loadError.ToString()
            : "알 수 없는 CSV 게임 데이터 초기화 오류입니다.";
        Debug.LogError($"[GameRoot] 게임 데이터 초기화에 실패했습니다. {errorMessage}");
        return false;
    }
}
