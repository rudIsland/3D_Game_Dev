using System;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Dev.CharacterTest
{
    // TestScene의 적 설정과 배치 위치를 Manager의 풀 Spawn으로 연결한다.
    public sealed class TestSceneEnemySpawner : MonoBehaviour
    {
        [Header("필수 연결")]
        [SerializeField] private WorldObjectManager worldObjectManager; // 씬 또는 시스템 참조

        [Header("적 설정과 배치 위치")]
        [SerializeField] private SpawnSettings[] enemySettings = // 행동 설정 참조
            Array.Empty<SpawnSettings>();
        [SerializeField] private Transform[] spawnPoints = // 씬 또는 시스템 참조
            Array.Empty<Transform>();

        [Header("다시 생성")]
        [SerializeField, Min(0f)] private float respawnDelay = 3f; // 시간 설정

        private WorldObjectView[] spawnedEnemies; // 씬 또는 시스템 참조
        private float[] remainingRespawnTimes; // 시간 설정

        private void Start()
        {
            if (!CanSpawn())
            {
                return;
            }

            spawnedEnemies = new WorldObjectView[enemySettings.Length];
            remainingRespawnTimes = new float[enemySettings.Length];
            SpawnMissingEnemies();
        }

        private void Update()
        {
            if (spawnedEnemies == null)
            {
                return;
            }

            for (int index = 0; index < spawnedEnemies.Length; index++)
            {
                WorldObjectView currentEnemy = spawnedEnemies[index];
                if (currentEnemy != null && currentEnemy.gameObject.activeSelf)
                {
                    remainingRespawnTimes[index] = respawnDelay;
                    continue;
                }

                if (remainingRespawnTimes[index] > 0f)
                {
                    remainingRespawnTimes[index] -= Time.deltaTime;
                    continue;
                }

                SpawnEnemy(index);
            }
        }

        private void OnDestroy()
        {
            if (worldObjectManager == null || spawnedEnemies == null)
            {
                return;
            }

            for (int index = 0; index < spawnedEnemies.Length; index++)
            {
                if (spawnedEnemies[index] != null)
                {
                    worldObjectManager.Despawn(spawnedEnemies[index]);
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Spawn Missing Enemies")]
        private void SpawnMissingEnemiesFromInspector()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "Spawn Missing Enemies는 Play 중에 사용해 주세요.",
                    this);
                return;
            }

            for (int index = 0; index < remainingRespawnTimes.Length; index++)
            {
                remainingRespawnTimes[index] = 0f;
            }

            SpawnMissingEnemies();
        }
#endif

        private void SpawnMissingEnemies()
        {
            if (spawnedEnemies == null || !CanSpawn())
            {
                return;
            }

            for (int index = 0; index < enemySettings.Length; index++)
            {
                WorldObjectView currentEnemy = spawnedEnemies[index];
                if (currentEnemy != null &&
                    currentEnemy.gameObject.activeSelf)
                {
                    continue;
                }

                SpawnEnemy(index);
            }
        }

        private void SpawnEnemy(int index)
        {
            Transform spawnPoint = spawnPoints[index];
            SpawnSettings enemySetting = enemySettings[index];

            if (spawnPoint == null || enemySetting == null)
            {
                Debug.LogError(
                    $"적 설정 또는 SpawnPoint {index} 연결이 비어 있습니다.",
                    this);
                remainingRespawnTimes[index] = respawnDelay;
                return;
            }

            if (!worldObjectManager.TrySpawn(
                    enemySetting,
                    spawnPoint.position,
                    spawnPoint.rotation,
                    out spawnedEnemies[index]))
            {
                Debug.LogError(
                    $"{enemySetting.name} 적을 Spawn하지 못했습니다.",
                    this);
                remainingRespawnTimes[index] = respawnDelay;
                return;
            }

            remainingRespawnTimes[index] = respawnDelay;
        }

        private bool CanSpawn()
        {
            if (worldObjectManager == null)
            {
                Debug.LogError(
                    "TestSceneEnemySpawner에 WorldObjectManager가 필요합니다.",
                    this);
                return false;
            }

            if (enemySettings.Length == 0 ||
                enemySettings.Length != spawnPoints.Length)
            {
                Debug.LogError(
                    "Enemy Settings와 Spawn Points 개수를 같게 연결해 주세요.",
                    this);
                return false;
            }

            return true;
        }
    }
}
