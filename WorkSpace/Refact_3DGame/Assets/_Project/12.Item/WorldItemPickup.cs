using Core;
using UnityEngine;

namespace Item
{
    // 씬에 놓인 아이템을 플레이어 인벤토리에 넣고 사용된 오브젝트를 끈다.
    [DisallowMultipleComponent]
    public sealed partial class WorldItemPickup : MonoBehaviour, IPlayerInteractable
    {
        private const string PickupGuideMessage = "책 줍기";
        private const string InventoryFullGuideMessage =
            "가방이 가득 찼습니다";

        [SerializeField]
        private ItemDefinition itemDefinition;

        private bool isCollected;
        internal ItemPool OwnerPool { get; private set; }
        internal bool IsTaken { get; set; }
        internal void Prepare(ItemPool pool, ItemDefinition definition)
        {
            OwnerPool = pool;
            IsTaken = true;
            SetItemDefinition(definition);
        }

        internal void SetItemDefinition(ItemDefinition definition)
        {
            itemDefinition = definition;
            isCollected = false;
        }

        public PlayerInteractionGuide GetInteractionGuide(
            IInteractionActor player)
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

        public bool CanInteract(IInteractionActor player)
        {
            return isActiveAndEnabled &&
                !isCollected &&
                player != null &&
                itemDefinition != null &&
                player.CanStoreInventoryItem(itemDefinition);
        }

        public bool TryInteract(IInteractionActor player)
        {
            if (!CanInteract(player) ||
                !player.TryStoreInventoryItem(itemDefinition))
            {
                return false;
            }

            isCollected = true;
            if (OwnerPool != null) OwnerPool.Return(this);
            else gameObject.SetActive(false);
            return true;
        }
    }
}
