using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Data;
using Player;
using Manager;
using UI;

namespace Scene
{
    // Start 씬에 배치하며 맵 이동 동안 플레이어·HUD·퀘스트 기록을 유지한다.
    [DisallowMultipleComponent]
    public sealed partial class StartScene : MonoBehaviour
    {
        private PlayerSpawnManager playerSpawn;
        private HudSpawnManager hudSpawn;
        private AudioListenerManager audioListener;
        private bool created;
        private bool quitting;

        public PlayerController Player => playerSpawn?.Player;
        public HudContainer Hud => hudSpawn?.Container;
        public QuestContainer Quests { get; private set; }

        /// <summary>Boots가 전달한 시작 입력으로 유지 객체의 생성 담당을 준비한다.</summary>
        public void Init(Camera camera, GameData data)
        {
            CancellationToken exitToken = Application.exitCancellationToken;
            var assets = AddressableManager.Instance;
            playerSpawn = new PlayerSpawnManager(assets, camera, gameObject.scene, data.PlayerAddress, exitToken);
            hudSpawn = new HudSpawnManager(assets, gameObject.scene, data.HudAddress, exitToken);
            audioListener = new AudioListenerManager(camera);
        }

        /// <summary>첫 진입은 유지 객체를 만들고 이후 진입은 같은 플레이어를 시작 위치에 배치한다.</summary>
        public async Task Create(Transform entryPoint)
        {
            if (created)
            {
                PlacePlayer(entryPoint);
                return;
            }
            if (PlayerController.Instance != null)
                throw new InvalidOperationException("이미 준비된 플레이어가 있습니다.");
            await playerSpawn.Load();
            if (IsQuitting()) return;
            playerSpawn.Create();
            Player.transform.SetPositionAndRotation(entryPoint.position, Quaternion.Euler(0f, entryPoint.eulerAngles.y, 0f));
            playerSpawn.Init(PlayerDataContainer.Instance.Config);
            Quests = QuestContainer.Create(Player, QuestDataContainer.Instance.Ground);
            await hudSpawn.Load();
            if (IsQuitting()) return;
            hudSpawn.Create(Player, Quests.Ground);
            created = true;
        }

        /// <summary>맵 이동 때 기존 플레이어의 이동 상태와 카메라 추적을 새 위치로 맞춘다.</summary>
        private void PlacePlayer(Transform entryPoint)
        {
            if (Player == null || !Player.TryMoveToPosition(entryPoint.position, Quaternion.Euler(0f, entryPoint.eulerAngles.y, 0f)))
                throw new InvalidOperationException("현재 플레이어를 다음 맵의 진입 위치로 이동할 수 없습니다.");
        }

        /// <summary>씬 준비 후 유지한 플레이어·HUD·리스너를 다시 활성화한다.</summary>
        public void Enable()
        {
            playerSpawn.Enable();
            if (Hud != null) hudSpawn.Enable();
            audioListener.EnableListener();
        }

        /// <summary>게임이 플레이 중일 때 유지한 플레이어를 갱신한다.</summary>
        public void Tick(float deltaTime) => Player.Tick(deltaTime);

        /// <summary>전환·반환 동안 플레이어 입력과 리스너를 중지한다.</summary>
        public void Disable()
        {
            playerSpawn?.Disable();
            audioListener?.DisableListener();
        }

        /// <summary>플레이어와 맵을 유지하며 HUD만 반환한다.</summary>
        public Task ReleaseHud() => hudSpawn.Release();

        /// <summary>맵이 플레이어 연결을 해제한 뒤 HUD·진행 기록·플레이어를 반환한다.</summary>
        public async Task ReleasePlayer()
        {
            playerSpawn.Disable();
            await hudSpawn.Release();
            Quests?.Dispose();
            Quests = null;
            await playerSpawn.Release();
        }

        /// <summary>게임 전체 종료에서 유지 객체와 리스너 참조를 반환한다.</summary>
        public async Task Release()
        {
            audioListener?.Release();
            audioListener = null;
            await ReleasePlayer();
        }

        /// <summary>Play 종료에서는 구독·입력만 정리하고 객체 파괴는 Unity에 맡긴다.</summary>
        public void Quit()
        {
            quitting = true;
            Disable();
            playerSpawn?.Quit();
            hudSpawn?.Quit();
            Quests?.Dispose();
            Quests = null;
            audioListener?.Release();
            audioListener = null;
        }

        private bool IsQuitting() => quitting || !Application.isPlaying;
    }
}
