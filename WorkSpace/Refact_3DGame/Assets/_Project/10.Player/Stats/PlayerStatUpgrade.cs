using Characters.Player.Lifecycle;
using Characters.Player.StateMachine;
using World.Interaction;

namespace Characters.Player.Stats
{
    // 강화 중복 여부를 확인하고 기록과 실제 능력치를 함께 변경한다.
    internal static class PlayerStatUpgrade
    {
        internal static bool HasUpgrade(StatueUpgradeType upgradeType)
        {
            return PlayerStatUpgradeSession.HasUpgrade(upgradeType);
        }

        internal static bool TryApply(
            StatueUpgradeType upgradeType,
            PlayerWorldUnit player,
            PlayerStateMachine stateMachine)
        {
            if (player == null || stateMachine == null || player.IsDead)
            {
                return false;
            }

            // 능력치 변경 이벤트에서 다시 요청해도 중복 적용되지 않게 먼저 기록한다.
            if (!PlayerStatUpgradeSession.TryActivate(upgradeType))
            {
                return false;
            }

            switch (upgradeType)
            {
                case StatueUpgradeType.MaxHealth:
                    player.MultiplyMaximumHealth(
                        PlayerStatUpgradeSession.MaxHealthMultiplier);
                    return true;

                case StatueUpgradeType.MaxStamina:
                    player.MultiplyMaximumStamina(
                        PlayerStatUpgradeSession.MaxStaminaMultiplier);
                    return true;

                case StatueUpgradeType.Strength:
                    stateMachine.SetAttackDamageMultiplier(
                        PlayerStatUpgradeSession.StrengthMultiplier);
                    return true;

                default:
                    return false;
            }
        }
    }
}
