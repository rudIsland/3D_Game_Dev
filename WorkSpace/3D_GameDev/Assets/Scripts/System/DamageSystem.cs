using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DamageSystem
{
    /// <summary>
    /// �����ڿ� �ǰ����� ������ ������� �������� ���
    /// </summary>
    public static void ApplyDamage(CharacterStatsComponent attacker, CharacterStatsComponent defender)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("������ �Ǵ� �ǰ��ڰ� null�Դϴ�.");
            return;
        }

        double attack = attacker.stats.attack;
        double defense = defender.stats.def;

        // ���ݷ� * (100/100*����)
        double damage = attack * (100.0 / (100.0 + defense));

        // �ּ� ������ 1
        damage = Mathf.Max((float)damage, 1f);

        // ������ ����
        defender.TakeDamage(damage);
    }
}
