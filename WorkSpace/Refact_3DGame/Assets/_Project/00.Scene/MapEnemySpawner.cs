using System;
using System.Collections.Generic;
using UnityEngine;
using Enemy;
using Data;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Scene
{
    /// <summary>한 맵의 소환 구역을 연결하고 적 컨테이너의 갱신·반환을 소유한다.</summary>
    public sealed class MapEnemySpawner
    {
        private readonly UnityScene scene;
        private readonly EnemyDataContainer enemyData;
        private readonly List<EnemyZoneController> zones = new List<EnemyZoneController>();
        private EnemyContainer enemies;
        private Transform player;
        private bool isRunning;

        /// <summary>적과 풀이 소속될 로드된 맵 씬을 받는다.</summary>
        public MapEnemySpawner(UnityScene scene, EnemyDataContainer enemyData)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new ArgumentException("소환할 맵 씬이 로드되어 있어야 합니다.", nameof(scene));
            this.scene = scene;
            this.enemyData = enemyData;
        }

        /// <summary>이 소환 담당이 생성한 적 컨테이너다. 생성 전과 반환 후에는 null이다.</summary>
        public EnemyContainer Enemies => enemies;

        /// <summary>플레이어 준비 후 같은 씬의 활성 구역을 연결한다. 같은 대상의 중복 생성은 재사용한다.</summary>
        public EnemyContainer Create(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
                throw new ArgumentException("활성화된 플레이어가 필요합니다.", nameof(target));
            if (enemies != null)
            {
                if (player != target)
                    throw new InvalidOperationException("적을 반환한 뒤 플레이어를 변경하세요.");
                return enemies;
            }

            player = target;
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (EnemyZoneController zone in root.GetComponentsInChildren<EnemyZoneController>(true))
                        if (zone.isActiveAndEnabled) zones.Add(zone);
                }

                enemies = EnemyContainer.Create(scene);
                foreach (EnemyZoneController zone in zones)
                {
                    zone.Connect(enemies, player, enemyData.GetEnemy(zone.EnemyId));
                    if (!zone.IsReady)
                        throw new InvalidOperationException(scene.name + "/" + zone.name + " 소환 구역 준비에 실패했습니다.");
                }
                isRunning = true;
                return enemies;
            }
            catch
            {
                Release();
                throw;
            }
        }

        /// <summary>게임 갱신에서 한 번 호출해 이 맵의 활성 적을 갱신한다.</summary>
        public void Tick(float deltaTime)
        {
            if (isRunning) enemies.Tick(deltaTime);
        }

        /// <summary>플레이어 반환·맵 이탈을 시작할 때 구역 재소환과 적 갱신을 먼저 중지한다.</summary>
        public void StopSpawning()
        {
            isRunning = false;
            foreach (EnemyZoneController zone in zones)
                if (zone != null) zone.StopSpawning();
        }

        /// <summary>구역 참조를 끊고 활성 적·풀·단일 컨테이너를 반환한다. 반복 호출도 안전하다.</summary>
        public void Release()
        {
            StopSpawning();
            foreach (EnemyZoneController zone in zones)
                if (zone != null) zone.Disconnect();
            zones.Clear();
            enemies?.Dispose();
            enemies = null;
            player = null;
        }
    }
}
