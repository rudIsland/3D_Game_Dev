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

        const double reductionRate = 0.01; // DEF 1�� 1% ����
        const double minRate = 0.2;        // �ּ� ������ ���� 20%

        double reduction = defense * reductionRate;
        double finalRate = Mathf.Max(1f - (float)reduction, (float)minRate); // float���� Ŭ���� ó��

        double damage = attack * finalRate;
        damage = Mathf.Max((float)damage, 1f); // �ּ� 1 ������
        defender.ApplyDamage(damage);
    }
}
