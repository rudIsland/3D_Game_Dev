using Core;

namespace NightShade
{
    // NightShade가 받은 공격 세기와 누적 경직 한계로 몸 반응을 고른다.
    internal static class NightShadeSwordHitReactionSelector
    {
        internal static HitReaction Select(
            AttackStrength attackStrength,
            bool reachedStaggerLimit,
            bool protectsSmallHit)
        {
            if (reachedStaggerLimit)
            {
                return HitReaction.StaggerBreak;
            }

            if (protectsSmallHit)
                return HitReaction.None;

            if (attackStrength == AttackStrength.Heavy || attackStrength == AttackStrength.Knockdown)
            {
                return HitReaction.BigHit;
            }

            return HitReaction.SmallHit;
        }
    }
}
