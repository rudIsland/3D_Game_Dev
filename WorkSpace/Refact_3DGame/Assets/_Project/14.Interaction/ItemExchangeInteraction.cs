using Core;
using UnityEngine;

namespace Interaction
{
    [DisallowMultipleComponent]
    public sealed partial class ItemExchangeInteraction :
        MonoBehaviour,
        IPlayerInteractable
    {
        private const string MissingCostItemGuideMessage =
            "책이 필요합니다";
        private const string ExchangeGuideMessage =
            "책을 바쳐 스크롤 받기";
        private const string RewardStorageFullGuideMessage =
            "가방에 스크롤을 넣을 수 없습니다";

        private ItemDefinition costItem;
        private ItemDefinition rewardItem;

        [SerializeField]
        private ItemType costItemType = ItemType.Book;

        [SerializeField]
        private ItemType rewardItemType = ItemType.Scroll;

        public ItemType CostItemType => costItemType;
        public ItemType RewardItemType => rewardItemType;

        /// <summary>맵 연결 담당에게 교환할 두 아이템의 정의를 받는다.</summary>
        public void Connect(ItemDefinition cost, ItemDefinition reward)
        {
            costItem = cost;
            rewardItem = reward;
        }

        public PlayerInteractionGuide GetInteractionGuide(
            IInteractionActor player)
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

        public bool CanInteract(IInteractionActor player)
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

        public bool TryInteract(IInteractionActor player)
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
            IInteractionActor player,
            out ItemDefinition costItem,
            out ItemDefinition rewardItem)
        {
            costItem = this.costItem;
            rewardItem = this.rewardItem;
            return isActiveAndEnabled && player != null && costItem != null && rewardItem != null;
        }
    }
}
