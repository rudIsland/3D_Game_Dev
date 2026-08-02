using rudIsland.RPG3D.Characters;
using UnityEngine;

namespace rudIsland.RPG3D.Combat
{
    // 공격 정보와 실제 접촉 결과를 함께 피해자에게 전달한다.
    public readonly struct AttackHitData
    {
        public AttackDamage Damage { get; } // 실제 피해 구조
        public UnitTeam AttackerTeam { get; } // 아군 또는 적
        public int AttackNumber { get; } // 1부터 시작하는 공격 순서
        public HitStrength Strength { get; } // 피격 반응의 세기
        public float StaggerDamage { get; } // 경직 수치에 더할 값
        public float PushDistance { get; } // 피격 대상을 미는 전체 거리
        public HitContact Contact { get; } // 무기와 피격 몸체의 접촉 결과

        public Vector3 HitPoint => Contact.HitPoint; // 피격 또는 피해 관련 값
        public Vector3 HitNormal => Contact.HitNormal; // 피격 또는 피해 관련 값
        public Vector3 HitDirection => Contact.HitDirection; // 피격 또는 피해 관련 값
        public HitBodyPart HitBodyPart => Contact.BodyPart; // 피격 또는 피해 관련 값
        public float HitSpeed => Contact.HitSpeed; // 피격 또는 피해 관련 값

        public AttackHitData(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber)
            : this(
                damage,
                attackerTeam,
                attackNumber,
                HitStrength.Light,
                damage.HealthDamage,
                0f)
        {
        }

        public AttackHitData(
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
                pushDistance,
                default)
        {
        }

        public AttackHitData(
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
                pushDistance,
                default)
        {
        }

        public AttackHitData(
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
                pushDistance,
                default)
        {
        }

        public AttackHitData(
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
                pushDistance,
                contact)
        {
        }

        public AttackHitData(
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
                pushDistance,
                contact)
        {
        }

        public AttackHitData(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber,
            HitStrength strength,
            float staggerDamage,
            float pushDistance,
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
            StaggerDamage =
                staggerDamage > 0f &&
                !float.IsNaN(staggerDamage) &&
                !float.IsInfinity(staggerDamage)
                    ? staggerDamage
                    : 0f;
            PushDistance =
                pushDistance > 0f &&
                !float.IsNaN(pushDistance) &&
                !float.IsInfinity(pushDistance)
                    ? pushDistance
                    : 0f;
            Contact = contact;
        }

        // 기존 공격 정보는 유지하고 실제 접촉 정보가 담긴 새 값을 만든다.
        public AttackHitData CreateWithHitContact(in HitContact contact)
        {
            return new AttackHitData(
                Damage,
                AttackerTeam,
                AttackNumber,
                Strength,
                StaggerDamage,
                PushDistance,
                contact);
        }
    }
}
