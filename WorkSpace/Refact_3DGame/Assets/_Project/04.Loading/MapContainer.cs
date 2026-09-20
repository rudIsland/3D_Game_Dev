using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace World.Loading
{
    // 맵 씬과 사용 횟수만 보관한다. 실행 순서는 호출자가 정한다.
    public sealed class MapContainer
    {
        private sealed class CachedMap
        {
            public AsyncOperationHandle<SceneInstance> handle;
            public int refCount;
        }

        private readonly Dictionary<string, CachedMap> cache =
            new Dictionary<string, CachedMap>(StringComparer.Ordinal);

        // 씬 내부 에셋 수가 아니라, 캐시에 보관된 씬 수다.
        public int ResourceCount => cache.Count;
        public bool IsBusy { get; private set; }

        public int GetRefCount(string address) =>
            address != null && cache.TryGetValue(address, out CachedMap map) ? map.refCount : 0;

        public async Task<Scene> LoadAsync(string address)
        {
            EnsureIdle();
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("맵 Address가 필요합니다.", nameof(address));

            if (cache.TryGetValue(address, out CachedMap cached))
            {
                cached.refCount++;
                return cached.handle.Result.Scene;
            }

            IsBusy = true;
            AsyncOperationHandle<SceneInstance> handle = default;
            bool stored = false;
            try
            {
                handle = Addressables.LoadSceneAsync(address, LoadSceneMode.Additive,
                    SceneReleaseMode.OnlyReleaseSceneOnHandleRelease);
                await handle.Task;
                if (handle.Status != AsyncOperationStatus.Succeeded)
                    throw new InvalidOperationException("맵 로딩 실패: " + address, handle.OperationException);

                cache.Add(address, new CachedMap { handle = handle, refCount = 1 });
                stored = true;
                return handle.Result.Scene;
            }
            finally
            {
                if (!stored && handle.IsValid()) Addressables.Release(handle);
                IsBusy = false;
            }
        }

        // 사용 횟수만 줄인다. 실제 씬 해제는 CleanUpAsync에서 한다.
        public bool Release(string address)
        {
            EnsureIdle();
            if (address == null || !cache.TryGetValue(address, out CachedMap map) || map.refCount == 0)
                return false;

            map.refCount--;
            return true;
        }

        public async Task CleanUpAsync()
        {
            EnsureIdle();
            IsBusy = true;
            try
            {
                var unused = new List<string>();
                foreach (var pair in cache)
                    if (pair.Value.refCount == 0) unused.Add(pair.Key);

                foreach (string address in unused)
                {
                    CachedMap map = cache[address];
                    var unloading = Addressables.UnloadSceneAsync(map.handle, autoReleaseHandle: false);
                    try
                    {
                        await unloading.Task;
                        if (unloading.Status != AsyncOperationStatus.Succeeded)
                            throw new InvalidOperationException("맵 해제 실패: " + address, unloading.OperationException);

                        cache.Remove(address);
                    }
                    finally
                    {
                        // UnloadSceneAsync가 로딩 핸들을 반환한다. 해제 작업 핸들만 직접 반환한다.
                        if (unloading.IsValid()) Addressables.Release(unloading);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void EnsureIdle()
        {
            if (IsBusy) throw new InvalidOperationException("진행 중인 맵 로딩·정리가 끝난 뒤 요청하세요.");
        }
    }
}
