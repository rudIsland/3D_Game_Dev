using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace rudIsland.RPG3D.World
{
    // SpawnSettings 하나에 해당하는 뷰 인스턴스를 재사용한다.
    internal sealed class WorldObjectPool : IDisposable
    {
        private readonly WorldObjectManager manager; // 씬 또는 시스템 참조
        private readonly SpawnSettings settings; // 행동 설정 참조
        private readonly Transform container; // 씬 또는 시스템 참조
        private readonly ObjectPool<WorldObjectView> pool; // 씬 또는 시스템 참조
        private readonly List<WorldObjectView> takenViews; // 씬 또는 시스템 참조
        private bool isDisposed; // 기능 사용 여부

        public int UsedCount => pool.CountActive; // 개수 또는 크기
        public int AvailableCount => pool.CountInactive; // 개수 또는 크기

        // 설정값으로 풀을 만들고 initialSize만큼 미리 준비한다.
        public WorldObjectPool(
            WorldObjectManager manager,
            SpawnSettings settings,
            Transform container)
        {
            this.manager = manager;
            this.settings = settings;
            this.container = container;
            takenViews =
                new List<WorldObjectView>(settings.MaxSize);

            pool = new ObjectPool<WorldObjectView>(
                CreateView,
                null,
                StoreView,
                DestroyView,
                true,
                Math.Max(1, settings.InitialSize),
                settings.MaxSize);

            WarmUp(settings.InitialSize);
        }

        // 보관 중인 뷰 하나를 꺼내 위치를 먼저 지정한다.
        public WorldObjectView Take(
            Vector3 position,
            Quaternion rotation)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(WorldObjectPool));
            }

            WorldObjectView view = pool.Get();
            view.transform.SetPositionAndRotation(position, rotation);
            view.IsTakenFromPool = true;
            takenViews.Add(view);
            return view;
        }

        // GameObject를 켠 다음 RuntimeObject의 사용을 시작한다.
        public void Show(WorldObjectView view)
        {
            view.gameObject.SetActive(true);
            manager.Enable(view.RuntimeObject);
        }

        // RuntimeObject를 멈추고 뷰 상태를 초기화한 뒤 풀에 보관한다.
        public void Return(WorldObjectView view)
        {
            if (!view.IsTakenFromPool)
            {
                return;
            }

            manager.Disable(view.RuntimeObject);
            view.ResetForPool();
            view.gameObject.SetActive(false);
            view.IsTakenFromPool = false;
            view.IsWaitingForDespawn = false;
            takenViews.Remove(view);
            pool.Release(view);
        }

        // 씬 종료 시 사용 중인 뷰와 보관 중인 뷰를 모두 제거한다.
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            for (int index = takenViews.Count - 1; index >= 0; index--)
            {
                DestroyTakenView(takenViews[index]);
            }

            takenViews.Clear();
            pool.Clear();
        }

        // 풀이 부족할 때 프리팹과 RuntimeObject를 새로 만들어 등록한다.
        private WorldObjectView CreateView()
        {
            WorldObjectView view = Object.Instantiate(
                settings.Prefab,
                container);

            view.gameObject.SetActive(false);
            view.Prepare(manager, this);
            manager.Register(view.RuntimeObject);
            return view;
        }

        private static void StoreView(WorldObjectView view)
        {
            view.gameObject.SetActive(false);
        }

        private void DestroyView(WorldObjectView view)
        {
            manager.Unregister(view.RuntimeObject);
            view.IsTakenFromPool = false;
            view.IsWaitingForDespawn = false;
            DestroyGameObject(view);
        }

        private void DestroyTakenView(WorldObjectView view)
        {
            manager.Disable(view.RuntimeObject);
            view.ResetForPool();
            view.gameObject.SetActive(false);
            manager.Unregister(view.RuntimeObject);
            view.IsTakenFromPool = false;
            view.IsWaitingForDespawn = false;
            DestroyGameObject(view);
        }

        private static void DestroyGameObject(WorldObjectView view)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(view.gameObject);
            }
            else
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }

        // 첫 Spawn에서 생성이 몰리지 않도록 지정한 개수만큼 미리 만든다.
        private void WarmUp(int initialSize)
        {
            if (initialSize <= 0)
            {
                return;
            }

            WorldObjectView[] warmViews =
                new WorldObjectView[initialSize];

            for (int index = 0; index < warmViews.Length; index++)
            {
                warmViews[index] = pool.Get();
            }

            for (int index = 0; index < warmViews.Length; index++)
            {
                pool.Release(warmViews[index]);
            }
        }
    }
}
