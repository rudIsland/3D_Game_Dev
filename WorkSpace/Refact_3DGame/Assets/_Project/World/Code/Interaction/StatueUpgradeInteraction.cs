using Characters.Player.Lifecycle;
using UnityEngine;

namespace World.Interaction
{
    public enum StatueUpgradeType
    {
        MaxHealth = 0,
        MaxStamina = 1,
        Strength = 2
    }

    [DisallowMultipleComponent]
    public sealed class StatueUpgradeInteraction :
        MonoBehaviour,
        IPlayerInteractable
    {
        [SerializeField]
        private StatueUpgradeType upgradeType;

        public StatueUpgradeType UpgradeType => upgradeType;

        public bool CanInteract(PlayerController player)
        {
            return enabled &&
                player != null &&
                !player.HasStatueUpgrade(upgradeType);
        }

        public bool TryInteract(PlayerController player)
        {
            if (!CanInteract(player) ||
                !player.TryApplyStatueUpgrade(upgradeType))
            {
                return false;
            }

            DisableInteraction();
            return true;
        }

        public void DisableInteraction()
        {
            enabled = false;
        }
    }
}
