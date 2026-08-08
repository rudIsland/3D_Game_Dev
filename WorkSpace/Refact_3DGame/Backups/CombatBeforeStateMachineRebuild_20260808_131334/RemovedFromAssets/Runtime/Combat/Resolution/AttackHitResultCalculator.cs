using rudIsland.RPG3D.Characters;
using UnityEngine;
using rudIsland.RPG3D.Combat.Attack;
using rudIsland.RPG3D.Combat.Result;

namespace rudIsland.RPG3D.Combat.Resolution
{
    // 1단계에서는 유효한 대상의 체력 피해만 계산한다.
    public sealed class AttackHitResultCalculator
    {
        public AttackHitResult CalculateResult(
            in AttackHitInput hit,
            Unit target,
            Vector3 targetForward)
        {
            if (target == null ||
                !target.CanTakeHit ||
                hit.AttackerTeam == target.Team ||
                !hit.Damage.IsValid)
            {
                return AttackHitResult.Ignored;
            }

            float healthDamage = Mathf.Min(
                target.Health.CurrentHealth,
                hit.HealthDamage);
            AttackHitResultType resultType =
                healthDamage >= target.Health.CurrentHealth
                    ? AttackHitResultType.Killed
                    : AttackHitResultType.Damaged;

            return new AttackHitResult(
                resultType,
                healthDamage,
                0f,
                0f,
                default,
                0f);
        }
    }
}