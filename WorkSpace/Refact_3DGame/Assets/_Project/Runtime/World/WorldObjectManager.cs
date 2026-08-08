using System;
using System.Collections.Generic;
using UnityEngine;

namespace rudIsland.RPG3D.World
{
    // 현재 씬의 월드 객체 등록, 갱신, 활성화와 풀 반환을 관리한다.
    public sealed class WorldObjectManager : MonoBehaviour
    {
        [Serializable]
        // Inspector에 특정 풀의 현재 사용량을 보여준다.
        private sealed class PoolUsage
        {
            // 사용량을 표시할 풀 설정이다.
            [SerializeField] private SpawnSettings settings;

            // 현재 사용 중인 뷰 수다.
            [SerializeField] private int usedCount;

            // 현재 꺼낼 수 있는 뷰 수다.
            [SerializeField] private int availableCount;

            // 풀 설정을 사용량 항목에 연결한다.
            public PoolUsage(SpawnSettings settings)
            {
                this.settings = settings;
            }

            // 사용량을 계산할 풀 설정을 반환한다.
            public SpawnSettings Settings => settings;

            // 풀의 현재 사용량을 Inspector 표시값에 반영한다.
            public void UpdateCount(WorldObjectPool pool)
            {
                usedCount = pool.UsedCount;
                availableCount = pool.AvailableCount;
            }
        }

        // Tick 중 발생한 변경 요청의 종류다.
        private enum PendingActionType
        {
            Register,
            Enable,
            Disable,
            Unregister,
            ShowView,
            ReturnView
        }

        // Tick이 끝난 뒤 적용할 변경 요청 하나를 담는다.
        private readonly struct PendingAction
        {
            // 처리할 변경 종류다.
            public PendingActionType Type { get; }

            // 변경 대상 Runtime 객체다.
            public IWorldObject WorldObject { get; }

            // 변경 대상 Unity 뷰다.
            public WorldObjectView View { get; }

            // 대기 요청에 종류와 대상을 저장한다.
            public PendingAction(
                PendingActionType type,
                IWorldObject worldObject,
                WorldObjectView view)
            {
                Type = type;
                WorldObject = worldObject;
                View = view;
            }
        }

        [Header("풀 설정")]
        // Inspector에서 연결한 풀 생성 설정 목록이다.
        [SerializeField] private SpawnSettings[] spawnSettings =
            Array.Empty<SpawnSettings>();

        [Header("실행 상태")]
        // 현재 활성화된 객체 수를 Inspector에 표시한다.
        [SerializeField] private int activeCount;

        // 각 풀의 사용량을 Inspector에 표시한다.
        [SerializeField] private List<PoolUsage> poolUsage = new List<PoolUsage>();

        // List는 순회에 사용하고 HashSet은 중복 등록을 빠르게 막는다.

        // Manager에 등록되어 있는 모든 Runtime 객체 목록이다.
        private readonly List<IWorldObject> registeredObjects =
            new List<IWorldObject>(64);

        // 중복 등록을 빠르게 확인하는 집합이다.
        private readonly HashSet<IWorldObject> registeredSet =
            new HashSet<IWorldObject>();

        // Tick을 실행할 활성 Runtime 객체 목록이다.
        private readonly List<IWorldObject> activeObjects =
            new List<IWorldObject>(64);

        // 중복 활성화를 빠르게 확인하는 집합이다.
        private readonly HashSet<IWorldObject> activeSet =
            new HashSet<IWorldObject>();

        // SpawnSettings별로 만든 객체 풀 목록이다.
        private readonly Dictionary<SpawnSettings, WorldObjectPool> pools =
            new Dictionary<SpawnSettings, WorldObjectPool>();

        // Tick 중 들어온 변경 요청을 잠시 보관하는 목록이다.
        private readonly List<PendingAction> pendingActions =
            new List<PendingAction>(16);

        // 현재 활성 객체를 순회 중인지 기록한다.
        private bool isTicking;

        // Manager가 종료 작업 중인지 기록한다.
        private bool isShuttingDown;

        // Runtime 객체가 활성화될 때 알린다.
        public event Action<IWorldObject> WorldObjectEnabled;

        // Runtime 객체가 비활성화될 때 알린다.
        public event Action<IWorldObject> WorldObjectDisabled;

        // 현재 활성화된 객체 목록을 읽기 전용으로 제공한다.
        public IReadOnlyList<IWorldObject> ActiveObjects => activeObjects;

        // 현재 활성화된 객체 수를 반환한다.
        public int ActiveCount => activeObjects.Count;

        // Manager에 등록된 객체 수를 반환한다.
        public int RegisteredCount => registeredObjects.Count;

        // 현재 만들어진 풀 수를 반환한다.
        public int PoolCount => pools.Count;
        // Inspector 설정마다 객체 풀을 만들고 예열한다.
        private void Awake()
        {
            for (int index = 0; index < spawnSettings.Length; index++)
            {
                AddPool(spawnSettings[index]);
            }

            RefreshInspectorCounts();
        }

        // 매 프레임 활성 객체만 한 번씩 갱신한다.
        private void Update()
        {
            TickActiveObjects(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        // 객체를 등록 목록에 넣고 Create를 호출한다.
        public void Register(IWorldObject worldObject)
        {
            ThrowIfNull(worldObject, nameof(worldObject));

            if (QueueWhileTicking(
                    PendingActionType.Register,
                    worldObject,
                    null))
            {
                return;
            }

            ApplyRegister(worldObject);
        }

        // 등록된 객체를 활성 목록에 넣고 Enable을 호출한다.
        public void Enable(IWorldObject worldObject)
        {
            ThrowIfNull(worldObject, nameof(worldObject));

            if (QueueWhileTicking(
                    PendingActionType.Enable,
                    worldObject,
                    null))
            {
                return;
            }

            ApplyEnable(worldObject);
        }

        // 활성 목록에서 제거하고 Disable을 호출한다.
        public void Disable(IWorldObject worldObject)
        {
            ThrowIfNull(worldObject, nameof(worldObject));

            if (QueueWhileTicking(
                    PendingActionType.Disable,
                    worldObject,
                    null))
            {
                return;
            }

            ApplyDisable(worldObject);
        }

        // 객체를 비활성화하고 등록 해제한 뒤 Dispose한다.
        public void Unregister(IWorldObject worldObject)
        {
            ThrowIfNull(worldObject, nameof(worldObject));

            if (QueueWhileTicking(
                    PendingActionType.Unregister,
                    worldObject,
                    null))
            {
                return;
            }

            ApplyUnregister(worldObject);
        }

        // 설정에 맞는 풀에서 뷰를 꺼내 위치와 회전을 지정한다.
        public bool TrySpawn(
            SpawnSettings settings,
            Vector3 position,
            Quaternion rotation,
            out WorldObjectView view)
        {
            view = null;

            if (settings == null ||
                !pools.TryGetValue(settings, out WorldObjectPool pool))
            {
                return false;
            }

            view = pool.Take(position, rotation);

            if (!QueueWhileTicking(
                    PendingActionType.ShowView,
                    null,
                    view))
            {
                ApplyShowView(view);
            }

            RefreshInspectorCounts();
            return true;
        }

        // 사용이 끝난 뷰를 원래 풀로 돌려보낸다.
        public void Despawn(WorldObjectView view)
        {
            if (view == null ||
                view.OwnerPool == null ||
                !ReferenceEquals(view.OwnerPool, GetOwnedPool(view)) ||
                !view.IsTakenFromPool ||
                view.IsWaitingForDespawn)
            {
                return;
            }

            view.IsWaitingForDespawn = true;

            if (!QueueWhileTicking(
                    PendingActionType.ReturnView,
                    null,
                    view))
            {
                ApplyReturnView(view);
            }
        }

        // Tick 중 목록이 바뀌지 않도록 순회가 끝난 뒤 대기 요청을 처리한다.
        internal void TickActiveObjects(float deltaTime)
        {
            if (isShuttingDown)
            {
                return;
            }

            isTicking = true;

            try
            {
                for (int index = 0; index < activeObjects.Count; index++)
                {
                    activeObjects[index].Tick(deltaTime);
                }
            }
            finally
            {
                isTicking = false;
                ApplyPendingActions();
            }
        }

        // 테스트에서 Manager 종료 처리를 직접 실행한다.
        internal void ShutdownForTests()
        {
            Shutdown();
        }

        // 하나의 SpawnSettings에 대한 풀을 만들고 등록한다.
        private bool AddPool(SpawnSettings settings)
        {
            if (settings == null ||
                settings.Prefab == null ||
                pools.ContainsKey(settings))
            {
                return false;
            }

            var poolContainer = new GameObject(
                $"{settings.name} Pool").transform;
            poolContainer.SetParent(transform, false);

            var pool = new WorldObjectPool(
                this,
                settings,
                poolContainer);

            pools.Add(settings, pool);
            poolUsage.Add(new PoolUsage(settings));
            RefreshInspectorCounts();
            return true;
        }

        // 등록 목록에 객체를 추가하고 최초 생성한다.
        private void ApplyRegister(IWorldObject worldObject)
        {
            if (!registeredSet.Add(worldObject))
            {
                return;
            }

            registeredObjects.Add(worldObject);
            worldObject.Create();
        }

        // 활성 목록 변경과 Enable 호출을 실제로 처리한다.
        private void ApplyEnable(IWorldObject worldObject)
        {
            if (!registeredSet.Contains(worldObject))
            {
                throw new InvalidOperationException(
                    "WorldObjectManager.Register()를 먼저 호출해야 합니다.");
            }

            if (!activeSet.Add(worldObject))
            {
                return;
            }

            activeObjects.Add(worldObject);
            worldObject.Enable();
            WorldObjectEnabled?.Invoke(worldObject);
            RefreshInspectorCounts();
        }

        // 활성 목록에서 객체를 빼고 Disable을 호출한다.
        private void ApplyDisable(IWorldObject worldObject)
        {
            if (!activeSet.Remove(worldObject))
            {
                worldObject.Disable();
                return;
            }

            RemoveFromActiveList(worldObject);
            worldObject.Disable();
            WorldObjectDisabled?.Invoke(worldObject);
            RefreshInspectorCounts();
        }

        // 객체를 비활성화하고 등록 목록과 자원을 정리한다.
        private void ApplyUnregister(IWorldObject worldObject)
        {
            if (!registeredSet.Remove(worldObject))
            {
                return;
            }

            ApplyDisable(worldObject);
            registeredObjects.Remove(worldObject);
            worldObject.Dispose();
        }

        // 풀에서 꺼낸 뷰의 GameObject와 RuntimeObject를 켠다.
        private void ApplyShowView(WorldObjectView view)
        {
            if (view == null ||
                !view.IsTakenFromPool ||
                view.IsWaitingForDespawn)
            {
                return;
            }

            view.OwnerPool.Show(view);
            RefreshInspectorCounts();
        }

        // 뷰를 원래 풀에 반환한다.
        private void ApplyReturnView(WorldObjectView view)
        {
            if (view == null || view.OwnerPool == null)
            {
                return;
            }

            view.OwnerPool.Return(view);
            RefreshInspectorCounts();
        }

        // Tick 중이면 변경 요청을 목록에 저장한다.
        private bool QueueWhileTicking(
            PendingActionType type,
            IWorldObject worldObject,
            WorldObjectView view)
        {
            if (!isTicking)
            {
                return false;
            }

            pendingActions.Add(
                new PendingAction(type, worldObject, view));
            return true;
        }

        // Tick 순회가 끝난 뒤 요청을 들어온 순서대로 반영한다.
        private void ApplyPendingActions()
        {
            try
            {
                for (int index = 0; index < pendingActions.Count; index++)
                {
                    PendingAction action = pendingActions[index];

                    switch (action.Type)
                    {
                        case PendingActionType.Register:
                            ApplyRegister(action.WorldObject);
                            break;
                        case PendingActionType.Enable:
                            ApplyEnable(action.WorldObject);
                            break;
                        case PendingActionType.Disable:
                            ApplyDisable(action.WorldObject);
                            break;
                        case PendingActionType.Unregister:
                            ApplyUnregister(action.WorldObject);
                            break;
                        case PendingActionType.ShowView:
                            ApplyShowView(action.View);
                            break;
                        case PendingActionType.ReturnView:
                            ApplyReturnView(action.View);
                            break;
                    }
                }
            }
            finally
            {
                pendingActions.Clear();
            }
        }

        // 활성 목록에서 객체를 빠르게 제거한다.
        private void RemoveFromActiveList(IWorldObject worldObject)
        {
            int index = activeObjects.IndexOf(worldObject);

            if (index < 0)
            {
                return;
            }

            int lastIndex = activeObjects.Count - 1;
            activeObjects[index] = activeObjects[lastIndex];
            activeObjects.RemoveAt(lastIndex);
        }

        // 뷰가 이 Manager가 소유한 풀에 속하는지 확인한다.
        private WorldObjectPool GetOwnedPool(WorldObjectView view)
        {
            foreach (KeyValuePair<SpawnSettings, WorldObjectPool> entry in pools)
            {
                if (ReferenceEquals(entry.Value, view.OwnerPool))
                {
                    return entry.Value;
                }
            }

            return null;
        }

        // 활성 객체 수와 각 풀의 사용량을 Inspector 값에 반영한다.
        private void RefreshInspectorCounts()
        {
            activeCount = activeObjects.Count;

            for (int index = 0; index < poolUsage.Count; index++)
            {
                PoolUsage status = poolUsage[index];

                if (pools.TryGetValue(
                        status.Settings,
                        out WorldObjectPool pool))
                {
                    status.UpdateCount(pool);
                }
            }
        }

        // 씬이 끝날 때 활성 객체, 풀, 등록 객체 순서로 정리한다.
        private void Shutdown()
        {
            if (isShuttingDown)
            {
                return;
            }

            isShuttingDown = true;
            isTicking = false;
            pendingActions.Clear();

            while (activeObjects.Count > 0)
            {
                ApplyDisable(activeObjects[activeObjects.Count - 1]);
            }

            foreach (KeyValuePair<SpawnSettings, WorldObjectPool> entry in pools)
            {
                entry.Value.Dispose();
            }

            pools.Clear();
            poolUsage.Clear();

            while (registeredObjects.Count > 0)
            {
                ApplyUnregister(
                    registeredObjects[registeredObjects.Count - 1]);
            }

            registeredSet.Clear();
            activeSet.Clear();
            activeObjects.Clear();
            activeCount = 0;
        }

        // 필수 Runtime 객체가 null인지 확인한다.
        private static void ThrowIfNull(
            IWorldObject worldObject,
            string parameterName)
        {
            if (worldObject == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }
    }
}
