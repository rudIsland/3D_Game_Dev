namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal static class NightShadeSwordAttackTiming
    {
        internal const float HeavyProtectionStartNormalizedTime = 0.16f;
        internal const float HeavyProtectionEndNormalizedTime = 0.39f;

        internal static bool IsHeavyProtectionTime(float normalizedTime)
        {
            return normalizedTime >= HeavyProtectionStartNormalizedTime &&
                normalizedTime < HeavyProtectionEndNormalizedTime;
        }
    }
}
