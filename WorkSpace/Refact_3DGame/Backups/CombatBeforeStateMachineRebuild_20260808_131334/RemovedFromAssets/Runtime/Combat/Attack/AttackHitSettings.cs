using System;
using rudIsland.RPG3D.Combat.Detection;
using UnityEngine;

namespace rudIsland.RPG3D.Combat.Attack
{
    [Serializable]
    // 공격 번호마다 사용할 Detector와 피해량만 보관한다.
    public struct AttackHitSettings
    {
        [SerializeField, Min(1)] private int attackNumber;
        [SerializeField] private MeleeHitDetector hitDetector;
        [SerializeField] private AttackDamage damage;

        public int AttackNumber => attackNumber;
        public MeleeHitDetector HitDetector => hitDetector;
        public AttackDamage Damage => damage;

        public AttackHitSettings(
            int attackNumber,
            MeleeHitDetector hitDetector,
            AttackDamage damage)
        {
            this.attackNumber = Mathf.Max(1, attackNumber);
            this.hitDetector = hitDetector;
            this.damage = damage;
        }

        public static bool TryFind(
            AttackHitSettings[] settings,
            int attackNumber,
            out AttackHitSettings foundSettings)
        {
            if (settings != null)
            {
                for (int index = 0; index < settings.Length; index++)
                {
                    if (settings[index].attackNumber != attackNumber)
                    {
                        continue;
                    }

                    foundSettings = settings[index];
                    return foundSettings.damage.IsValid;
                }
            }

            foundSettings = default;
            return false;
        }

        public static bool HasDuplicateAttackNumber(
            AttackHitSettings[] settings)
        {
            if (settings == null)
            {
                return false;
            }

            for (int first = 0; first < settings.Length; first++)
            {
                for (int second = first + 1; second < settings.Length; second++)
                {
                    if (settings[first].attackNumber ==
                        settings[second].attackNumber)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}