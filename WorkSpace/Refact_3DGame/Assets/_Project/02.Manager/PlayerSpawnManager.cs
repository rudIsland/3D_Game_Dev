using System;
using System.Threading;
using System.Threading.Tasks;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Player;
using Object = UnityEngine.Object;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Manager
{
    // 플레이어 원본 요청과 생성 객체를 소유한다. StartScene이 생성·활성화·반환을 요청한다.
    public sealed partial class PlayerSpawnManager
    {
        private readonly AddressableManager assets;
        private readonly Camera camera;
        private readonly UnityScene scene;
        private readonly string address;
        private readonly CancellationToken exitToken;
        private GameObject prefab;
        private GameObject objects;
        private bool loaded;
        private bool quitting;

        /// <summary>다른 기능에 연결할 플레이어다. 생성·반환은 이 담당자가 수행한다.</summary>
        public PlayerController Player { get; private set; }

        /// <summary>플레이어 로더·카메라·생성 씬·주소를 전달받는다.</summary>
        public PlayerSpawnManager(AddressableManager assets, Camera camera, UnityScene scene,
            string address, CancellationToken exitToken)
        {
            if (camera == null || !camera.isActiveAndEnabled || camera.gameObject.scene != scene)
                throw new ArgumentException("Start 씬의 활성 MainCamera가 필요합니다.", nameof(camera));
            CinemachineBrain brain = camera.GetComponent<CinemachineBrain>();
            if (brain == null || !brain.isActiveAndEnabled || camera.GetComponent<UniversalAdditionalCameraData>() == null)
                throw new InvalidOperationException("MainCamera에 활성 CinemachineBrain과 URP 카메라 설정이 필요합니다.");
            this.assets = assets;
            this.camera = camera;
            this.scene = scene;
            this.address = address;
            this.exitToken = exitToken;
        }

        /// <summary>원본 요청을 보관한다. 생성 전 종료 여부는 호출자가 확인한다.</summary>
        public async Task Load()
        {
            prefab = await assets.LoadAssetAsync<GameObject>(address);
            loaded = true;
        }

        /// <summary>초기화 전에 입력이 켜지지 않도록 비활성 부모 아래에 생성한다.</summary>
        public void Create()
        {
            objects = new GameObject("PlayerObjects");
            objects.SetActive(false);
            SceneManager.MoveGameObjectToScene(objects, scene);
            GameObject instance = Object.Instantiate(prefab, objects.transform, false);
            instance.name = address;
            Player = instance.GetComponent<PlayerController>();
            if (Player == null || !Player.enabled || !instance.activeSelf)
                throw new InvalidOperationException("PlayerRoot에 활성 PlayerController가 필요합니다.");
        }

        /// <summary>맵이 위치를 적용한 뒤 플레이어에 카메라와 설정을 전달한다.</summary>
        public void Init(PlayerCharacterConfig config) => Player.Init(camera.transform, config);

        /// <summary>준비한 플레이어를 활성화하고 입력 준비 상태를 확인한다.</summary>
        public void Enable()
        {
            objects.SetActive(true);
            Player.Enable();
            if (!Player.RuntimeUnit.IsEnabled)
                throw new InvalidOperationException("PlayerRoot 입력 준비에 실패했습니다.");
        }

        /// <summary>반환 전에 플레이어 입력과 행동을 중지한다.</summary>
        public void Disable()
        {
            if (Player != null) Player.Disable();
        }

        /// <summary>플레이어 해제와 파괴가 끝난 뒤 자신이 받은 원본 요청을 반환한다.</summary>
        public async Task Release()
        {
            // 비활성 생성 실패에서도 OnDestroy에 의존하지 않고 해제한다.
            if (Player != null) Player.Release();
            Player = null;
            if (IsQuitting()) return;
            if (objects != null)
            {
                objects.SetActive(false);
                Object.Destroy(objects);
                while (objects != null && !IsQuitting()) await Task.Yield();
                objects = null;
            }
            while (assets.IsBusy && !IsQuitting()) await Task.Yield();
            if (IsQuitting()) return;
            prefab = null;
            if (loaded) { assets.Release(address); loaded = false; }
            assets.CleanUpUnusedAssets();
        }

        /// <summary>Play 종료 후 새 비동기 반환을 진행하지 않도록 기록한다.</summary>
        public void Quit() => quitting = true;

        private bool IsQuitting() => quitting || exitToken.IsCancellationRequested || !Application.isPlaying;
    }
}
