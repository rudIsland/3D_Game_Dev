using Core;
using UnityEngine;

namespace Player
{
    // PlayerController 인스턴스가 소유하는 강화 기록이다.
    // 플레이어 재생성 시 기록을 이어 주는 기능은 실행 흐름을 연결할 때 정한다.
    // 새 기능에서는 이 클래스를 전역 상태 보관 방식으로 따라 쓰지 않는다.
    internal sealed class PlayerStatUpgradeSession
    {
        internal const float MaxHealthMultiplier = 1.2f;
        internal const float MaxStaminaMultiplier = 1.5f;
        internal const float StrengthMultiplier = 1.3f;

        private bool hasMaxHealthUpgrade;
        private bool hasMaxStaminaUpgrade;
        private bool hasStrengthUpgrade;

        internal float CurrentMaxHealthMultiplier =>
            hasMaxHealthUpgrade ? MaxHealthMultiplier : 1f;

        internal float CurrentMaxStaminaMultiplier =>
            hasMaxStaminaUpgrade ? MaxStaminaMultiplier : 1f;

        internal float CurrentStrengthMultiplier =>
            hasStrengthUpgrade ? StrengthMultiplier : 1f;

        internal bool HasUpgrade(StatueUpgradeType upgradeType)
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

        internal bool TryActivate(StatueUpgradeType upgradeType)
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

    }
}
