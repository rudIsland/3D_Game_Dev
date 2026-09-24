using Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Characters.Enemies
{
    // 한 씬의 적과 적 풀만 소유한다. 갱신과 해제는 Boots가 호출한다.
    public sealed class EnemyContainer : Singleton<EnemyContainer>, IDisposable
    {
        /// <summary>소속 씬으로 단일 컨테이너를 준비한다. 같은 씬은 재사용하며, 다른 씬은 Dispose 후 지정한다.</summary>
        public static EnemyContainer Create(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new ArgumentException("로드된 적 소속 씬이 필요합니다.", nameof(scene));
            if (CurrentInstance != null && CurrentInstance.scene != scene)
                throw new InvalidOperationException("기존 EnemyContainer를 Dispose한 뒤 소속 씬을 바꾸세요.");
            return CurrentInstance ?? StoreInstance(new EnemyContainer(scene));
        }

        // Domain Reload를 꺼도 이전 Play의 적 목록과 풀에 접근하지 않는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay() { ResetInstance(); }

        private readonly Scene scene;
        private readonly List<Unit> registeredObjects = new List<Unit>(64);
        private readonly HashSet<Unit> registeredSet = new HashSet<Unit>();
        private readonly List<Unit> activeObjects = new List<Unit>(64);
        private readonly HashSet<Unit> activeSet = new HashSet<Unit>();
        private readonly Dictionary<EnemySpawnSettings, EnemyPool> pools = new Dictionary<EnemySpawnSettings, EnemyPool>();
        private readonly List<PendingAction> pendingActions = new List<PendingAction>(16);
        private bool isTicking;
        private bool isShuttingDown;
        public bool IsPaused { get; set; }
        public event Action<Unit> EnemyEnabled;
        public event Action<Unit> EnemyDisabled;
        public IReadOnlyList<Unit> ActiveObjects => activeObjects;
        public int ActiveCount => activeObjects.Count;
        public int RegisteredCount => registeredObjects.Count;
        public int PoolCount => pools.Count;
        // Create에서 전달한 씬에 적 풀을 배치한다.
        private EnemyContainer(Scene scene) { this.scene = scene; }

        public void RegisterPool(EnemySpawnSettings settings, bool warmUp = false)
        {
            if (isShuttingDown) throw new ObjectDisposedException(nameof(EnemyContainer));
            if (settings == null || settings.Prefab == null) throw new ArgumentException("적 프리팹과 설정이 필요합니다.");
            if (pools.ContainsKey(settings)) return;
            var parent = new GameObject(settings.name + " Pool").transform;
            SceneManager.MoveGameObjectToScene(parent.gameObject, scene);
            try { pools.Add(settings, new EnemyPool(this, settings, parent, warmUp)); }
            catch { UnityEngine.Object.Destroy(parent.gameObject); throw; }
        }
        /// <summary>적과 풀·구독을 정리하고 단일 인스턴스를 해제한다. 다음 사용 전에 Create를 호출한다.</summary>
        public void Dispose()
        {
            Shutdown();
            EnemyEnabled = null;
            EnemyDisabled = null;
            ClearInstance();
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
            public Unit Enemy { get; }

            // 변경 대상 Unity 뷰다.
            public EnemyView View { get; }

            // 대기 요청에 종류와 대상을 저장한다.
            public PendingAction(
                PendingActionType type,
                Unit enemy,
                EnemyView view)
            {
                Type = type;
                Enemy = enemy;
                View = view;
            }
        }

        // 객체를 등록 목록에 넣고 Create를 호출한다.
        public void Register(Unit enemy)
        {
            ThrowIfNull(enemy, nameof(enemy));
            if (isShuttingDown) return;

            if (QueueWhileTicking(
                    PendingActionType.Register,
                    enemy,
                    null))
            {
                return;
            }

            ApplyRegister(enemy);
        }

        // 등록된 객체를 활성 목록에 넣고 Enable을 호출한다.
        public void Enable(Unit enemy)
        {
            ThrowIfNull(enemy, nameof(enemy));
            if (isShuttingDown) return;

            if (QueueWhileTicking(
                    PendingActionType.Enable,
                    enemy,
                    null))
            {
                return;
            }

            ApplyEnable(enemy);
        }

        // 활성 목록에서 제거하고 Disable을 호출한다.
        public void Disable(Unit enemy)
        {
            ThrowIfNull(enemy, nameof(enemy));
            if (isShuttingDown) return;

            if (QueueWhileTicking(
                    PendingActionType.Disable,
                    enemy,
                    null))
            {
                return;
            }

            ApplyDisable(enemy);
        }

        // 객체를 비활성화하고 등록 해제한 뒤 Dispose한다.
        public void Unregister(Unit enemy)
        {
            ThrowIfNull(enemy, nameof(enemy));
            if (isShuttingDown) return;

            if (QueueWhileTicking(
                    PendingActionType.Unregister,
                    enemy,
                    null))
            {
                return;
            }

            ApplyUnregister(enemy);
        }

        // 설정에 맞는 풀에서 뷰를 꺼내 위치와 회전을 지정한다.
        public bool TrySpawn(
            EnemySpawnSettings settings,
            Vector3 position,
            Quaternion rotation,
            out EnemyView view)
        {
            view = null;

            if (isShuttingDown || settings == null ||
                !pools.TryGetValue(settings, out EnemyPool pool))
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

            return true;
        }

        // 사용이 끝난 뷰를 원래 풀로 돌려보낸다.
        public void Despawn(EnemyView view)
        {
            if (view == null ||
                view.OwnerPool == null ||
                !ReferenceEquals(view.Owner, this) ||
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
        public void Tick(float deltaTime)
        {
            if (isShuttingDown || IsPaused)
            {
                return;
            }

            isTicking = true;

            try
            {
                for (int index = 0; index < activeObjects.Count; index++)
                {
                    activeObjects[index].Tick(deltaTime);
                    if (IsPaused) break;
                }
            }
            finally
            {
                isTicking = false;
                ApplyPendingActions();
            }
        }

        // 등록 목록에 객체를 추가하고 최초 생성한다.
        private void ApplyRegister(Unit enemy)
        {
            if (!registeredSet.Add(enemy))
            {
                return;
            }

            registeredObjects.Add(enemy);
            enemy.Create();
        }

        // 활성 목록 변경과 Enable 호출을 실제로 처리한다.
        private void ApplyEnable(Unit enemy)
        {
            if (!registeredSet.Contains(enemy))
            {
                throw new InvalidOperationException("EnemyContainer.Register()를 먼저 호출해야 합니다.");
            }

            if (!activeSet.Add(enemy))
            {
                return;
            }

            activeObjects.Add(enemy);
            enemy.Enable();
            EnemyEnabled?.Invoke(enemy);
        }

        // 활성 목록에서 객체를 빼고 Disable을 호출한다.
        private void ApplyDisable(Unit enemy)
        {
            if (!activeSet.Remove(enemy))
            {
                enemy.Disable();
                return;
            }

            RemoveFromActiveList(enemy);
            enemy.Disable();
            EnemyDisabled?.Invoke(enemy);
        }

        // 객체를 비활성화하고 등록 목록과 자원을 정리한다.
        private void ApplyUnregister(Unit enemy)
        {
            if (!registeredSet.Remove(enemy))
            {
                return;
            }

            ApplyDisable(enemy);
            registeredObjects.Remove(enemy);
            enemy.Dispose();
        }

        // 풀에서 꺼낸 뷰의 GameObject와 RuntimeObject를 켠다.
        private void ApplyShowView(EnemyView view)
        {
            if (view == null ||
                !view.IsTakenFromPool ||
                view.IsWaitingForDespawn)
            {
                return;
            }

            view.gameObject.SetActive(true);
            ApplyEnable(view.RuntimeObject);
        }

        // 뷰를 원래 풀에 반환한다.
        private void ApplyReturnView(EnemyView view)
        {
            if (view == null || view.OwnerPool == null || !view.IsTakenFromPool)
            {
                return;
            }

            DisableView(view);
            view.OwnerPool.Return(view);
        }

        // RuntimeObject 정지 → Unity 상태 초기화 → GameObject 숨김 순서로 정리한다.
        private void DisableView(EnemyView view)
        {
            ApplyDisable(view.RuntimeObject);
            view.ResetForPool();
            view.gameObject.SetActive(false);
        }

        // Tick 중이면 변경 요청을 목록에 저장한다.
        private bool QueueWhileTicking(
            PendingActionType type,
            Unit enemy,
            EnemyView view)
        {
            if (!isTicking)
            {
                return false;
            }

            pendingActions.Add(new PendingAction(type, enemy, view));
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
                            ApplyRegister(action.Enemy);
                            break;
                        case PendingActionType.Enable:
                            ApplyEnable(action.Enemy);
                            break;
                        case PendingActionType.Disable:
                            ApplyDisable(action.Enemy);
                            break;
                        case PendingActionType.Unregister:
                            ApplyUnregister(action.Enemy);
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
        private void RemoveFromActiveList(Unit enemy)
        {
            int index = activeObjects.IndexOf(enemy);

            if (index < 0)
            {
                return;
            }

            int lastIndex = activeObjects.Count - 1;
            activeObjects[index] = activeObjects[lastIndex];
            activeObjects.RemoveAt(lastIndex);
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

            foreach (KeyValuePair<EnemySpawnSettings, EnemyPool> entry in pools)
            {
                IReadOnlyList<EnemyView> takenViews = entry.Value.TakenViews;
                for (int index = takenViews.Count - 1; index >= 0; index--)
                {
                    EnemyView view = takenViews[index];
                    if (view == null) continue;
                    DisableView(view);
                    entry.Value.DestroyTakenView(view);
                }

                entry.Value.Dispose();
            }

            pools.Clear();

            while (registeredObjects.Count > 0)
            {
                ApplyUnregister(registeredObjects[registeredObjects.Count - 1]);
            }

            registeredSet.Clear();
            activeSet.Clear();
            activeObjects.Clear();
        }

        // 필수 Runtime 객체가 null인지 확인한다.
        private static void ThrowIfNull(Unit enemy, string parameterName)
        {
            if (enemy == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }
    }
}
