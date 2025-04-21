using UnityEngine;

public static class DamageCalculator
{
    /// <summary>
    /// 공격자와 피격자의 스탯을 기반으로 데미지를 계산하여 적용합니다.
    /// </summary>
    public static void DamageCalculate(CharacterBase attacker, CharacterBase defender)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("공격자 또는 피격자가 null입니다.");
            return;
        }

        double attack = attacker.stats.ATK;
        double defense = defender.stats.DEF;

        const double reductionRate = 0.01; // DEF 1당 1% 감소
        const double minRate = 0.2;        // 최소 데미지 비율 20%

        double reduction = defense * reductionRate;
        double finalRate = Mathf.Max(1f - (float)reduction, (float)minRate); // float으로 클램프 처리

        double damage = attack * finalRate;
        damage = Mathf.Max((float)damage, 1f); // 최소 1 데미지
        defender.ApplyDamage(damage);
    }
}
