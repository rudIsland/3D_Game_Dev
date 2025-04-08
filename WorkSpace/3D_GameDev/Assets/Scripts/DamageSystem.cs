using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DamageSystem
{
    /// <summary>
    /// 공격자와 피격자의 스탯을 기반으로 데미지를 계산하고 적용합니다. (크리티컬 없음)
    /// </summary>
    public static void ApplyDamage(CharacterStatsComponent attacker, CharacterStatsComponent defender)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("공격자 또는 피격자가 null입니다.");
            return;
        }

        double attack = attacker.stats.Attack;
        double defense = defender.stats.Dex;

        // RPG 스타일 데미지 공식
        double damage = attack * (100.0 / (100.0 + defense));

        // 최소 데미지 1 보장
        damage = Mathf.Max((float)damage, 1f);

        // 데미지 적용
        defender.TakeDamage(damage);
        
    }
}
