using Characters.Player.Lifecycle;
using Items;
using UnityEngine;

namespace World.Interaction
{
    [DisallowMultipleComponent]
    public sealed class ItemRewardInteraction :
        MonoBehaviour,
        IPlayerInteractable
    {
        [SerializeField]
        private ItemCatalog itemCatalog;

        [SerializeField]
        private ItemType rewardItemType;

        public bool CanInteract(PlayerController player)
        {
            return isActiveAndEnabled &&
                player != null &&
                itemCatalog != null &&
                itemCatalog.TryGetItem(
                    rewardItemType,
                    out ItemCatalogEntry item) &&
                item.ItemDefinition != null &&
                player.CanStoreInventoryItem(item.ItemDefinition);
        }

        public bool TryInteract(PlayerController player)
        {
            if (!CanInteract(player) ||
                !itemCatalog.TryGetItem(
                    rewardItemType,
                    out ItemCatalogEntry item) ||
                !player.TryStoreInventoryItem(item.ItemDefinition))
            {
                return false;
            }

            enabled = false;
            return true;
        }
    }
}
