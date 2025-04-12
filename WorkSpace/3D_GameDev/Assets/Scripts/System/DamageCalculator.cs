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

        double attack = attacker.stats.attack;
        double defense = defender.stats.def;

        double damage = attack * (100.0 / (100.0 + defense));
        damage = Mathf.Max((float)damage, 1f);
        defender.ApplyDamage(damage);
    }
}
