using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Items
{
    // 한 씬의 필드 아이템 풀만 소유한다. 획득 정보는 인벤토리에 남긴다.
    public sealed class ItemContainer : IDisposable
    {
        private readonly Scene scene;
        private readonly Dictionary<WorldItemPickup, ItemPool> pools = new Dictionary<WorldItemPickup, ItemPool>();
        private bool disposed;
        public int PoolCount => pools.Count;
        public ItemContainer(Scene scene) { this.scene = scene; }

        public bool TrySpawn(ItemCatalogEntry item, Vector3 position, Quaternion rotation, out WorldItemPickup pickup)
        {
            pickup = null;
            if (disposed || item == null || item.WorldItemPrefab == null || item.ItemDefinition == null) return false;
            if (!pools.TryGetValue(item.WorldItemPrefab, out ItemPool pool))
            {
                pool = new ItemPool(item.WorldItemPrefab, scene);
                pools.Add(item.WorldItemPrefab, pool);
            }
            pickup = pool.Take(item.ItemDefinition, position, rotation);
            return true;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (ItemPool pool in pools.Values) pool.Dispose();
            pools.Clear();
        }
    }
}
