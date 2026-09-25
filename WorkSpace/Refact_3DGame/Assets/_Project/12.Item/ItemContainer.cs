using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Item
{
    // 한 씬의 필드 아이템 풀만 소유한다. 획득 정보는 인벤토리에 남긴다.
    public sealed class ItemContainer : Singleton<ItemContainer>, IDisposable
    {
        /// <summary>소속 씬으로 단일 컨테이너를 준비한다. 같은 씬은 재사용하며, 다른 씬은 Dispose 후 지정한다.</summary>
        public static ItemContainer Create(UnityScene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new ArgumentException("로드된 아이템 소속 씬이 필요합니다.", nameof(scene));
            if (CurrentInstance != null && CurrentInstance.scene != scene)
                throw new InvalidOperationException("기존 ItemContainer를 Dispose한 뒤 소속 씬을 바꾸세요.");
            return CurrentInstance ?? StoreInstance(new ItemContainer(scene));
        }

        // Domain Reload를 꺼도 이전 Play의 아이템 풀에 접근하지 않는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay() { ResetInstance(); }

        private readonly UnityScene scene;
        private readonly Dictionary<WorldItemPickup, ItemPool> pools = new Dictionary<WorldItemPickup, ItemPool>();
        private bool disposed;
        // Create에서 전달한 씬에 필드 아이템을 배치한다.
        private ItemContainer(UnityScene scene) { this.scene = scene; }

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
        /// <summary>아이템 풀을 정리하고 단일 인스턴스를 해제한다. 다음 사용 전에 Create를 호출한다.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (ItemPool pool in pools.Values) pool.Dispose();
            pools.Clear();
            ClearInstance();
        }
    }
}
