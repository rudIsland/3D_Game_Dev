using System;
using System.Threading;
using System.Threading.Tasks;
using Characters.Player.Lifecycle;
using Cinemachine;
using Core.ConstantValid;
using GameUI;
using GameUI.CombatHud;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using World.Loading;
using Object = UnityEngine.Object;

namespace World.Management
{
    // 첫 맵과 플레이어·HUD의 생성 순서, 생성 객체, 성공한 로드 요청을 소유한다.
    public sealed class GameManager
    {
        private const string PlayerAddress = "PlayerRoot";
        private const string HudAddress = "CombatHud";

        private Camera mainCamera;
        private Scene startScene;
        private MapManager maps;
        private AddressableManager assets;
        private MapScene map;
        private GameObject gameObjects;
        private PlayerController player;
        private HudContainer hud;
        private Task startTask;
        private Task releaseTask;
        private CancellationToken exitToken;
        private bool initialized;
        private bool releaseRequested;
        private bool quitting;
        private bool mapLoaded;
        private bool playerLoaded;
        private bool hudLoaded;

        /// <summary>플레이어·HUD·환경음 준비를 마쳐 갱신할 수 있는지 반환한다.</summary>
        public bool IsReady { get; private set; }

        /// <summary>Start 씬과 활성 카메라를 받는다. 같은 관리자의 재초기화는 허용하지 않는다.</summary>
        public void Init(Camera camera, Scene startScene)
        {
            if (initialized) throw new InvalidOperationException("GameManager.Init은 한 번만 호출하세요.");
            if (!startScene.IsValid() || !startScene.isLoaded)
                throw new ArgumentException("로드된 Start 씬이 필요합니다.", nameof(startScene));
            if (camera == null || !camera.isActiveAndEnabled || camera.gameObject.scene != startScene)
                throw new ArgumentException("Start 씬의 활성 MainCamera가 필요합니다.", nameof(camera));
            AudioListener listener = camera.GetComponent<AudioListener>();
            if (listener == null || !listener.isActiveAndEnabled)
                throw new InvalidOperationException("MainCamera에 활성 AudioListener가 필요합니다.");
            CinemachineBrain brain = camera.GetComponent<CinemachineBrain>();
            if (brain == null || !brain.isActiveAndEnabled || camera.GetComponent<UniversalAdditionalCameraData>() == null)
                throw new InvalidOperationException("MainCamera에 활성 CinemachineBrain과 URP 카메라 설정이 필요합니다.");

            mainCamera = camera;
            this.startScene = startScene;
            maps = MapManager.Instance;
            assets = AddressableManager.Instance;
            exitToken = Application.exitCancellationToken;
            initialized = true;
        }

        /// <summary>Ground → 플레이어 → HUD를 순서대로 준비한다. 중복 호출은 같은 작업을 반환한다.</summary>
        public Task Start()
        {
            if (!initialized) throw new InvalidOperationException("GameManager.Init을 먼저 호출하세요.");
            if (releaseRequested) throw new InvalidOperationException("반환을 요청한 게임은 다시 시작할 수 없습니다.");
            if (startTask == null && PlayerController.Instance != null)
                throw new InvalidOperationException("이미 준비된 플레이어가 있습니다. 기존 게임을 반환한 뒤 시작하세요.");
            return startTask ??= StartGame();
        }

        // 완료된 요청을 먼저 기록한 뒤 종료 여부를 확인해 로딩 중 반환도 빠뜨리지 않는다.
        private async Task StartGame()
        {
            string step = "Ground 로딩";
            try
            {
                Scene ground = await maps.LoadAsync(ConstantValid.GroundMapAddress);
                mapLoaded = true;
                if (ShouldStop()) return;

                step = "Ground MapScene 연결";
                map = FindMapScene(ground);
                map.Init();
                if (!SceneManager.SetActiveScene(ground))
                    throw new InvalidOperationException("Ground를 활성 씬으로 지정하지 못했습니다.");

                gameObjects = new GameObject("GameObjects");
                gameObjects.SetActive(false);
                SceneManager.MoveGameObjectToScene(gameObjects, startScene);

                step = "PlayerRoot 로딩";
                GameObject playerPrefab = await assets.LoadAssetAsync<GameObject>(PlayerAddress);
                playerLoaded = true;
                if (ShouldStop()) return;

                step = "PlayerRoot 생성·초기화";
                GameObject playerObject = Object.Instantiate(playerPrefab, gameObjects.transform, false);
                playerObject.name = PlayerAddress;
                Transform spawn = map.PlayerSpawnPoint;
                playerObject.transform.SetPositionAndRotation(
                    spawn.position, Quaternion.Euler(0f, spawn.eulerAngles.y, 0f));
                player = playerObject.GetComponent<PlayerController>();
                if (player == null || !player.enabled || !playerObject.activeSelf)
                    throw new InvalidOperationException("PlayerRoot에 활성 PlayerController가 필요합니다.");
                player.Init(mainCamera.transform);

                step = "CombatHud 로딩";
                GameObject hudPrefab = await assets.LoadAssetAsync<GameObject>(HudAddress);
                hudLoaded = true;
                if (ShouldStop()) return;

                step = "CombatHud 생성·연결";
                GameObject hudObject = Object.Instantiate(hudPrefab, gameObjects.transform, false);
                hudObject.name = HudAddress;
                CombatHudController view = hudObject.GetComponent<CombatHudController>();
                CombatHudToolkitView toolkit = hudObject.GetComponent<CombatHudToolkitView>();
                UIDocument document = hudObject.GetComponent<UIDocument>();
                if (view == null || !view.enabled || toolkit == null || !toolkit.enabled ||
                    document == null || !document.enabled || document.panelSettings == null ||
                    document.visualTreeAsset == null || !hudObject.activeSelf)
                    throw new InvalidOperationException("CombatHud의 Controller·ToolkitView·UIDocument 연결이 필요합니다.");
                hud = HudContainer.Create(player);
                hud.Add(view);

                step = "플레이어·HUD 활성화";
                gameObjects.SetActive(true);
                if (!player.RuntimeUnit.IsEnabled || !view.IsReady)
                    throw new InvalidOperationException("PlayerRoot 입력 또는 CombatHud 구독·화면 준비에 실패했습니다.");
                map.PlayAmbient();
                IsReady = true;
            }
            catch (Exception exception)
            {
                IsReady = false;
                // Play 종료 후에는 Unity가 객체와 씬을 정리한다.
                if (quitting || exitToken.IsCancellationRequested || !Application.isPlaying) return;
                try { await ReleaseObjects(); }
                catch (Exception cleanupException) { Debug.LogException(cleanupException); }
                throw new InvalidOperationException("게임 시작 실패 [" + step + "]", exception);
            }
        }

        // 로드된 맵 내부만 한 번 살펴보고 누락·중복을 설정 오류로 알린다.
        private static MapScene FindMapScene(Scene scene)
        {
            MapScene result = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MapScene candidate in root.GetComponentsInChildren<MapScene>(true))
                {
                    if (result != null) throw new InvalidOperationException("Ground에는 MapScene이 하나만 있어야 합니다.");
                    result = candidate;
                }
            }
            if (result == null || !result.isActiveAndEnabled)
                throw new InvalidOperationException("Ground에 활성 MapScene이 필요합니다.");
            return result;
        }

        // 종료되거나 반환 요청을 받은 로딩은 다음 생성 단계로 넘어가지 않는다.
        private bool ShouldStop() => releaseRequested || quitting ||
            exitToken.IsCancellationRequested || !Application.isPlaying;

        /// <summary>준비 완료 상태에서만 플레이어의 입력·이동·스태미나를 갱신한다.</summary>
        public void Tick(float deltaTime)
        {
            if (IsReady) player.Tick(deltaTime);
        }

        /// <summary>갱신을 멈추고 로딩 완료·객체 파괴·요청 반환까지 기다린다. 중복 호출은 같은 작업이다.</summary>
        public Task Release()
        {
            releaseRequested = true;
            IsReady = false;
            return releaseTask ??= ReleaseGame();
        }

        // 진행 중인 시작 작업과 반환 작업이 AddressableManager를 동시에 변경하지 않게 한다.
        private async Task ReleaseGame()
        {
            if (startTask != null)
            {
                try { await startTask; }
                catch { /* 시작 호출자가 단계가 포함된 오류를 기록한다. 반환은 계속한다. */ }
            }
            await ReleaseObjects();
        }

        // 입력·구독·객체를 먼저 정리하고 성공한 요청만 생성의 역순으로 반환한다.
        private async Task ReleaseObjects()
        {
            IsReady = false;
            if (player != null) player.Disable();
            hud?.Dispose();
            hud = null;
            // 비활성 상태에서 실패한 객체에는 OnDestroy가 호출되지 않을 수 있다.
            if (player != null) player.Release();
            player = null;
            if (quitting || exitToken.IsCancellationRequested || !Application.isPlaying) return;

            if (gameObjects != null)
            {
                gameObjects.SetActive(false);
                Object.Destroy(gameObjects);
                while (gameObjects != null)
                {
                    await Task.Yield();
                    if (quitting || exitToken.IsCancellationRequested || !Application.isPlaying) return;
                }
            }

            if (assets != null)
            {
                while (assets.IsBusy)
                {
                    await Task.Yield();
                    if (quitting || exitToken.IsCancellationRequested || !Application.isPlaying) return;
                }
                if (hudLoaded) { assets.Release(HudAddress); hudLoaded = false; }
                if (playerLoaded) { assets.Release(PlayerAddress); playerLoaded = false; }
                assets.CleanUpUnusedAssets();
            }
            if (map != null) map.StopAmbient();
            map = null;
            if (mapLoaded)
            {
                if (startScene.IsValid() && startScene.isLoaded) SceneManager.SetActiveScene(startScene);
                maps.Release(ConstantValid.GroundMapAddress);
                mapLoaded = false;
                await maps.CleanUpAsync();
            }
        }

        /// <summary>Play終了を記録して入力・HUD・環境音を止める。新しい非同期解放は開始しない。</summary>
        public void Quit()
        {
            quitting = true;
            releaseRequested = true;
            IsReady = false;
            if (player != null) player.Disable();
            hud?.Dispose();
            hud = null;
            if (map != null) map.StopAmbient();
        }
    }
}
