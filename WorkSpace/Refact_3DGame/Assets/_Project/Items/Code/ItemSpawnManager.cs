using UnityEngine;

namespace Items
{
    [DisallowMultipleComponent]
    public sealed class ItemSpawnManager : MonoBehaviour
    {
        [SerializeField]
        private ItemCatalog itemCatalog;

        private void Awake()
        {
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
                spawnPoint.transform.rotation);

            worldItem.SetItemDefinition(item.ItemDefinition);
        }
    }
}
