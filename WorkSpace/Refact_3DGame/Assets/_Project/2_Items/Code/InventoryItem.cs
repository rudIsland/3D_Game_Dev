using Characters.Player.Lifecycle;
using UnityEngine;
using World.Interaction;

namespace Items
{
    public sealed class InventoryItem : MonoBehaviour, IPlayerInteractable
    {
        private const string PickupGuideMessage = "아이템 줍기";
        private const string InventoryFullGuideMessage =
            "가방이 가득 찼습니다";

        [SerializeField]
        private ItemDefinition itemDefinition;

        [SerializeField]
        private int count;

        public PlayerInteractionGuide GetInteractionGuide(
            PlayerController player)
        {
            if (!isActiveAndEnabled ||
                player == null ||
                itemDefinition == null)
            {
                return PlayerInteractionGuide.Hidden;
            }

            return player.CanStoreInventoryItem(itemDefinition)
                ? new PlayerInteractionGuide(PickupGuideMessage, true)
                : new PlayerInteractionGuide(
                    InventoryFullGuideMessage,
                    false);
        }


        public bool CanInteract(PlayerController player)
        {
            return isActiveAndEnabled &&
                player != null &&
                itemDefinition != null &&
                player.CanStoreInventoryItem(itemDefinition);
        }

        public bool TryInteract(PlayerController player)
        {
            if (!CanInteract(player) ||
                !player.TryStoreInventoryItem(itemDefinition))
            {
                return false;
            }

            gameObject.SetActive(false);
            return true;
        }
    }
}
