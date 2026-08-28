using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace World
{
    // 같은 설정의 뷰를 재사용하는 객체 풀이다.
    internal sealed class WorldObjectPool : IDisposable
    {
        // 뷰의 등록과 활성화를 맡은 관리자다.
        private readonly WorldObjectManager manager;

        // 이 풀이 사용할 프리팹과 크기 설정이다.
        private readonly SpawnSettings settings;

        // 생성된 뷰를 담아둘 부모 Transform이다.
        private readonly Transform container;

        // 뷰를 꺼내고 되돌리는 Unity 객체 풀이다.
        private readonly ObjectPool<WorldObjectView> pool;

        // 현재 풀에서 빌려 사용 중인 뷰 목록이다.
        private readonly List<WorldObjectView> takenViews;

        // 풀이 제거되었는지 기록한다.
        private bool isDisposed;

        // 현재 사용 중인 뷰 수를 반환한다.
        public int UsedCount => pool.CountActive;

        // 현재 꺼낼 수 있는 뷰 수를 반환한다.
        public int AvailableCount => pool.CountInactive;

        // 설정값으로 풀을 만들고 시작 수만큼 미리 준비한다.
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

        // 풀에서 뷰 하나를 꺼내 위치와 회전을 지정한다.
        public WorldObjectView Take(Vector3 position, Quaternion rotation)
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

        // GameObject를 켜고 RuntimeObject를 활성화한다.
        public void Show(WorldObjectView view)
        {
            view.gameObject.SetActive(true);
            manager.Enable(view.RuntimeObject);
        }

        // RuntimeObject를 끄고 뷰 상태를 초기화한 뒤 풀에 돌려보낸다.
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

        // 꺼낼 뷰가 부족할 때 프리팹과 RuntimeObject를 새로 만든다.
        private WorldObjectView CreateView()
        {
            WorldObjectView view = Object.Instantiate(settings.Prefab, container);

            view.gameObject.SetActive(false);
            view.Prepare(manager, this);
            manager.Register(view.RuntimeObject);
            return view;
        }

        // 풀에 보관된 뷰의 GameObject를 끈다.
        private static void StoreView(WorldObjectView view)
        {
            view.gameObject.SetActive(false);
        }

        // 풀에 보관된 뷰를 등록 해제하고 파괴한다.
        private void DestroyView(WorldObjectView view)
        {
            manager.Unregister(view.RuntimeObject);
            view.IsTakenFromPool = false;
            view.IsWaitingForDespawn = false;
            DestroyGameObject(view);
        }

        // 사용 중인 뷰를 비활성화하고 등록 해제한 뒤 파괴한다.
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

        // 실행 중인지에 따라 GameObject를 안전하게 파괴한다.
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

        // 첫 생성 순간의 부담을 줄이도록 지정한 개수만큼 미리 만든다.
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
