using System;
using UnityEngine;

namespace rudIsland.RPG3D.Combat
{
    [Serializable]
    // 공격 번호에 맞는 판정기, 반응 세기, 피해와 밀림 거리를 보관한다.
    public struct AttackHitSettings
    {
        [SerializeField, Min(1)] private int attackNumber; // 공격 관련 설정 또는 상태
        [SerializeField] private MeleeHitDetector hitDetector; // 피격 또는 피해 관련 값
        [SerializeField] private AttackDamage damage; // 피격 또는 피해 관련 값
        [SerializeField] private HitStrength strength; // 피격 반응의 세기
        [SerializeField, Min(0f)] private float staggerDamage; // 경직 수치에 더할 값
        [SerializeField, Min(0f)] private float pushDistance; // 거리 설정

        public int AttackNumber => attackNumber; // 공격 관련 설정 또는 상태
        public MeleeHitDetector HitDetector => hitDetector; // 피격 또는 피해 관련 값
        public AttackDamage Damage => damage; // 피격 또는 피해 관련 값
        public HitStrength Strength =>
            strength >= HitStrength.Light &&
            strength <= HitStrength.Knockdown
                ? strength
                : HitStrength.Light;
        public float StaggerDamage =>
            staggerDamage > 0f &&
            !float.IsNaN(staggerDamage) &&
            !float.IsInfinity(staggerDamage)
                ? staggerDamage
                : 0f;
        public float PushDistance => // 거리 설정
            pushDistance > 0f &&
            !float.IsNaN(pushDistance) &&
            !float.IsInfinity(pushDistance)
                ? pushDistance
                : 0f;

        public AttackHitSettings(
            int attackNumber,
            MeleeHitDetector hitDetector,
            AttackDamage damage,
            float staggerDamage,
            float pushDistance)
            : this(
                attackNumber,
                hitDetector,
                damage,
                HitStrength.Light,
                staggerDamage,
                pushDistance)
        {
        }

        public AttackHitSettings(
            int attackNumber,
            MeleeHitDetector hitDetector,
            AttackDamage damage,
            HitStrength strength,
            float staggerDamage,
            float pushDistance)
        {
            this.attackNumber = attackNumber;
            this.hitDetector = hitDetector;
            this.damage = damage;
            this.strength = strength;
            this.staggerDamage = staggerDamage;
            this.pushDistance = pushDistance;
        }

        public static bool TryFind(
            AttackHitSettings[] attackHitSettings,
            int attackNumber,
            out AttackHitSettings foundSettings)
        {
            if (attackHitSettings != null)
            {
                for (int index = 0;
                    index < attackHitSettings.Length;
                    index++)
                {
                    if (attackHitSettings[index].attackNumber !=
                        attackNumber)
                    {
                        continue;
                    }

                    foundSettings = attackHitSettings[index];
                    return foundSettings.damage.IsValid;
                }
            }

            foundSettings = default;
            return false;
        }

        public static bool HasDuplicateAttackNumber(
            AttackHitSettings[] attackHitSettings)
        {
            if (attackHitSettings == null)
            {
                return false;
            }

            for (int first = 0;
                first < attackHitSettings.Length;
                first++)
            {
                for (int second = first + 1;
                    second < attackHitSettings.Length;
                    second++)
                {
                    if (attackHitSettings[first].attackNumber ==
                        attackHitSettings[second].attackNumber)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
