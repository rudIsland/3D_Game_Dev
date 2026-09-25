using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Core;
using Quest;
using UI;
using Object = UnityEngine.Object;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Manager
{
    // HUD 원본 요청·생성 객체·표시 연결을 소유한다. 플레이어와 맵의 수명은 변경하지 않는다.
    public sealed partial class HudSpawnManager
    {
        private readonly AddressableManager assets;
        private readonly UnityScene scene;
        private readonly string address;
        private readonly CancellationToken exitToken;
        private GameObject prefab;
        private GameObject objects;
        private CombatHudController view;
        private MinimapInfoController minimap;
        private bool loaded;
        private bool quitting;

        /// <summary>게임 흐름이 현재 맵의 적을 연결할 표시 컨테이너다.</summary>
        public HudContainer Container { get; private set; }

        /// <summary>HUD 로더·생성 씬·주소를 전달받는다.</summary>
        public HudSpawnManager(AddressableManager assets, UnityScene scene,
            string address, CancellationToken exitToken)
        {
            this.assets = assets;
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

        /// <summary>비활성 HUD를 생성하고 플레이어와 퀘스트를 표시 컨테이너에 연결한다.</summary>
        public void Create(IPlayerHudSource player, GroundQuestProgress quest)
        {
            objects = new GameObject("HudObjects");
            objects.SetActive(false);
            SceneManager.MoveGameObjectToScene(objects, scene);
            GameObject instance = Object.Instantiate(prefab, objects.transform, false);
            instance.name = address;
            view = instance.GetComponent<CombatHudController>();
            CombatHudToolkitView toolkit = instance.GetComponent<CombatHudToolkitView>();
            UIDocument document = instance.GetComponent<UIDocument>();
            minimap = instance.GetComponent<MinimapInfoController>();
            if (view == null || !view.enabled || toolkit == null || !toolkit.enabled ||
                document == null || !document.enabled || document.panelSettings == null ||
                document.visualTreeAsset == null || minimap == null || !minimap.enabled || !instance.activeSelf)
                throw new InvalidOperationException("CombatHud의 Controller·ToolkitView·UIDocument·MinimapInfo 연결이 필요합니다.");
            Container = HudContainer.Create(player);
            Container.Add(view);
            Container.Add(minimap, quest);
        }

        /// <summary>표시 연결을 마친 HUD를 활성화하고 화면 준비 상태를 확인한다.</summary>
        public void Enable()
        {
            objects.SetActive(true);
            if (!view.IsReady || !minimap.IsReady)
                throw new InvalidOperationException("CombatHud 구독·화면 준비에 실패했습니다.");
        }

        /// <summary>구독·미니맵 텍스처와 객체를 정리한 뒤 원본 요청을 반환한다.</summary>
        public async Task Release()
        {
            Container?.Dispose();
            Container = null;
            if (IsQuitting()) return;
            if (objects != null)
            {
                objects.SetActive(false);
                Object.Destroy(objects);
                while (objects != null && !IsQuitting()) await Task.Yield();
                objects = null;
            }
            view = null;
            minimap = null;
            while (assets.IsBusy && !IsQuitting()) await Task.Yield();
            if (IsQuitting()) return;
            prefab = null;
            if (loaded) { assets.Release(address); loaded = false; }
            assets.CleanUpUnusedAssets();
        }

        /// <summary>Play 종료에서는 표시 연결만 해제하고 객체 파괴는 Unity 종료에 맡긴다.</summary>
        public void Quit()
        {
            quitting = true;
            Container?.Dispose();
            Container = null;
        }

        private bool IsQuitting() => quitting || exitToken.IsCancellationRequested || !Application.isPlaying;
    }
}
