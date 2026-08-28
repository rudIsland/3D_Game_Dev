using System;
using Characters.Player.Lifecycle;
using UnityEngine;
using UnityEngine.AI;

namespace World.Zones
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    // Zone의 소환 지점과 플레이어 재진입에 따른 적 재소환을 관리한다.
    public sealed class EnemyZoneController : MonoBehaviour
    {
        [Serializable]
        private sealed class EnemySpawnSlot
        {
            private readonly Transform spawnPoint;
            private WorldObjectView spawnedView;
            private IZoneEnemy spawnedEnemy;
            private bool hasLoggedNavMeshError;
            private bool hasLoggedEnemyTypeError;

            internal EnemySpawnSlot(Transform spawnPoint)
            {
                this.spawnPoint = spawnPoint;
            }

            internal bool IsEmpty => spawnedView == null;

            internal void RefreshOwner(EnemyZoneArea zoneArea)
            {
                if (spawnedView == null)
                {
                    return;
                }

                if (!spawnedView.gameObject.activeInHierarchy ||
                    spawnedEnemy == null ||
                    !ReferenceEquals(spawnedEnemy.HomeZone, zoneArea))
                {
                    spawnedView = null;
                    spawnedEnemy = null;
                }
            }

            internal void TrySpawn(
                EnemyZoneArea zoneArea,
                WorldObjectManager objectManager,
                SpawnSettings spawnSettings,
                float navMeshSampleRadius,
                UnityEngine.Object logContext)
            {
                if (!IsEmpty)
                {
                    return;
                }

                if (!NavMesh.SamplePosition(
                        spawnPoint.position,
                        out NavMeshHit navMeshHit,
                        navMeshSampleRadius,
                        NavMesh.AllAreas))
                {
                    if (!hasLoggedNavMeshError)
                    {
                        Debug.LogError(
                            $"{spawnPoint.name} 주변 {navMeshSampleRadius:0.##}m 안에서 NavMesh를 찾지 못했습니다.",
                            logContext);
                        hasLoggedNavMeshError = true;
                    }

                    return;
                }

                if (!objectManager.TrySpawn(
                        spawnSettings,
                        navMeshHit.position,
                        spawnPoint.rotation,
                        out WorldObjectView view))
                {
                    return;
                }

                if (!(view is IZoneEnemy zoneEnemy))
                {
                    if (!hasLoggedEnemyTypeError)
                    {
                        Debug.LogError(
                            $"{spawnSettings.name} 프리팹은 IZoneEnemy를 구현해야 합니다.",
                            logContext);
                        hasLoggedEnemyTypeError = true;
                    }

                    view.RequestDespawn();
                    return;
                }

                zoneEnemy.SetHomeZone(zoneArea, navMeshHit.position);
                spawnedView = view;
                spawnedEnemy = zoneEnemy;
            }
        }

        [Header("필수 연결")]
        [SerializeField] private WorldObjectManager worldObjectManager;
        [SerializeField] private SpawnSettings enemySpawnSettings;
        [SerializeField] private Transform player;
        [SerializeField] private Transform enemySpawnPoints;

        [Header("NavMesh 보정")]
        [SerializeField, Min(0.1f)]
        private float navMeshSampleRadius = 3f;

        private BoxCollider zoneCollider;
        private EnemyZoneArea zoneArea;
        private EnemySpawnSlot[] spawnSlots =
            Array.Empty<EnemySpawnSlot>();
        private bool wasPlayerInside;
        private bool isReady;

        private void Awake()
        {
            FindSceneReferences();
            BuildSpawnSlots();

            if (zoneCollider == null ||
                worldObjectManager == null ||
                enemySpawnSettings == null ||
                player == null ||
                spawnSlots.Length == 0)
            {
                Debug.LogError(
                    $"{name}의 BoxCollider, WorldObjectManager, SpawnSettings, Player와 SpawnPoint 연결을 확인하세요.",
                    this);
                enabled = false;
                return;
            }

            zoneArea = new EnemyZoneArea(zoneCollider);
            isReady = true;
        }

        private void Start()
        {
            if (!isReady)
            {
                return;
            }

            SpawnEmptySlots();
            wasPlayerInside = zoneArea.Contains(player.position);
        }

        private void Update()
        {
            for (int index = 0; index < spawnSlots.Length; index++)
            {
                spawnSlots[index].RefreshOwner(zoneArea);
            }

            bool isPlayerInside = zoneArea.Contains(player.position);
            if (!wasPlayerInside && isPlayerInside)
            {
                SpawnEmptySlots();
            }

            wasPlayerInside = isPlayerInside;
        }

        private void SpawnEmptySlots()
        {
            for (int index = 0; index < spawnSlots.Length; index++)
            {
                spawnSlots[index].TrySpawn(
                    zoneArea,
                    worldObjectManager,
                    enemySpawnSettings,
                    navMeshSampleRadius,
                    this);
            }
        }

        private void FindSceneReferences()
        {
            zoneCollider = GetComponent<BoxCollider>();

            if (worldObjectManager == null)
            {
                worldObjectManager =
                    FindFirstObjectByType<WorldObjectManager>();
            }

            if (player == null)
            {
                PlayerController playerController =
                    FindFirstObjectByType<PlayerController>();
                player = playerController != null
                    ? playerController.transform
                    : null;
            }

            if (enemySpawnPoints == null)
            {
                enemySpawnPoints = transform.Find("EnemySpawnPoints");
            }
        }

        private void BuildSpawnSlots()
        {
            if (enemySpawnPoints == null)
            {
                spawnSlots = Array.Empty<EnemySpawnSlot>();
                return;
            }

            int spawnPointCount = enemySpawnPoints.childCount;
            spawnSlots = new EnemySpawnSlot[spawnPointCount];

            for (int index = 0; index < spawnPointCount; index++)
            {
                spawnSlots[index] = new EnemySpawnSlot(
                    enemySpawnPoints.GetChild(index));
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            navMeshSampleRadius = Mathf.Max(
                0.1f,
                navMeshSampleRadius);
            zoneCollider = GetComponent<BoxCollider>();
        }
#endif
    }
}
