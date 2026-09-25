using Core;
using UnityEngine;

namespace Interaction
{
    [DisallowMultipleComponent]
    public sealed class StatueUpgradeInteraction :
        MonoBehaviour,
        IPlayerInteractable
    {
        private const string UpgradeGuideMessage = "석상 사용하기";

        [SerializeField]
        private StatueUpgradeType upgradeType;

        public StatueUpgradeType UpgradeType => upgradeType;

        public PlayerInteractionGuide GetInteractionGuide(
            IInteractionActor player)
        {
            return CanInteract(player)
                ? new PlayerInteractionGuide(UpgradeGuideMessage, true)
                : PlayerInteractionGuide.Hidden;
        }

        public bool CanInteract(IInteractionActor player)
        {
            return enabled &&
                player != null &&
                !player.HasStatueUpgrade(upgradeType);
        }

        public bool TryInteract(IInteractionActor player)
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
