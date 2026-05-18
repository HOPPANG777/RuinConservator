using UnityEditor.EditorTools;
using UnityEngine;

public class GameRoot : Singleton<GameRoot>
{

    [Header("Controller")]
    [Tooltip("카메라 컨트롤러입니다.")]
    [SerializeField] private CameraController _cameraController;
    public CameraController CameraController { get => _cameraController; }

    void Start()
    {
        if (_cameraController == null) Debug.Log("CameraController가 없습니다."); return;

        _cameraController.Init();

    }

}