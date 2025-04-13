using UnityEngine;

public static class DamageCalculator
{
    /// <summary>
    /// �����ڿ� �ǰ����� ������ ������� �������� ����Ͽ� �����մϴ�.
    /// </summary>
    public static void DamageCalculate(CharacterBase attacker, CharacterBase defender)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("������ �Ǵ� �ǰ��ڰ� null�Դϴ�.");
            return;
        }

        double attack = attacker.stats.ATK;
        double defense = defender.stats.DEF;

        double damage = attack * (100.0 / (100.0 + defense));
        damage = Mathf.Max((float)damage, 1f);
        defender.ApplyDamage(damage);
    }
}
