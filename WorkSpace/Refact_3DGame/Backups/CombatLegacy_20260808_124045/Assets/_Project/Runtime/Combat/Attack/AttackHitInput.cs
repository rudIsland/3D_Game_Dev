using rudIsland.RPG3D.Characters;
using UnityEngine;
using rudIsland.RPG3D.Combat.Detection;
using rudIsland.RPG3D.Combat.Result;

namespace rudIsland.RPG3D.Combat.Attack
{
    // 공격 정보와 실제 접촉 결과를 함께 피해자에게 전달한다.
    public readonly struct AttackHitInput
    {
        public AttackDamage Damage { get; } // 실제 피해 구조
        public UnitTeam AttackerTeam { get; } // 아군 또는 적
        public int AttackNumber { get; } // 1부터 시작하는 공격 순서
        public HitStrength Strength { get; } // 피격 반응의 세기
        public float StaggerDamage { get; } // 경직 수치에 더할 값
        public float GuardStaminaDamage { get; } // 가드 시 감소시킬 Stamina
        public bool CanBeBlocked { get; } // 가드 가능 여부
        public bool CanBeParried { get; } // 패리 가능 여부
        public float PushDistance { get; } // 피격 대상을 미는 전체 거리
        public float HitStopTime { get; } // 피격 정지 시간
        public HitContact Contact { get; } // 무기와 피격 몸체의 접촉 결과

        public float HealthDamage => Damage.HealthDamage; // 실제 체력 피해
        public Vector3 HitPoint => Contact.HitPoint; // 피격 또는 피해 관련 값
        public Vector3 HitNormal => Contact.HitNormal; // 피격 또는 피해 관련 값
        public Vector3 HitDirection => Contact.HitDirection; // 피격 또는 피해 관련 값
        public HitBodyPart HitBodyPart => Contact.BodyPart; // 피격 또는 피해 관련 값
        public float HitSpeed => Contact.HitSpeed; // 피격 또는 피해 관련 값

        public AttackHitInput(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber)
            : this(
                damage,
                attackerTeam,
                attackNumber,
                HitStrength.Light,
                damage.HealthDamage,
                0f,
                true,
                true,
                0f,
                0f,
                default)
        {
        }

        public AttackHitInput(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber,
            float pushDistance)
            : this(
                damage,
                attackerTeam,
                attackNumber,
                HitStrength.Light,
                damage.HealthDamage,
                0f,
                true,
                true,
                pushDistance,
                0f,
                default)
        {
        }

        public AttackHitInput(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber,
            float staggerDamage,
            float pushDistance)
            : this(
                damage,
                attackerTeam,
                attackNumber,
                HitStrength.Light,
                staggerDamage,
                0f,
                true,
                true,
                pushDistance,
                0f,
                default)
        {
        }

        public AttackHitInput(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber,
            HitStrength strength,
            float staggerDamage,
            float pushDistance)
            : this(
                damage,
                attackerTeam,
                attackNumber,
                strength,
                staggerDamage,
                0f,
                true,
                true,
                pushDistance,
                0f,
                default)
        {
        }

        public AttackHitInput(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber,
            float pushDistance,
            HitContact contact)
            : this(
                damage,
                attackerTeam,
                attackNumber,
                HitStrength.Light,
                damage.HealthDamage,
                0f,
                true,
                true,
                pushDistance,
                0f,
                contact)
        {
        }

        public AttackHitInput(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber,
            float staggerDamage,
            float pushDistance,
            HitContact contact)
            : this(
                damage,
                attackerTeam,
                attackNumber,
                HitStrength.Light,
                staggerDamage,
                0f,
                true,
                true,
                pushDistance,
                0f,
                contact)
        {
        }

        public AttackHitInput(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber,
            HitStrength strength,
            float staggerDamage,
            float pushDistance,
            HitContact contact)
            : this(
                damage,
                attackerTeam,
                attackNumber,
                strength,
                staggerDamage,
                0f,
                true,
                true,
                pushDistance,
                0f,
                contact)
        {
        }

        public AttackHitInput(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber,
            HitStrength strength,
            float staggerDamage,
            float guardStaminaDamage,
            bool canBeBlocked,
            bool canBeParried,
            float pushDistance,
            float hitStopTime,
            HitContact contact)
        {
            Damage = damage;
            AttackerTeam = attackerTeam;
            AttackNumber = attackNumber;
            Strength =
                strength >= HitStrength.Light &&
                strength <= HitStrength.Knockdown
                    ? strength
                    : HitStrength.Light;
            StaggerDamage = SanitizeNonNegative(staggerDamage);
            GuardStaminaDamage = SanitizeNonNegative(guardStaminaDamage);
            CanBeBlocked = canBeBlocked;
            CanBeParried = canBeParried;
            PushDistance = SanitizeNonNegative(pushDistance);
            HitStopTime = SanitizeNonNegative(hitStopTime);
            Contact = contact;
        }

        // 기존 공격 정보는 유지하고 실제 접촉 정보가 담긴 새 값을 만든다.
        public AttackHitInput CreateWithHitContact(in HitContact contact)
        {
            return new AttackHitInput(
                Damage,
                AttackerTeam,
                AttackNumber,
                Strength,
                StaggerDamage,
                GuardStaminaDamage,
                CanBeBlocked,
                CanBeParried,
                PushDistance,
                HitStopTime,
                contact);
        }

        private static float SanitizeNonNegative(float value)
        {
            return value > 0f &&
                !float.IsNaN(value) &&
                !float.IsInfinity(value)
                    ? value
                    : 0f;
        }
    }
}
