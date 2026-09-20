using UnityEngine;

namespace Items
{
    [DisallowMultipleComponent]
    public sealed class ItemSpawnManager : MonoBehaviour
    {
        [SerializeField]
        private ItemCatalog itemCatalog;
        private bool hasSpawned;
        private ItemContainer items;

        public void Connect(ItemContainer container)
        {
            items = container;
            SpawnItems();
        }

        public void SpawnItems()
        {
            if (hasSpawned || items == null) return;
            if (itemCatalog == null)
            {
                return;
            }

            ItemSpawnPoint[] spawnPoints =
                GetComponentsInChildren<ItemSpawnPoint>();

            for (int index = 0; index < spawnPoints.Length; index++)
            {
                SpawnItem(spawnPoints[index]);
            }
            hasSpawned = true;
        }

        private void SpawnItem(ItemSpawnPoint spawnPoint)
        {
            if (spawnPoint == null ||
                !itemCatalog.TryGetItem(
                    spawnPoint.ItemType,
                    out ItemCatalogEntry item) ||
                item.ItemDefinition == null ||
                item.WorldItemPrefab == null)
            {
                return;
            }

            items.TrySpawn(item, spawnPoint.transform.position,
                spawnPoint.transform.rotation, out _);
        }
    }
}
