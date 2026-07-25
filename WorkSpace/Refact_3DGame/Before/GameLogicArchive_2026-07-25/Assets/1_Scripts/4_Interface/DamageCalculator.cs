using UnityEngine;

public static class DamageCalculator
{
    private const float DefenseConstant = 200f;

    // 플레이어가 공격할 때 (기존)
    public static DamageInfo CalculatePlayerDamage(PlayerStateMachine attacker, IDamageable victim)
    {
        return ProcessCalculation(attacker.attack, attacker.gameObject, victim);
    }

    // 적이 공격할 때 (추가)
    public static DamageInfo CalculateEnemyDamage(Enemy attacker, int weaponIndex, IDamageable victim)
    {
        // 적의 특정 무기/패턴 데미지를 가져옴
        float enemyAtk = attacker.GetCurrentAttackDamage(weaponIndex);
        return ProcessCalculation(enemyAtk, attacker.gameObject, victim);
    }

    // 실제 계산 로직 (공용)
    private static DamageInfo ProcessCalculation(float atk, GameObject attackerObj, IDamageable victim)
    {
        float targetDef = victim.defense;
        float finalAmount = atk * (DefenseConstant / (DefenseConstant + targetDef));

        return new DamageInfo(Mathf.Max(1, finalAmount), attackerObj);
    }
}