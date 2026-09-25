using Core;
using System;
using UnityEngine;
using UnityEngine.AI;
using Enemy;

namespace Scene
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
            private EnemyView spawnedView;
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
                EnemyContainer objectManager,
                EnemySpawnSettings spawnSettings,
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
                        out EnemyView view))
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
        private EnemyContainer enemyContainer;
        [SerializeField] private string enemyId;
        private EnemySpawnSettings enemySpawnSettings;
        public string EnemyId => enemyId;
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
        private bool hasStarted;

        /// <summary>필수 참조와 소환 지점이 연결되어 있는지 반환한다.</summary>
        public bool IsReady => isReady;

        /// <summary>맵 소환 담당이 준비한 컨테이너와 플레이어를 전달한다.</summary>
        public void Connect(EnemyContainer container, Transform target, EnemySpawnSettings settings)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (isReady && (enemyContainer != container || player != target))
                throw new InvalidOperationException("소환 구역 연결을 해제한 뒤 대상을 변경하세요.");
            player = target;
            enemySpawnSettings = settings;
            Connect(container);
        }

        private void Connect(EnemyContainer container)
        {
            if (isReady) return;
            enemyContainer = container;
            FindSceneReferences();
            BuildSpawnSlots();

            if (zoneCollider == null ||
                enemyContainer == null ||
                enemySpawnSettings == null ||
                player == null ||
                spawnSlots.Length == 0)
            {
                Debug.LogError(
                    $"{name}의 BoxCollider, EnemyContainer, EnemySpawnSettings, Player와 SpawnPoint 연결을 확인하세요.",
                    this);
                enabled = false;
                return;
            }

            zoneArea = new EnemyZoneArea(zoneCollider);
            isReady = true;
            enemyContainer.RegisterPool(enemySpawnSettings, player);
            StartSpawning();
        }

        public void StartSpawning()
        {
            if (!isReady || hasStarted)
            {
                return;
            }

            SpawnEmptySlots();
            wasPlayerInside = zoneArea.Contains(player.position);
            hasStarted = true;
        }

        public void StopSpawning()
        {
            hasStarted = false;
        }

        /// <summary>컨테이너 반환 전에 재소환을 중지하고 런타임 참조를 비운다.</summary>
        public void Disconnect()
        {
            StopSpawning();
            isReady = false;
            wasPlayerInside = false;
            enemyContainer = null;
            player = null;
            zoneArea = null;
            spawnSlots = Array.Empty<EnemySpawnSlot>();
        }

        private void Update()
        {
            if (!hasStarted || !isReady || player == null || enemyContainer.IsPaused) return;
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
                    enemyContainer,
                    enemySpawnSettings,
                    navMeshSampleRadius,
                    this);
            }
        }

        private void FindSceneReferences()
        {
            zoneCollider = GetComponent<BoxCollider>();

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
