using System;
using UnityEngine;

namespace rudIsland.RPG3D.Combat
{
    [Serializable]
    // 1부터 시작하는 플레이어 공격 번호와 피해를 함께 보관한다.
    public struct PlayerAttackDamage
    {
        [SerializeField, Range(1, 6)] private int attackNumber;
        [SerializeField] private AttackDamage damage;

        public int AttackNumber => attackNumber;
        public AttackDamage Damage => damage;

        public PlayerAttackDamage(int attackNumber, AttackDamage damage)
        {
            this.attackNumber = attackNumber;
            this.damage = damage;
        }

        public static bool TryGetDamage(
            PlayerAttackDamage[] attackDamages,
            int attackNumber,
            out AttackDamage damage)
        {
            if (attackDamages != null)
            {
                for (int index = 0; index < attackDamages.Length; index++)
                {
                    if (attackDamages[index].attackNumber != attackNumber)
                    {
                        continue;
                    }

                    damage = attackDamages[index].damage;
                    return damage.IsValid;
                }
            }

            damage = default;
            return false;
        }

        public static bool HasDuplicateAttackNumber(
            PlayerAttackDamage[] attackDamages)
        {
            if (attackDamages == null)
            {
                return false;
            }

            for (int first = 0; first < attackDamages.Length; first++)
            {
                for (int second = first + 1;
                    second < attackDamages.Length;
                    second++)
                {
                    if (attackDamages[first].attackNumber ==
                        attackDamages[second].attackNumber)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
