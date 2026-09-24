using Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace World.Loading
{
    // 일반 에셋과 씬의 로딩·해제를 처리하고, 각각의 핸들과 사용 요청 횟수를 보관한다.
    public sealed class AddressableManager : Singleton<AddressableManager>
    {
        /// <summary>모든 호출자가 에셋·씬 핸들과 사용 횟수를 공유하는 단일 관리자다. Unity 메인 스레드에서 사용한다.</summary>
        public new static AddressableManager Instance => CurrentInstance ?? StoreInstance(new AddressableManager());

        // 외부에서 별도의 에셋 캐시를 만들지 못하게 한다.
        private AddressableManager() { }

        // Domain Reload를 꺼도 이전 Play의 관리자를 다시 사용하지 않는다.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay() { ResetInstance(); }

        // 같은 키를 다른 타입으로 다시 요청하지 않도록 로드 타입도 함께 기록한다.
        private sealed class LoadedAsset
        {
            public AsyncOperationHandle handle;
            public Type assetType;
            public int refCount;
        }

        private readonly Dictionary<string, LoadedAsset> loadedAssets =
            new Dictionary<string, LoadedAsset>(StringComparer.Ordinal);

        // 보관 중인 원본 에셋 수다. 메모리 사용량이나 생성된 인스턴스 수가 아니다.
        public int AssetCount => loadedAssets.Count;

        // 로딩·정리 중에는 캐시가 바뀌는 요청을 받지 않는다.
        public bool IsBusy { get; private set; }

#if UNITY_EDITOR
        /// <summary>Editor 조회용 값이다. 실제 에셋이나 로딩 핸들을 보관하지 않는다.</summary>
        public readonly struct LoadInfo
        {
            public string Address { get; }
            public bool IsScene { get; }
            public int RequestCount { get; }
            public bool HandleIsValid { get; }
            public bool SceneIsLoaded { get; }

            // 로더가 보관 중인 값만 복사해 관찰자가 자원 수명에 영향을 주지 않게 한다.
            internal LoadInfo(string address, bool isScene, int requestCount,
                bool handleIsValid, bool sceneIsLoaded)
            {
                Address = address;
                IsScene = isScene;
                RequestCount = requestCount;
                HandleIsValid = handleIsValid;
                SceneIsLoaded = sceneIsLoaded;
            }
        }

        /// <summary>
        /// Unity 메인 스레드에서 Editor 목록을 비우고 현재 보관 상태를 복사한다.
        /// 관리자가 없으면 생성하지 않고 false를 반환한다. 진행 중인 최초 로드는 완료 후 목록에 나타난다.
        /// </summary>
        public static bool CopyLoadInfo(List<LoadInfo> results, out bool isBusy)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            AddressableManager manager = CurrentInstance;
            isBusy = manager != null && manager.IsBusy;
            if (manager == null) return false;

            foreach (var entry in manager.loadedAssets)
            {
                results.Add(new LoadInfo(entry.Key, false, entry.Value.refCount,
                    entry.Value.handle.IsValid(), false));
            }
            foreach (var entry in manager.loadedScenes)
            {
                bool valid = entry.Value.handle.IsValid();
                bool loaded = valid && entry.Value.handle.IsDone &&
                    entry.Value.handle.Status == AsyncOperationStatus.Succeeded &&
                    entry.Value.handle.Result.Scene.isLoaded;
                results.Add(new LoadInfo(entry.Key, true, entry.Value.refCount, valid, loaded));
            }
            return true;
        }
#endif

        /// <summary>키를 사용하는 현재 요청 수를 반환한다. 보관하지 않는 키는 0이다.</summary>
        public int GetRefCount(string key) =>
            key != null && loadedAssets.TryGetValue(key, out LoadedAsset asset)
                ? asset.refCount
                : 0;

        /// <summary>
        /// 키로 원본 에셋을 로드한다. 같은 키와 타입이면 캐시를 재사용하며 요청마다 사용 횟수를 늘린다.
        /// </summary>
        /// <returns>로딩 완료 후 원본 에셋을 반환한다. 로딩 실패나 다른 타입의 재요청은 예외로 알린다.</returns>
        public async Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
        {
            EnsureIdle();
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("에셋 키가 필요합니다.", nameof(key));

            if (loadedAssets.TryGetValue(key, out LoadedAsset cached))
            {
                if (cached.assetType != typeof(T))
                    throw new InvalidOperationException("같은 키를 다른 에셋 타입으로 요청했습니다: " + key);

                cached.refCount++;
                return (T)cached.handle.Result;
            }

            IsBusy = true;
            AsyncOperationHandle<T> handle = default;
            bool stored = false;
            try
            {
                handle = Addressables.LoadAssetAsync<T>(key);
                await handle.Task;
                if (handle.Status != AsyncOperationStatus.Succeeded)
                    throw new InvalidOperationException("에셋 로딩 실패: " + key, handle.OperationException);

                loadedAssets.Add(key, new LoadedAsset
                {
                    handle = handle,
                    assetType = typeof(T),
                    refCount = 1
                });
                stored = true;
                return handle.Result;
            }
            finally
            {
                if (!stored && handle.IsValid()) Addressables.Release(handle);
                IsBusy = false;
            }
        }

        /// <summary>키의 사용 요청을 한 번 반환한다. 실제 핸들은 CleanUpUnusedAssets에서 정리한다.</summary>
        /// <returns>횟수를 줄였으면 true, 키가 없거나 이미 0이면 false를 반환한다.</returns>
        public bool Release(string key)
        {
            EnsureIdle();
            if (key == null || !loadedAssets.TryGetValue(key, out LoadedAsset asset) ||
                asset.refCount == 0)
            {
                return false;
            }

            asset.refCount--;
            return true;
        }

        /// <summary>사용 요청 수가 0인 원본 에셋의 핸들만 Addressables에 반환한다.</summary>
        public void CleanUpUnusedAssets()
        {
            EnsureIdle();
            IsBusy = true;
            try
            {
                var unusedKeys = new List<string>();
                foreach (KeyValuePair<string, LoadedAsset> entry in loadedAssets)
                    if (entry.Value.refCount == 0) unusedKeys.Add(entry.Key);

                foreach (string key in unusedKeys)
                {
                    Addressables.Release(loadedAssets[key].handle);
                    loadedAssets.Remove(key);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        // 일반 에셋과 해제 API가 다른 씬 핸들을 별도로 기록한다.
        private sealed class LoadedScene
        {
            public AsyncOperationHandle<SceneInstance> handle;
            public int refCount;
        }

        private readonly Dictionary<string, LoadedScene> loadedScenes =
            new Dictionary<string, LoadedScene>(StringComparer.Ordinal);

        // 씬 내부 에셋 수가 아니라, 캐시에 보관된 씬 수다.
        /// <summary>보관 중인 씬 수를 반환한다. 일반 에셋 수와 구분한다.</summary>
        public int SceneCount => loadedScenes.Count;

        /// <summary>씬 주소의 사용 요청 수를 반환한다. 보관하지 않는 주소는 0이다.</summary>
        public int GetSceneRefCount(string address) =>
            address != null && loadedScenes.TryGetValue(address, out LoadedScene map) ? map.refCount : 0;

        /// <summary>주소로 씬을 추가 로딩한다. 같은 주소는 재사용하고 사용 횟수를 늘린다.</summary>
        /// <returns>완료된 씬을 반환한다. 잘못된 주소·진행 중 재요청·로딩 실패는 예외로 알린다.</returns>
        public async Task<Scene> LoadSceneAsync(string address)
        {
            EnsureIdle();
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("맵 Address가 필요합니다.", nameof(address));

            if (loadedScenes.TryGetValue(address, out LoadedScene cachedScene))
            {
                cachedScene.refCount++;
                return cachedScene.handle.Result.Scene;
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

                loadedScenes.Add(address, new LoadedScene { handle = handle, refCount = 1 });
                stored = true;
                return handle.Result.Scene;
            }
            finally
            {
                if (!stored && handle.IsValid()) Addressables.Release(handle);
                IsBusy = false;
            }
        }

        /// <summary>씬 사용 요청을 한 번 반환한다. 감소하면 true이며 미등록 주소나 사용 0인 씬은 false다.</summary>
        public bool ReleaseScene(string address)
        {
            EnsureIdle();
            if (address == null || !loadedScenes.TryGetValue(address, out LoadedScene map) || map.refCount == 0)
                return false;

            map.refCount--;
            return true;
        }

        /// <summary>사용 횟수 0인 씬을 해제하고 핸들 기록을 제거한다. 완료까지 기다리며 실패는 예외로 알린다.</summary>
        public async Task CleanUpUnusedScenesAsync()
        {
            EnsureIdle();
            IsBusy = true;
            try
            {
                var unused = new List<string>();
                foreach (var pair in loadedScenes)
                    if (pair.Value.refCount == 0) unused.Add(pair.Key);

                foreach (string address in unused)
                {
                    LoadedScene map = loadedScenes[address];
                    var unloading = Addressables.UnloadSceneAsync(map.handle, autoReleaseHandle: false);
                    try
                    {
                        await unloading.Task;
                        if (unloading.Status != AsyncOperationStatus.Succeeded)
                            throw new InvalidOperationException("맵 해제 실패: " + address, unloading.OperationException);

                        loadedScenes.Remove(address);
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

        // 로딩·정리 도중 캐시를 변경하면 핸들 보관·반환 순서가 어긋나므로 요청을 거절한다.
        private void EnsureIdle()
        {
            if (IsBusy)
                throw new InvalidOperationException("진행 중인 에셋·씬 로딩·정리가 끝난 뒤 요청하세요.");
        }
    }
}
