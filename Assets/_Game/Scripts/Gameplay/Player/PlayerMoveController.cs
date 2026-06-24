using UnityEngine;
using UnityEngine.InputSystem;

//==============================================================================
// class::PlayerMoveController
// 플레이어의 이동을 담당한다.
//==============================================================================
// TODO: IMoveable로 의존성 주입 구현.
//
//==============================================================================
public class PlayerMoveController : MonoBehaviour, IMoveable
{
    //==============================================================================
    // 변수
    //============================================================================== 
    [Header("Move")]
    [Tooltip("플레이어의 이동 방향")]
    [SerializeField] private Vector3 _moveDirection;
    public Vector3 MoveDirection { get => _moveDirection; }

    [Header("Move")]
    [Tooltip("플레이어의 이동 속도")]
    [SerializeField] private float _moveSpeed;
    public float MoveSpeed { get => _moveSpeed; }

    [Header("Move")]
    [Tooltip("플레이어")]
    [SerializeField] private GameObject _player;

    [Header("Move")]
    [Tooltip("인풋")]
    [SerializeField] private InputActionReference _moveAction;

    [Header("Move")]
    [Tooltip("플레이어 이동 입력 허용 여부입니다. 대화 중에는 false로 설정합니다.")]
    [SerializeField] private bool _canMove = true;

    //==============================================================================
    // 함수
    //==============================================================================
    void Update()
    {
        Move();
    }

    private void OnEnable()
    {
        if (_moveAction != null)
        {
            _moveAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (_moveAction != null)
        {
            _moveAction.action.Disable();
        }
    }

    /// <summary>
    /// 플레이어 이동 입력 허용 여부를 설정합니다.
    /// </summary>
    /// <param name="canMove">이동 허용 여부입니다.</param>
    public void SetCanMove(bool canMove)
    {
        _canMove = canMove;

        if (_canMove == false)
        {
            _moveDirection = Vector3.zero;
        }
    }

    // 이동 로직
    public void Move()
    {
        if (_canMove == false || _moveAction == null || _player == null)
        {
            return;
        }

        // 입력
        Vector2 vec = _moveAction.action.ReadValue<Vector2>();
        _moveDirection = new Vector3(vec.x, vec.y, 0.0f);

        // 방향 벡터
        Vector3 nextVector = _moveDirection.normalized * (_moveSpeed * Time.deltaTime);

        // 이동
        _player.transform.position = _player.transform.position + nextVector;
    }
}
