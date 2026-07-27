using System;
using System.Collections.Generic;
using UnityEngine;

namespace rudIsland.RPG3D.World
{
    // 현재 씬의 등록, 갱신, 풀 반환 순서를 한곳에서 관리한다.
    public sealed class WorldObjectManager : MonoBehaviour
    {
        [Serializable]
        private sealed class PoolUsage
        {
            [SerializeField] private SpawnSettings settings;
            [SerializeField] private int usedCount;
            [SerializeField] private int availableCount;

            public PoolUsage(SpawnSettings settings)
            {
                this.settings = settings;
            }

            public SpawnSettings Settings => settings;

            public void UpdateCount(WorldObjectPool pool)
            {
                usedCount = pool.UsedCount;
                availableCount = pool.AvailableCount;
            }
        }

        private enum PendingActionType
        {
            Register,
            Enable,
            Disable,
            Unregister,
            ShowView,
            ReturnView
        }

        private readonly struct PendingAction
        {
            public PendingActionType Type { get; }
            public IWorldObject WorldObject { get; }
            public WorldObjectView View { get; }

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
        [SerializeField] private SpawnSettings[] spawnSettings =
            Array.Empty<SpawnSettings>();

        [Header("실행 상태")]
        [SerializeField] private int activeCount;
        [SerializeField] private List<PoolUsage> poolUsage =
            new List<PoolUsage>();

        // List는 순회에 사용하고 HashSet은 중복 등록을 빠르게 막는다.
        private readonly List<IWorldObject> registeredObjects =
            new List<IWorldObject>(64);
        private readonly HashSet<IWorldObject> registeredSet =
            new HashSet<IWorldObject>();
        private readonly List<IWorldObject> activeObjects =
            new List<IWorldObject>(64);
        private readonly HashSet<IWorldObject> activeSet =
            new HashSet<IWorldObject>();
        private readonly Dictionary<SpawnSettings, WorldObjectPool> pools =
            new Dictionary<SpawnSettings, WorldObjectPool>();
        // Tick 도중 들어온 변경 요청을 순회가 끝날 때까지 잠시 보관한다.
        private readonly List<PendingAction> pendingActions =
            new List<PendingAction>(16);

        private bool isTicking;
        private bool isShuttingDown;

        public int ActiveCount => activeObjects.Count;
        public int RegisteredCount => registeredObjects.Count;
        public int PoolCount => pools.Count;

        // Inspector에 연결한 설정마다 풀을 만들고 예열한다.
        private void Awake()
        {
            for (int index = 0; index < spawnSettings.Length; index++)
            {
                AddPool(spawnSettings[index]);
            }

            RefreshInspectorCounts();
        }

        // 활성 객체만 한 번씩 갱신한다.
        private void Update()
        {
            TickActiveObjects(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        // 객체를 목록에 넣고 최초 준비 작업인 Create를 호출한다.
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

        // 활성 목록에서 제거한 뒤 Disable을 호출한다.
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

        // 비활성화한 뒤 등록 목록에서 빼고 Dispose한다.
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

        // 설정에 맞는 풀에서 뷰를 꺼내 지정한 위치에 활성화한다.
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

        // 사용이 끝난 뷰를 원래 풀로 되돌린다.
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

        // Tick 중 목록이 바뀌지 않도록 끝난 뒤 대기 요청을 처리한다.
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

        internal bool AddPoolForTests(SpawnSettings settings)
        {
            return AddPool(settings);
        }

        internal void ShutdownForTests()
        {
            Shutdown();
        }

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

        private void ApplyRegister(IWorldObject worldObject)
        {
            if (!registeredSet.Add(worldObject))
            {
                return;
            }

            registeredObjects.Add(worldObject);
            worldObject.Create();
        }

        // 실제 활성 목록 변경과 Enable 호출은 이 메서드 한곳에서 처리한다.
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
            RefreshInspectorCounts();
        }

        private void ApplyDisable(IWorldObject worldObject)
        {
            if (!activeSet.Remove(worldObject))
            {
                worldObject.Disable();
                return;
            }

            RemoveFromActiveList(worldObject);
            worldObject.Disable();
            RefreshInspectorCounts();
        }

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

        private void ApplyReturnView(WorldObjectView view)
        {
            if (view == null || view.OwnerPool == null)
            {
                return;
            }

            view.OwnerPool.Return(view);
            RefreshInspectorCounts();
        }

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

        // 씬이 끝나면 활성 객체, 풀 객체, 일반 등록 객체 순서로 정리한다.
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
