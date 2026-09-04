using Characters.Player.Lifecycle;
using UnityEngine;
using World.Interaction;

namespace Items
{
    // 씬에 놓인 아이템을 플레이어 인벤토리에 넣고 사용된 오브젝트를 끈다.
    [DisallowMultipleComponent]
    public sealed class WorldItemPickup : MonoBehaviour, IPlayerInteractable
    {
        private const string PickupGuideMessage = "책 줍기";
        private const string InventoryFullGuideMessage =
            "가방이 가득 찼습니다";

        [SerializeField]
        private ItemDefinition itemDefinition;

        private bool isCollected;

        internal void SetItemDefinition(ItemDefinition definition)
        {
            itemDefinition = definition;
            isCollected = false;
        }

        public PlayerInteractionGuide GetInteractionGuide(
            PlayerController player)
        {
            if (!isActiveAndEnabled ||
                isCollected ||
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
                !isCollected &&
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

            isCollected = true;
            gameObject.SetActive(false);
            return true;
        }
    }
}
