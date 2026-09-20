using Characters.Player.Lifecycle;
using Items;
using UnityEngine;

namespace World.Interaction
{
    [DisallowMultipleComponent]
    public sealed class ItemExchangeInteraction :
        MonoBehaviour,
        IPlayerInteractable
    {
        private const string MissingCostItemGuideMessage =
            "책이 필요합니다";
        private const string ExchangeGuideMessage =
            "책을 바쳐 스크롤 받기";
        private const string RewardStorageFullGuideMessage =
            "가방에 스크롤을 넣을 수 없습니다";

        [SerializeField]
        private ItemCatalog itemCatalog;

        [SerializeField]
        private ItemType costItemType = ItemType.Book;

        [SerializeField]
        private ItemType rewardItemType = ItemType.Scroll;

        public PlayerInteractionGuide GetInteractionGuide(
            PlayerController player)
        {
            if (!TryGetExchangeItems(
                    player,
                    out ItemDefinition costItem,
                    out ItemDefinition rewardItem))
            {
                return PlayerInteractionGuide.Hidden;
            }

            if (!player.HasInventoryItem(costItem))
            {
                return new PlayerInteractionGuide(
                    MissingCostItemGuideMessage,
                    false);
            }

            return player.CanExchangeInventoryItem(costItem, rewardItem)
                ? new PlayerInteractionGuide(ExchangeGuideMessage, true)
                : new PlayerInteractionGuide(
                    RewardStorageFullGuideMessage,
                    false);
        }

        public bool CanInteract(PlayerController player)
        {
            if (!TryGetExchangeItems(
                    player,
                    out ItemDefinition costItem,
                    out ItemDefinition rewardItem))
            {
                return false;
            }

            return player.CanExchangeInventoryItem(costItem, rewardItem);
        }

        public bool TryInteract(PlayerController player)
        {
            if (!TryGetExchangeItems(
                    player,
                    out ItemDefinition costItem,
                    out ItemDefinition rewardItem) ||
                !player.TryExchangeInventoryItem(costItem, rewardItem))
            {
                return false;
            }

            enabled = false;
            return true;
        }

        private bool TryGetExchangeItems(
            PlayerController player,
            out ItemDefinition costItem,
            out ItemDefinition rewardItem)
        {
            costItem = null;
            rewardItem = null;

            if (!isActiveAndEnabled ||
                player == null ||
                itemCatalog == null ||
                !itemCatalog.TryGetItem(
                    costItemType,
                    out ItemCatalogEntry costEntry) ||
                costEntry.ItemDefinition == null ||
                !itemCatalog.TryGetItem(
                    rewardItemType,
                    out ItemCatalogEntry rewardEntry) ||
                rewardEntry.ItemDefinition == null)
            {
                return false;
            }

            costItem = costEntry.ItemDefinition;
            rewardItem = rewardEntry.ItemDefinition;
            return true;
        }
    }
}
