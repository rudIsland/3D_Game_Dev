using System;
using Items;

namespace Characters.Player.Inventory
{
    // 플레이어가 소지한 아이템을 두 개의 고정 슬롯에 순서대로 보관한다.
    public sealed class PlayerInventory
    {
        public const int SlotCount = 2;

        private readonly ItemDefinition[] items = new ItemDefinition[SlotCount];
        private readonly int[] itemCounts = new int[SlotCount];

        private int occupiedSlotCount;

        public int ItemCount => occupiedSlotCount;
        public bool HasEmptySlot => occupiedSlotCount < SlotCount;

        public event Action<PlayerInventory> Changed;

        public bool CanAdd(ItemDefinition item)
        {
            if (item == null)
            {
                return false;
            }

            for(int index=0; index<items.Length; index++)
            {
                if (items[index] != item)
                {
                    continue;
                }

                return itemCounts[index] < item.MaxStackCount;
            }

            return HasEmptySlot;
        }

        public bool TryAdd(ItemDefinition item)
        {
            if (item == null)
            {
                return false;
            }

            //1. 같은 아이템이 있으면 해당 아이템의 수량만 증가시킨다.
            for(int index=0; index<items.Length; index++)
            {
                //같은 아이템이 아닐경우
                if (items[index] != item)
                {
                    continue;
                }
                //슬롯에 아이템의 갯수가 이미 최대치일경우
                if(itemCounts[index]>=item.MaxStackCount){
                    return false;
                }

                //해당 슬롯에 수량증가
                itemCounts[index]++;
                Changed?.Invoke(this);
                return true;

            }

            //2. 같은 아이템이 없으면 빈 슬롯을 사용한다.
            for (int index = 0; index < items.Length; index++)
            {
                if (items[index] != null)
                {
                    continue;
                }

                items[index] = item;
                itemCounts[index] = 1;
                occupiedSlotCount++;
                Changed?.Invoke(this);
                return true;
            }

            return false;
        }

        public bool CanExchangeItem(
            ItemDefinition costItem,
            ItemDefinition rewardItem)
        {
            if (costItem == null ||
                rewardItem == null ||
                costItem == rewardItem)
            {
                return false;
            }

            int costSlotIndex = FindItemSlot(costItem);
            if (costSlotIndex < 0 || itemCounts[costSlotIndex] < 1)
            {
                return false;
            }

            int rewardSlotIndex = FindItemSlot(rewardItem);
            if (rewardSlotIndex >= 0)
            {
                return itemCounts[rewardSlotIndex] <
                    rewardItem.MaxStackCount;
            }

            return HasEmptySlot || itemCounts[costSlotIndex] == 1;
        }

        public bool TryExchangeItem(
            ItemDefinition costItem,
            ItemDefinition rewardItem)
        {
            if (!CanExchangeItem(costItem, rewardItem))
            {
                return false;
            }

            int costSlotIndex = FindItemSlot(costItem);
            int rewardSlotIndex = FindItemSlot(rewardItem);

            itemCounts[costSlotIndex]--;
            if (itemCounts[costSlotIndex] == 0)
            {
                items[costSlotIndex] = null;
                occupiedSlotCount--;
            }

            if (rewardSlotIndex >= 0)
            {
                itemCounts[rewardSlotIndex]++;
            }
            else
            {
                int emptySlotIndex = FindEmptySlot();
                items[emptySlotIndex] = rewardItem;
                itemCounts[emptySlotIndex] = 1;
                occupiedSlotCount++;
            }

            Changed?.Invoke(this);
            return true;
        }

    //아이템 제거 시도
        public bool TryRemove(ItemDefinition item, int removeCount)
        {
            if(item==null || removeCount <= 0)
            {
                return false;
            }

            for(int index=0; index<items.Length; index++)
            {
                //아이템이 같지 않을경우
                if (items[index] != item)
                {
                    continue;
                }
                //아이템 보유 수량이 제거갯수보다 적을경우
                if(itemCounts[index] < removeCount)
                {
                    return false;
                }

                //제거 수행
                itemCounts[index] -= removeCount;
                if (itemCounts[index] == 0)
                {
                    items[index] = null;
                    occupiedSlotCount--;
                }

                Changed?.Invoke(this);
                return true;
            }

            return false;
        }


        public ItemDefinition GetItem(int slotIndex)
        {
            return IsValidSlot(slotIndex)?items[slotIndex]:null;
        }

        public int GetCount(int slotIndex)
        {
            return IsValidSlot(slotIndex) ? itemCounts[slotIndex] : 0;
        }

        public bool HasItem(ItemDefinition item)
        {
            return item != null && FindItemSlot(item) >= 0;
        }

        private int FindItemSlot(ItemDefinition item)
        {
            for (int index = 0; index < items.Length; index++)
            {
                if (items[index] == item)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindEmptySlot()
        {
            for (int index = 0; index < items.Length; index++)
            {
                if (items[index] == null)
                {
                    return index;
                }
            }

            return -1;
        }

        private bool IsValidSlot(int slotIndex)
        {
            return slotIndex >=0 && slotIndex < SlotCount;
        }
    }
}
