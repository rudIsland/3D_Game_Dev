using System;
using UnityEngine;

namespace Items
{
    [Serializable]
    public sealed class ItemCatalogEntry
    {
        [SerializeField]
        private ItemType itemType;

        [SerializeField]
        private ItemDefinition itemDefinition;

        [SerializeField]
        private WorldItemPickup worldItemPrefab;

        public ItemType ItemType => itemType;
        public ItemDefinition ItemDefinition => itemDefinition;
        public WorldItemPickup WorldItemPrefab => worldItemPrefab;
    }

    [CreateAssetMenu(
        fileName = "ItemCatalog",
        menuName = "Items/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField]
        private ItemCatalogEntry[] entries = Array.Empty<ItemCatalogEntry>();

        public bool TryGetItem(ItemType itemType, out ItemCatalogEntry item)
        {
            if (itemType == ItemType.None)
            {
                item = null;
                return false;
            }

            for (int index = 0; index < entries.Length; index++)
            {
                ItemCatalogEntry entry = entries[index];

                if (entry == null || entry.ItemType != itemType)
                {
                    continue;
                }

                item = entry;
                return true;
            }

            item = null;
            return false;
        }
    }
}
