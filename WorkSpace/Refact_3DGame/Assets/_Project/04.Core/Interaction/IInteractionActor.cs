namespace Core
{
    // 상호작용 물체가 요청할 수 있는 가방·강화 동작이다.
    public interface IInteractionActor
    {
        bool CanStoreInventoryItem(ItemDefinition item);
        bool TryStoreInventoryItem(ItemDefinition item);
        bool HasInventoryItem(ItemDefinition item);
        bool CanExchangeInventoryItem(ItemDefinition costItem, ItemDefinition rewardItem);
        bool TryExchangeInventoryItem(ItemDefinition costItem, ItemDefinition rewardItem);
        bool HasStatueUpgrade(StatueUpgradeType upgradeType);
        bool TryApplyStatueUpgrade(StatueUpgradeType upgradeType);
    }

    // 석상이 요청하는 강화 항목이다.
    public enum StatueUpgradeType
    {
        MaxHealth = 0,
        MaxStamina = 1,
        Strength = 2
    }
}
