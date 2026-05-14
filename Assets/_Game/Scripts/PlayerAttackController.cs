using UnityEngine;
using UnityEngine.InputSystem; // New Input System 네임스페이스

// 기능: PlayerAttackController
// 설명: InputActionReference를 사용하여 공격 입력을 처리
public class PlayerAttackController : MonoBehaviour, IAttackable
{
    #region Fields, Properties

    [Header("Input Settings")]
    [Tooltip("공격 입력")]
    [SerializeField] private InputActionReference _attackAction;

    [Header("Attack Settings")]
    [Tooltip("공격 반지름")]
    [SerializeField] private float _attackRadius = 0.7f;
    [Tooltip("부채꼴 각도")]
    [SerializeField] private float _attackAngle = 90f;
    [Tooltip("공격 판정 유지 시간")]
    [SerializeField] private float _activeDuration = 0.3f;
    [Tooltip("다음 공격 가능 시간")]
    [SerializeField] private float _coolTime = 0.6f;

    // todo : 데미지는 player 한테받는걸로??
    [Tooltip("기본 공격 데미지")]
    [SerializeField] private int _baseDamage = 10;

    [Header("Layer Settings")]
    [Tooltip("타격 대상 레이어")]
    [SerializeField] private LayerMask _targetLayer;
    [Tooltip("장애물 레이어")]
    [SerializeField] private LayerMask _wallLayer;

    private PlayerMoveController _moveController;

    // 상태 변수
    private float _coolTimeElapsed;
    private float _activeElapsed;
    private bool _isAttacking;

    private Vector3 _currentAttackDir;
    private int _currentDamage;

    public bool CanAttack => _coolTimeElapsed >= _coolTime;

    #endregion

    #region Unity

    public void Construct(PlayerMoveController moveController)
    {
        _moveController = moveController;
    }

    private void OnEnable()
    {
        if (_attackAction != null)
            _attackAction.action.Enable();
    }

    private void OnDisable()
    {
        if (_attackAction != null)
            _attackAction.action.Disable();
    }

    private void Update()
    {
        HandleInput();
        UpdateTimers();
    }

    #endregion

    #region Private

    private void HandleInput()
    {
        // InputActionReference를 통한 입력 감지
        if (_attackAction != null && _attackAction.action.triggered)
        {
            if (CanAttack)
            {
                Vector3 attackDir = _moveController.MoveDirection;
                if (attackDir == Vector3.zero) attackDir = transform.forward;

                ExecuteAttack(this.gameObject, _baseDamage, attackDir);
            }
        }
    }

    private void UpdateTimers()
    {
        if (_coolTimeElapsed < _coolTime)
            _coolTimeElapsed += Time.deltaTime;

        if (_isAttacking)
        {
            _activeElapsed += Time.deltaTime;
            PerformDetection();

            if (_activeElapsed >= _activeDuration)
                StopAttack();
        }
    }

    #endregion

    #region Attack Method

    public void ExecuteAttack(GameObject attacker, int damage, Vector3 direction)
    {
        _currentDamage = damage;
        _currentAttackDir = direction.normalized;

        _isAttacking = true;
        _coolTimeElapsed = 0f;
        _activeElapsed = 0f;

        Debug.Log($"Attack시전자 - {attacker.name}");
    }

    private void PerformDetection()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, _attackRadius, _targetLayer);

        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 targetPos = targets[i].transform.position;
            Vector3 dirToTarget = (targetPos - transform.position).normalized;

            if (Vector3.Angle(_currentAttackDir, dirToTarget) <= _attackAngle * 0.5f)
            {
                float distToTarget = Vector3.Distance(transform.position, targetPos);
                if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, _wallLayer))
                {
                    targets[i].GetComponent<IDamageable>()?.TakeDamage(_currentDamage);
                }
            }
        }
    }

    private void StopAttack()
    {
        _isAttacking = false;
    }
    #endregion
}