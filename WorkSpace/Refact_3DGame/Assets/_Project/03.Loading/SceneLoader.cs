using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;
using Manager;
using Scene;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Loading
{
    // 씬 정리·로드와 자신이 받은 씬 요청만 소유한다. 플레이 준비·갱신은 각 씬이 맡는다.
    public sealed partial class SceneLoader : Singleton<SceneLoader>
    {
        private readonly string startSceneName;
        private GameScene currentScene;
        private string loadedAddress;
        private Task sceneTask = Task.CompletedTask;
        private bool released;
        private bool quitting;

        /// <summary>맵을 비울 때 돌아갈 상주 씬 이름으로 로더를 등록한다.</summary>
        public static SceneLoader Create(string startSceneName) =>
            CurrentInstance ?? StoreInstance(new SceneLoader(startSceneName));

        private SceneLoader(string startSceneName) { this.startSceneName = startSceneName; }

        /// <summary>현재 씬을 정리하고 주소의 씬을 로드·실행한다. 로딩 완료까지 기다릴 작업을 반환한다.</summary>
        public Task LoadAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("씬 주소가 필요합니다.", nameof(address));
            if (released || IsQuitting()) throw new InvalidOperationException("종료 중에는 씬을 로드할 수 없습니다.");
            if (!sceneTask.IsCompleted) throw new InvalidOperationException("현재 씬 처리가 끝난 뒤 요청하세요.");
            currentScene?.Disable();
            return sceneTask = LoadScene(address);
        }

        // 씬의 실행 함수를 호출하며 엔티티 종류나 생성 방법은 조회하지 않는다.
        private async Task LoadScene(string address)
        {
            try
            {
                await ReleaseScene();
                if (released || IsQuitting()) return;
                UnityScene scene = await MapManager.Instance.LoadAsync(address);
                loadedAddress = address;
                if (released || IsQuitting()) return;
                currentScene = FindScene(scene);
                SceneManager.SetActiveScene(scene);
                currentScene.Init();
                await currentScene.Create();
                if (released || IsQuitting()) return;
                currentScene.Enable();
            }
            catch (Exception exception)
            {
                if (IsQuitting()) return;
                currentScene?.Disable();
                try { await ReleaseScene(); }
                catch (Exception cleanupException) { Debug.LogException(cleanupException); }
                throw new InvalidOperationException("씬 로딩 실패 [" + address + "]", exception);
            }
        }

        /// <summary>로딩 완료를 기다려 현재 씬과 등록을 반환한다. 중복 반환도 같은 작업을 기다린다.</summary>
        public Task Release()
        {
            if (released) return sceneTask;
            released = true;
            currentScene?.Disable();
            return sceneTask = ReleaseWhenReady(sceneTask);
        }

        private async Task ReleaseWhenReady(Task loading)
        {
            try { await loading; }
            catch { /* 로딩 호출자가 오류를 받는다. 남은 씬 요청은 정리한다. */ }
            await ReleaseScene();
            ClearInstance();
        }

        // 객체·구독 정리는 동기식이다. 비동기 씬 해제가 끝난 뒤 사용하지 않는 원본을 정리한다.
        private async Task ReleaseScene()
        {
            currentScene?.Release();
            currentScene = null;
            if (IsQuitting()) return;
            var maps = MapManager.Instance;
            while (maps.IsBusy)
            {
                await Task.Yield();
                if (IsQuitting()) return;
            }
            if (loadedAddress != null)
            {
                UnityScene start = SceneManager.GetSceneByName(startSceneName);
                if (start.IsValid() && start.isLoaded) SceneManager.SetActiveScene(start);
                maps.Release(loadedAddress);
                loadedAddress = null;
            }
            await maps.CleanUpAsync();
            if (!IsQuitting()) AddressableManager.Instance.CleanUpUnusedAssets();
        }

        /// <summary>Play 종료에서는 실행·구독만 정리하고 새 비동기 언로드를 시작하지 않는다.</summary>
        public void Quit()
        {
            quitting = true;
            released = true;
            currentScene?.Disable();
            currentScene?.Release();
            currentScene = null;
            ClearInstance();
        }

        private bool IsQuitting() => quitting || !Application.isPlaying;

        // 로드한 씬의 실행 컴포넌트 하나만 찾는다.
        private static GameScene FindScene(UnityScene scene)
        {
            GameScene result = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (GameScene candidate in root.GetComponentsInChildren<GameScene>(true))
                {
                    if (result != null) throw new InvalidOperationException(scene.name + "에는 GameScene이 하나만 있어야 합니다.");
                    result = candidate;
                }
            if (result == null || !result.isActiveAndEnabled)
                throw new InvalidOperationException(scene.name + "에 활성 GameScene이 필요합니다.");
            return result;
        }
    }
}
