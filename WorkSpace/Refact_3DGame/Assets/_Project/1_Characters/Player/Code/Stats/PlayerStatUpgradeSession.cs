using UnityEngine;
using World.Interaction;

namespace Characters.Player.Stats
{
    internal static class PlayerStatUpgradeSession
    {
        internal const float MaxHealthMultiplier = 1.2f;
        internal const float MaxStaminaMultiplier = 1.5f;
        internal const float StrengthMultiplier = 1.3f;

        private static bool hasMaxHealthUpgrade;
        private static bool hasMaxStaminaUpgrade;
        private static bool hasStrengthUpgrade;

        internal static float CurrentMaxHealthMultiplier =>
            hasMaxHealthUpgrade ? MaxHealthMultiplier : 1f;

        internal static float CurrentMaxStaminaMultiplier =>
            hasMaxStaminaUpgrade ? MaxStaminaMultiplier : 1f;

        internal static float CurrentStrengthMultiplier =>
            hasStrengthUpgrade ? StrengthMultiplier : 1f;

        internal static bool HasUpgrade(StatueUpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case StatueUpgradeType.MaxHealth:
                    return hasMaxHealthUpgrade;

                case StatueUpgradeType.MaxStamina:
                    return hasMaxStaminaUpgrade;

                case StatueUpgradeType.Strength:
                    return hasStrengthUpgrade;

                default:
                    return false;
            }
        }

        internal static bool TryActivate(StatueUpgradeType upgradeType)
        {
            if (HasUpgrade(upgradeType))
            {
                return false;
            }

            switch (upgradeType)
            {
                case StatueUpgradeType.MaxHealth:
                    hasMaxHealthUpgrade = true;
                    return true;

                case StatueUpgradeType.MaxStamina:
                    hasMaxStaminaUpgrade = true;
                    return true;

                case StatueUpgradeType.Strength:
                    hasStrengthUpgrade = true;
                    return true;

                default:
                    return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            hasMaxHealthUpgrade = false;
            hasMaxStaminaUpgrade = false;
            hasStrengthUpgrade = false;
        }
    }
}
