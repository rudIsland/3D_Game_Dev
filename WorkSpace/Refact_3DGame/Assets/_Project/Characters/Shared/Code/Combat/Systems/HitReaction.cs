using UnityEngine;

namespace Characters.Combat
{
    public enum HitDamageResult
    {
        Ignored = 0,
        Damaged = 1,
        Killed = 2
    }

    public enum HitReaction
    {
        None = 0,
        SmallHit = 1,
        BigHit = 2,
        Knockback = 3,
        Knockdown = 4,
        StaggerBreak = 5
    }

    public enum AttackStrength
    {
        Light = 0,
        Heavy = 1,
        Knockdown = 2
    }

    // 체력 피해가 확정된 뒤 행동 중단 수치와 대상 지원 범위로 몸 반응을 고른다.
    internal static class HitReactionSelector
    {
        internal static HitReaction Select(
            AttackStrength attackStrength,
            bool reachedStopLimit,
            bool protectsSmallHit,
            bool supportsKnockback,
            bool supportsKnockdown)
        {
            if (!reachedStopLimit)
            {
                return protectsSmallHit
                    ? HitReaction.None
                    : HitReaction.SmallHit;
            }

            switch (attackStrength)
            {
                case AttackStrength.Heavy:
                    return supportsKnockback
                        ? HitReaction.Knockback
                        : HitReaction.BigHit;
                case AttackStrength.Knockdown:
                    return supportsKnockdown
                        ? HitReaction.Knockdown
                        : HitReaction.BigHit;
                default:
                    return HitReaction.BigHit;
            }
        }
    }

    // 연속 피격에서 현재 반응보다 약한 애니메이션 재시작을 막는다.
    internal static class HitReactionPlayback
    {
        internal const float SmallHitRestartDelay = 0.18f;

        internal static bool CanStart(
            HitReaction currentReaction,
            HitReaction nextReaction,
            float elapsedReactionTime)
        {
            if (nextReaction == HitReaction.None)
            {
                return false;
            }

            if (currentReaction == HitReaction.None)
            {
                return true;
            }

            if (nextReaction < currentReaction)
            {
                return false;
            }

            return nextReaction != HitReaction.SmallHit ||
                elapsedReactionTime >= SmallHitRestartDelay;
        }
    }

    // 반응 단계별로 요청된 밀림 거리만 제한한다.
    internal static class HitPushDistance
    {
        private const float SmallHitDistanceScale = 0.15f;
        private const float SmallHitMaximumDistance = 0.08f;
        private const float BigHitMaximumDistance = 0.25f;

        internal static float GetDistance(
            float requestedDistance,
            HitReaction reaction)
        {
            float safeDistance = Mathf.Max(0f, requestedDistance);
            switch (reaction)
            {
                case HitReaction.SmallHit:
                    return Mathf.Min(
                        safeDistance * SmallHitDistanceScale,
                        SmallHitMaximumDistance);
                case HitReaction.BigHit:
                case HitReaction.StaggerBreak:
                    return Mathf.Min(
                        safeDistance,
                        BigHitMaximumDistance);
                case HitReaction.Knockback:
                case HitReaction.Knockdown:
                    return safeDistance;
                default:
                    return 0f;
            }
        }
    }
}
