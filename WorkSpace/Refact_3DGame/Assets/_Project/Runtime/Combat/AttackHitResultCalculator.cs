using rudIsland.RPG3D.Characters;
using UnityEngine;

namespace rudIsland.RPG3D.Combat
{
    // 현재 Unit 값을 읽어 공격 결과만 계산한다. 계산 중에는 Unit을 변경하지 않는다.
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

            if (target.DefenseStatus.IsInvincible)
            {
                return AttackHitResult.Dodged;
            }

            if (target.DefenseStatus.IsParryWindowOpen &&
                hit.CanBeParried)
            {
                return AttackHitResult.Parried;
            }

            if (target.DefenseStatus.IsGuarding &&
                hit.CanBeBlocked &&
                IsInsideGuardAngle(
                    targetForward,
                    hit.HitDirection,
                    target.DefenseStatus.GuardAngle))
            {
                if (hit.GuardStaminaDamage <= 0f ||
                    target.Stamina.CanSpend(hit.GuardStaminaDamage))
                {
                    return new AttackHitResult(
                        AttackHitResultType.Blocked,
                        0f,
                        hit.GuardStaminaDamage,
                        0f,
                        default,
                        hit.HitStopTime);
                }

                return new AttackHitResult(
                    AttackHitResultType.GuardBroken,
                    0f,
                    target.Stamina.CurrentStamina,
                    0f,
                    default,
                    hit.HitStopTime);
            }

            float healthDamage = Mathf.Min(
                target.Health.CurrentHealth,
                hit.HealthDamage);
            HitReaction reaction = CreateReaction(
                in hit,
                targetForward);

            if (healthDamage >= target.Health.CurrentHealth)
            {
                return new AttackHitResult(
                    AttackHitResultType.Killed,
                    healthDamage,
                    0f,
                    0f,
                    reaction,
                    hit.HitStopTime);
            }

            if (hit.Strength == HitStrength.Knockdown &&
                !target.DefenseStatus.IsSuperArmorActive)
            {
                return new AttackHitResult(
                    AttackHitResultType.KnockedDown,
                    healthDamage,
                    0f,
                    hit.StaggerDamage,
                    reaction,
                    hit.HitStopTime);
            }

            bool isStaggered =
                target.Stagger.WillReachLimit(hit.StaggerDamage) &&
                !target.DefenseStatus.IsSuperArmorActive;
            return new AttackHitResult(
                isStaggered
                    ? AttackHitResultType.Staggered
                    : AttackHitResultType.Damaged,
                healthDamage,
                0f,
                hit.StaggerDamage,
                reaction,
                hit.HitStopTime);
        }

        private static HitReaction CreateReaction(
            in AttackHitInput hit,
            Vector3 targetForward)
        {
            Vector3 safeForward = targetForward;
            safeForward.y = 0f;
            if (safeForward.sqrMagnitude <= 0.0001f)
            {
                safeForward = Vector3.forward;
            }

            Vector3 targetRight = Vector3.Cross(
                Vector3.up,
                safeForward.normalized);
            return HitReaction.Create(
                in hit,
                safeForward,
                targetRight);
        }

        private static bool IsInsideGuardAngle(
            Vector3 targetForward,
            Vector3 hitDirection,
            float guardAngle)
        {
            Vector3 incomingDirection = -hitDirection;
            incomingDirection.y = 0f;
            targetForward.y = 0f;

            if (incomingDirection.sqrMagnitude <= 0.0001f ||
                targetForward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            float minimumDot = Mathf.Cos(
                guardAngle * 0.5f * Mathf.Deg2Rad);
            return Vector3.Dot(
                    targetForward.normalized,
                    incomingDirection.normalized) >= minimumDot;
        }
    }
}
