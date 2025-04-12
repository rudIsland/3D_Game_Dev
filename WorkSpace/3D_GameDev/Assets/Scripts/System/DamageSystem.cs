using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DamageSystem
{
    /// <summary>
    /// 공격자와 피격자의 스탯을 기반으로 데미지를 계산
    /// </summary>
    public static void ApplyDamage(CharacterStatsComponent attacker, CharacterStatsComponent defender)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("공격자 또는 피격자가 null입니다.");
            return;
        }

        double attack = attacker.stats.attack;
        double defense = defender.stats.def;

        // 공격력 * (100/100*방어력)
        double damage = attack * (100.0 / (100.0 + defense));

        // 최소 데미지 1
        damage = Mathf.Max((float)damage, 1f);

        // 데미지 적용
        defender.TakeDamage(damage);
    }
}
