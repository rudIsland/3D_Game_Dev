using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Data;
using Item;
using Interaction;
using Manager;
using Core;
using UnityEngine.SceneManagement;

namespace Scene
{
    // 각 플레이 맵에 배치한다. 이 씬의 설정·배치로 객체를 만들고 이탈 때 직접 정리한다.
    [DisallowMultipleComponent]
    public sealed class MapPlayScene : GameScene
    {
        [SerializeField] private MapScene map;
        private MapEnemySpawner enemySpawner;
        private ItemContainer items;
        private readonly List<GroundQuestController> questControllers = new List<GroundQuestController>();
        private QuestContainer quests;

        private StartScene sharedScene;
        private bool playing;

        /// <summary>맵의 배치 설정과 유지 객체를 연결한다.</summary>
        public override void Init()
        {
            foreach (GameObject root in SceneManager.GetSceneByName(ConstantValid.StartSceneName).GetRootGameObjects())
            {
                sharedScene = root.GetComponentInChildren<StartScene>(true);
                if (sharedScene != null) break;
            }
            map.Init();
        }

        /// <summary>전역 데이터를 읽어 같은 씬의 아이템·교환·퀘스트 판정을 연결한다.</summary>
        public override async Task Create()
        {
            await sharedScene.Create(map.PlayerSpawnPoint);
            if (!Application.isPlaying) return;
            ItemCatalog itemData = ItemDataContainer.Instance.Catalog;
            quests = sharedScene.Quests;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (ItemSpawnManager spawner in root.GetComponentsInChildren<ItemSpawnManager>(true))
                {
                    items ??= ItemContainer.Create(gameObject.scene);
                    spawner.Connect(items, itemData);
                }
                foreach (GroundQuestController quest in root.GetComponentsInChildren<GroundQuestController>(true))
                {
                    quests.Connect(quest);
                    questControllers.Add(quest);
                }
                foreach (ItemExchangeInteraction exchange in root.GetComponentsInChildren<ItemExchangeInteraction>(true))
                {
                    itemData.TryGetItem(exchange.CostItemType, out ItemCatalogEntry cost);
                    itemData.TryGetItem(exchange.RewardItemType, out ItemCatalogEntry reward);
                    exchange.Connect(cost.ItemDefinition, reward.ItemDefinition);
                }
            }
            enemySpawner = new MapEnemySpawner(gameObject.scene, EnemyDataContainer.Instance);
        }

        /// <summary>플레이어 활성화 후 이 맵의 몬스터와 HUD·환경음을 연결한다.</summary>
        public override void Enable()
        {
            sharedScene.Enable();
            var enemies = enemySpawner.Create(sharedScene.Player.transform);
            sharedScene.Hud?.AddEnemies(enemies);
            map.PlayAmbient();
            playing = true;
        }

        /// <summary>이 맵의 몬스터와 퀘스트 판정을 게임 순서에서 갱신한다.</summary>
        public void Tick(float deltaTime)
        {
            sharedScene.Tick(deltaTime);
            enemySpawner?.Tick(deltaTime);
            quests?.Tick(deltaTime);
        }

        // 이 맵이 실행 중일 때만 플레이어·적·퀘스트를 갱신한다.
        private void Update()
        {
            if (playing) Tick(Time.deltaTime);
        }

        /// <summary>입력·새 소환과 환경음을 멈춘다.</summary>
        public override void Disable()
        {
            playing = false;
            sharedScene?.Disable();
            enemySpawner?.StopSpawning();
            map.StopAmbient();
        }

        /// <summary>이 씬의 HUD 적 구독·퀘스트 판정·몬스터를 정리하며 진행 기록은 유지한다.</summary>
        public override void DisconnectPlayer()
        {
            if (enemySpawner?.Enemies != null) sharedScene.Hud?.RemoveEnemies(enemySpawner.Enemies);
            enemySpawner?.Release();
            enemySpawner = null;
            foreach (GroundQuestController quest in questControllers) quests?.Disconnect(quest);
            questControllers.Clear();
            quests = null;
        }

        /// <summary>씬 언로드 전에 맵 소유 객체와 풀을 반환한다.</summary>
        public override void Release()
        {
            DisconnectPlayer();
            items?.Dispose();
            items = null;
        }

    }
}
