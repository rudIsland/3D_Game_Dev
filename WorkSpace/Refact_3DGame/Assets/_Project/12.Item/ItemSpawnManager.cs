using UnityEngine;

namespace Items
{
    [DisallowMultipleComponent]
    public sealed class ItemSpawnManager : MonoBehaviour
    {
        [SerializeField]
        private ItemCatalog itemCatalog;
        private bool hasSpawned;

        private void Awake()
        {
            SpawnItems();
        }

        public void SpawnItems()
        {
            if (hasSpawned) return;
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

            WorldItemPickup worldItem = Instantiate(
                item.WorldItemPrefab,
                spawnPoint.transform.position,
                spawnPoint.transform.rotation,
                spawnPoint.transform);

            worldItem.SetItemDefinition(item.ItemDefinition);
        }
    }
}
