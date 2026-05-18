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

    //==============================================================================
    // 함수
    //============================================================================== 
    private void Start()
    {
        // 카메라 컨트롤러가 없으면 초기화를 진행하지 않습니다.
        if (_cameraController == null)
        {
            Debug.Log("CameraController가 없습니다.");
            return;
        }

        // 카메라 컨트롤러를 초기화합니다.
        _cameraController.Init();
    }

}