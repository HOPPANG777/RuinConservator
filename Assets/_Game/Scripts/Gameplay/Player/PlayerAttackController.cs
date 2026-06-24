using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 설명: 플레이어 공격

public class PlayerAttackController : MonoBehaviour, IAttackable
{
    #region Fields, Properties

    [Header("Input Settings")]
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
    [Tooltip("기본 공격 데미지")]
    [SerializeField] private int _baseDamage = 10;

    [Header("Layer Settings")]
    [Tooltip("타격 대상 레이어 (Enemy)")]
    [SerializeField] private LayerMask _targetLayer;
    [Tooltip("장애물 레이어 (Wall)")]
    [SerializeField] private LayerMask _wallLayer;

    private PlayerMoveController _moveController;
    private Vector3 _lastLookDir = Vector3.right; // 마지막 방향

    private float _coolTimeElapsed;
    private float _activeElapsed;
    private bool _isAttacking;

    private Vector3 _currentAttackDir;
    private int _currentDamage;

    public bool CanAttack => _coolTimeElapsed >= _coolTime;

    private HashSet<GameObject> _hitTargets = new HashSet<GameObject>();

    #endregion

    #region Unity Lifecycle

    public void Construct(PlayerMoveController moveController)
    {
        _moveController = moveController;
    }

    private void OnEnable() => _attackAction?.action.Enable();
    private void OnDisable() => _attackAction?.action.Disable();

    private void Update()
    {
        HandleInput();
        UpdateTimers();
    }

    #endregion

    #region Logic

    private void HandleInput()
    {
        // 마지막 이동 방향 기억
        if (_moveController != null && _moveController.MoveDirection != Vector3.zero)
        {
            _lastLookDir = _moveController.MoveDirection.normalized;
        }

        // 공격 입력 처리
        if (_attackAction != null && _attackAction.action.triggered)
        {
            if (CanAttack)
            {
                ExecuteAttack(this.gameObject, _baseDamage, _lastLookDir);
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

            // 매 프레임 체크하지만 PerformDetection 내부에서 중복을 걸러냄
            PerformDetection();

            if (_activeElapsed >= _activeDuration)
                StopAttack();
        }
    }

    public void ExecuteAttack(GameObject attacker, int damage, Vector3 direction)
    {
        _currentDamage = damage;
        _currentAttackDir = direction.normalized;

        _isAttacking = true;
        _coolTimeElapsed = 0f;
        _activeElapsed = 0f;

        // 공격 시작 시 타격 명단 초기화
        _hitTargets.Clear();

        Debug.Log($"{attacker.name} - 공격 개시");
    }

    private void PerformDetection()
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, _attackRadius, _targetLayer);

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject targetObj = targets[i].gameObject;

            // 이미 이번 공격에 맞은 적이라면 스킵
            if (_hitTargets.Contains(targetObj)) continue;
            if (targetObj == gameObject) continue;

            Vector3 targetPos = targetObj.transform.position;
            Vector3 dirToTarget = (targetPos - transform.position).normalized;

            float angle = Vector3.Angle(_currentAttackDir, dirToTarget);
            if (angle <= _attackAngle * 0.5f)
            {
                float distToTarget = Vector2.Distance(transform.position, targetPos);

                RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToTarget, distToTarget, _wallLayer);
                if (hit.collider == null)
                {
                    // 2. 데미지 적용 후 타격 명단에 추가
                    IDamageable damageable = targetObj.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(_currentDamage);
                        _hitTargets.Add(targetObj); // 명단에 등록
                        Debug.Log($"{targetObj.name} 검출");
                    }
                }
            }
        }
    }

    private void StopAttack()
    {
        _isAttacking = false;
        _hitTargets.Clear(); // 공격 종료 시 명단 비우기
    }

    #endregion

    #region 기즈모

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isAttacking ? Color.red : Color.cyan;
        Vector3 baseDir = _isAttacking ? _currentAttackDir : _lastLookDir;
        Vector3 center = transform.position;
        float halfAngle = _attackAngle * 0.5f;

        Vector3 leftBoundary = Quaternion.Euler(0, 0, -halfAngle) * baseDir;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, halfAngle) * baseDir;

        Gizmos.DrawLine(center, center + leftBoundary * _attackRadius);
        Gizmos.DrawLine(center, center + rightBoundary * _attackRadius);

        // 호
        int segments = 10;
        Vector3 prevPoint = center + leftBoundary * _attackRadius;
        for (int i = 1; i <= segments; i++)
        {
            float curAngle = -halfAngle + (_attackAngle / segments) * i;
            Vector3 nextPoint = center + (Quaternion.Euler(0, 0, curAngle) * baseDir) * _attackRadius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    #endregion
}