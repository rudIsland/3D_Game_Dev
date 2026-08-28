using Characters.Player.Lifecycle;
using UnityEngine;
using World.Interaction;

namespace Items
{
    public sealed class InventoryItem : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField]
        private ItemDefinition itemDefinition;

        [SerializeField]
        private int count;


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
