using UnityEngine;



//==============================================================================

// 기능: class::GameRoot

// 설명: 현재 프로젝트 상에 하나 만 존재하는 매니저

//==============================================================================

// TODO: GameRoot에 들어가는 컨트롤러는 우리가 원래 매니저 라고 생각하는 컨트롤러들이 들어갑니다.

// 예: SoundController, MapController, PoolController등

//==============================================================================

public class GameRoot : Singleton<GameRoot>

{

    //==============================================================================

    // 변수

    //============================================================================== 

    [Header("Controller")]

    [Tooltip("카메라 컨트롤러입니다.")]

    [SerializeField] private CameraController _cameraController;

    public CameraController CameraController { get => _cameraController; }



    [Header("Controller")]

    [Tooltip("게임 진행 상태(플래그, 씬 오브젝트 상태)를 보관하는 컨트롤러입니다.")]

    [SerializeField] private ProgressController _progressController;

    // 외부에는 구현체가 아닌 인터페이스(IProgressService) 계약만 노출합니다.

    public IProgressService Progress { get => _progressController; }



    [Header("Controller")]

    [Tooltip("NPC 대화 그래프 진행과 대화 UI를 관리하는 컨트롤러입니다.")]

    [SerializeField] private DialogueController _dialogueController;

    // 외부에는 구현체가 아닌 인터페이스(IDialogueService) 계약만 노출합니다.

    public IDialogueService Dialogue { get => _dialogueController; }



    [Header("Controller")]

    [Tooltip("씬/스테이지 전환 흐름과 페이드 연출을 담당하는 컨트롤러입니다.")]

    [SerializeField] private StageFlowController _stageFlowController;

    // 외부에는 구현체가 아닌 인터페이스(IStageFlowService) 계약만 노출합니다.

    public IStageFlowService StageFlow { get => _stageFlowController; }



    [Header("Controller")]

    [Tooltip("StageFlow Capture/Restore 이벤트와 Progress, 씬 오브젝트를 중개합니다.")]

    [SerializeField] private SceneStateBridge _sceneStateBridge;



    //==============================================================================

    // 함수

    //============================================================================== 

    private void Start()

    {

        // 진행 상태 컨트롤러를 초기화합니다.

        if (_progressController != null)

        {

            _progressController.Init();

        }



        // 대화 컨트롤러를 초기화합니다.

        if (_dialogueController != null)

        {

            _dialogueController.Init();

        }



        // 스테이지 플로우 컨트롤러가 있으면 페이드/로딩 UI를 초기 상태로 정렬합니다.

        if (_stageFlowController == null)

        {

            Debug.Log("StageFlowController가 없습니다.");

            return;

        }



        _stageFlowController.Init();



        // Capture/Restore 중개 브릿지를 연결합니다.

        if (_sceneStateBridge != null && _progressController != null)

        {

            _sceneStateBridge.Init(_stageFlowController, _progressController);

        }



        // 카메라 컨트롤러가 없으면 카메라 초기화만 건너뜁니다.

        if (_cameraController == null)

        {

            Debug.Log("CameraController가 없습니다.");

            return;

        }



        // 카메라 컨트롤러를 초기화합니다.

        _cameraController.Init();

    }



}

