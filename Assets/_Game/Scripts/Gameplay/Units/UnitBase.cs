using UnityEngine;


//==============================================================================
// class::UnitBase
// Player와 Enemy가 상속받을 공통 기능 부모 클래스.
//==============================================================================
// TODO: TakeDamage, Dead 작성해야함.
//
//==============================================================================
public class UnitBase : MonoBehaviour, IKillable, IDamageable, IHealth
{
    //==============================================================================
    // 변수
    //============================================================================== 
    [Header("Unit")]
    [Tooltip("유닛의 사망 판정")]
    [SerializeField] private bool _isAlive;
    public bool IsAlive { get => _isAlive; }

    [Header("Unit")]
    [Tooltip("유닛의 최대체력")]
    [SerializeField] private float _maxHealth;
    public float MaxHealth { get => _maxHealth; }

    [Header("Unit")]
    [Tooltip("유닛의 현재체력")]
    [SerializeField] private float _currentHealth;
    public float CurrentHealth { get => _currentHealth; }

    //==============================================================================
    // 함수
    //============================================================================== 
    // 유닛의 데미지 판정을 담당합니다.
    virtual public void TakeDamage(float damage)
    {
        // 사망했으면 리턴
        if (!_isAlive) return;
        // 데미지가 0이하면(음수) 리턴
        if (damage < 0.0f) return;

        // 데미지 입력(방어 스탯 추가 시 방어력 계산 후 적용)
        float resultHealth = _currentHealth - damage;
        // 체력이 0이면 Die호출
        if (resultHealth <= 0.0f) Die();

        // 체력 반영
        _currentHealth = resultHealth;
        Debug.Log("공격받음");
    }

    // 유닛의 사망판정을 담당합니다
    virtual public void Die()
    {
        // 이미 죽어있다면 리턴
        if (!_isAlive) return;

        // 체력 0으로 초기화 및 죽음 활성화
        _currentHealth = 0.0f;
        _isAlive = false;
    }

}
