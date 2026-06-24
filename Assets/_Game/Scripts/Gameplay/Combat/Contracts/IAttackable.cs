using UnityEngine;

public interface IAttackable
{
    /// <param name="attacker">시전자(GameObject)</param>
    /// <param name="damage">데미지 수치</param>
    /// <param name="direction">공격 방향</param>
    void ExecuteAttack(GameObject attacker, int damage, Vector3 direction);
}