using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace World
{
    // 씬 단위 로딩과 해제를 관리한다. 배치 객체와 공용 에셋의 참조는 각 씬이 소유한다.
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class WorldObjectContainer : MonoBehaviour
    {
        private const string EnvironmentRootName = "Setup&Lights";
        [Serializable]
        private sealed class SceneSource
        {
            public string name;
            public string scenePath;
        }

        [SerializeField] private SceneSource[] sources =
        {
            new SceneSource { name = "Ground", scenePath = "Assets/_Project/0_Scenes/Ground.unity" },
            new SceneSource { name = "Underground", scenePath = "Assets/_Project/0_Scenes/Underground.unity" }
        };
        [SerializeField] private string loadOnStart = "Ground";
        [SerializeField] private bool pauseWhileLoading = true;
        [SerializeField] private bool isBusy;
        [SerializeField] private string lastError;
        [SerializeField] private int loadedCount;

        private readonly Dictionary<string, Scene> loadedScenes = new Dictionary<string, Scene>(StringComparer.Ordinal);
        private float previousTimeScale;
        private bool timePaused;
        private bool isClosing;
        private AsyncOperation pendingLoad;
        private string pendingLoadPath;
        private AsyncOperation pendingUnload;
        private int unloadingSceneHandle;
        private bool discardPendingLoad;
        private int releasesInFlight;
        public bool IsBusy => isBusy;
        public string LastError => lastError;
        public int LoadedCount => loadedScenes.Count;

        private void Awake()
        {
            SceneManager.sceneUnloaded += ForgetScene;
        }

        private void Start()
        {
            // 로딩 전에는 공통 씬을 사용하고, 완료 후 해당 지역의 환경을 적용한다.
            SceneManager.SetActiveScene(gameObject.scene);
            if (!string.IsNullOrEmpty(loadOnStart)) Load(loadOnStart);
        }

        private void OnEnable()
        {
            if (!isClosing) return;
            isClosing = false;
            SceneManager.sceneUnloaded += ForgetScene;
        }

        public bool IsLoaded(string name) => name != null && loadedScenes.ContainsKey(name);

        public bool Load(string name)
        {
            if (!CanStart() || string.IsNullOrWhiteSpace(name) || loadedScenes.ContainsKey(name)) return false;
            string path = null;
            foreach (SceneSource source in sources)
                if (source != null && source.name == name) { path = source.scenePath; break; }
            if (string.IsNullOrEmpty(path) || path == gameObject.scene.path || !Application.CanStreamedLevelBeLoaded(path))
            {
                lastError = "불러올 씬의 이름과 빌드 등록을 확인하세요: " + name;
                return false;
            }
            foreach (var pair in loadedScenes)
                if (pair.Value.path == path) return false;
            StartOperation(LoadScene(name, path));
            return true;
        }

        public bool Unload(string name)
        {
            if (!CanStart() || name == null || !loadedScenes.TryGetValue(name, out Scene scene)) return false;
            StartOperation(UnloadScene(name, scene));
            return true;
        }

        // 두 맵을 함께 열어 둔 경우 사용할 환경을 명시적으로 선택한다.
        public bool ApplyEnvironment(string name)
        {
            if (!CanStart() || name == null || !loadedScenes.TryGetValue(name, out Scene scene) || !scene.isLoaded) return false;
            SelectEnvironment(scene);
            return true;
        }

        private void SelectEnvironment(Scene scene)
        {
            SceneManager.SetActiveScene(scene);
            foreach (var pair in loadedScenes)
            {
                if (!pair.Value.isLoaded) continue;
                bool enable = pair.Value == scene;
                foreach (GameObject root in pair.Value.GetRootGameObjects())
                    if (root.name == EnvironmentRootName && root.activeSelf != enable) root.SetActive(enable);
            }
        }

        private bool CanStart() => Application.isPlaying && isActiveAndEnabled && !isBusy && !isClosing &&
            pendingLoad == null && pendingUnload == null && releasesInFlight == 0;

        private void StartOperation(IEnumerator work)
        {
            isBusy = true;
            lastError = null;
            if (pauseWhileLoading)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                timePaused = true;
            }
            StartCoroutine(RunOperation(work));
        }

        private IEnumerator RunOperation(IEnumerator work)
        {
            try
            {
                // 요청을 호출한 Awake·Tick이 끝난 다음 Unity 씬 작업을 실행한다.
                yield return null;
                while (true)
                {
                    bool hasNext;
                    try { hasNext = work.MoveNext(); }
                    catch (Exception exception)
                    {
                        lastError = exception.Message;
                        Debug.LogException(exception, this);
                        break;
                    }
                    if (!hasNext) break;
                    yield return work.Current;
                }
            }
            finally
            {
                (work as IDisposable)?.Dispose();
                if (timePaused) Time.timeScale = previousTimeScale;
                timePaused = false;
                isBusy = false;
                loadedCount = loadedScenes.Count;
            }
        }

        private IEnumerator LoadScene(string name, string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            if (!scene.isLoaded)
            {
                AsyncOperation loading = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                if (loading == null) throw new InvalidOperationException("씬 로딩을 시작하지 못했습니다: " + name);
                pendingLoadPath = path;
                discardPendingLoad = false;
                pendingLoad = loading;
                loading.completed += FinishLoading;
                yield return loading;
                scene = SceneManager.GetSceneByPath(path);
            }
            if (!scene.isLoaded) throw new InvalidOperationException("씬 로딩을 완료하지 못했습니다: " + name);
            loadedScenes.Add(name, scene);
            SelectEnvironment(scene);
            // 배치 컴포넌트의 Start에서 라이트맵 복원이 끝난 뒤 정리한다.
            yield return null;
            RemoveUnusedLightmaps();
        }

        private IEnumerator UnloadScene(string name, Scene scene)
        {
            if (SceneManager.GetActiveScene() == scene)
            {
                Scene next = gameObject.scene;
                foreach (var pair in loadedScenes)
                    if (pair.Value != scene && pair.Value.isLoaded) { next = pair.Value; break; }
                SelectEnvironment(next);
            }
            if (scene.isLoaded)
            {
                AsyncOperation unloading = SceneManager.UnloadSceneAsync(scene);
                if (unloading == null) throw new InvalidOperationException("씬 해제를 시작하지 못했습니다: " + name);
                pendingUnload = unloading;
                unloadingSceneHandle = scene.handle;
                unloading.completed += FinishUnloading;
                yield return unloading;
                pendingUnload = null;
                unloadingSceneHandle = 0;
                if (scene.isLoaded) throw new InvalidOperationException("씬 해제를 완료하지 못했습니다: " + name);
            }
            loadedScenes.Remove(name);
            yield return null;
            RemoveUnusedLightmaps();
            yield return Resources.UnloadUnusedAssets();
        }

        private void ForgetScene(Scene scene)
        {
            string removed = null;
            foreach (var pair in loadedScenes)
                if (pair.Value == scene) { removed = pair.Key; break; }
            if (removed != null) loadedScenes.Remove(removed);
            loadedCount = loadedScenes.Count;
        }

        private void FinishLoading(AsyncOperation operation)
        {
            operation.completed -= FinishLoading;
            pendingLoad = null;
            if (discardPendingLoad) ReleaseScene(SceneManager.GetSceneByPath(pendingLoadPath));
            pendingLoadPath = null;
        }

        private void FinishUnloading(AsyncOperation operation)
        {
            operation.completed -= FinishUnloading;
            pendingUnload = null;
            unloadingSceneHandle = 0;
        }

        private void ReleaseScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || SceneManager.sceneCount <= 1) return;
            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            if (operation != null) { releasesInFlight++; operation.completed += FinishReleasedScene; }
        }

        private void FinishReleasedScene(AsyncOperation operation)
        {
            operation.completed -= FinishReleasedScene;
            releasesInFlight--;
            if (releasesInFlight > 0) return;
            RemoveUnusedLightmaps();
            Resources.UnloadUnusedAssets();
        }

        private void OnDisable()
        {
            if (isClosing) return;
            isClosing = true;
            discardPendingLoad = true;
            StopAllCoroutines();
            SceneManager.sceneUnloaded -= ForgetScene;
            if (timePaused) Time.timeScale = previousTimeScale;
            if (Application.isPlaying && gameObject.scene.isLoaded && loadedScenes.Count > 0)
                SelectEnvironment(gameObject.scene);
            if (pendingUnload != null) { releasesInFlight++; pendingUnload.completed += FinishReleasedScene; }
            var scenes = new List<Scene>(loadedScenes.Count);
            foreach (var pair in loadedScenes)
            {
                if (pair.Value.handle != unloadingSceneHandle) scenes.Add(pair.Value);
            }
            loadedScenes.Clear();
            loadedCount = 0;
            isBusy = false;
            timePaused = false;
            if (Application.isPlaying)
                foreach (Scene scene in scenes) ReleaseScene(scene);
        }

        // Unity의 씬 해제와 기존 Bake 복원이 끝난 뒤 사용 중인 슬롯만 보존한다.
        private static void RemoveUnusedLightmaps()
        {
            LightmapData[] current = LightmapSettings.lightmaps;
            if (current.Length == 0) return;
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Terrain[] terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var rendererSlots = new int[renderers.Length];
            var terrainSlots = new int[terrains.Length];
            var used = new bool[current.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                int slot = rendererSlots[i] = renderers[i].lightmapIndex;
                if (slot >= 0 && slot < used.Length) used[slot] = true;
            }
            for (int i = 0; i < terrains.Length; i++)
            {
                int slot = terrainSlots[i] = terrains[i].lightmapIndex;
                if (slot >= 0 && slot < used.Length) used[slot] = true;
            }
            var kept = new List<LightmapData>(current.Length);
            var remap = new int[current.Length];
            for (int i = 0; i < current.Length; i++)
            {
                remap[i] = -1;
                if (!used[i]) continue;
                LightmapData entry = current[i];
                for (int j = 0; j < kept.Count; j++)
                    if (kept[j].lightmapColor == entry.lightmapColor && kept[j].lightmapDir == entry.lightmapDir && kept[j].shadowMask == entry.shadowMask)
                    { remap[i] = j; break; }
                if (remap[i] < 0) { remap[i] = kept.Count; kept.Add(entry); }
            }
            if (kept.Count == current.Length) return;
            LightmapSettings.lightmaps = kept.ToArray();
            for (int i = 0; i < renderers.Length; i++)
                if (rendererSlots[i] >= 0 && rendererSlots[i] < remap.Length) renderers[i].lightmapIndex = remap[rendererSlots[i]];
            for (int i = 0; i < terrains.Length; i++)
                if (terrainSlots[i] >= 0 && terrainSlots[i] < remap.Length) terrains[i].lightmapIndex = remap[terrainSlots[i]];
        }
    }
}
