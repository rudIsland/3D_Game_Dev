using Characters.Enemies;
using Characters;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Characters.Enemies
{
    // 같은 설정의 뷰를 재사용하는 객체 풀이다.
    internal sealed class EnemyPool : IDisposable
    {
        // 뷰 생성과 파괴 시 RuntimeObject를 등록하고 해제하는 관리자다.
        private readonly EnemyContainer manager;

        // 이 풀이 사용할 프리팹과 크기 설정이다.
        private readonly EnemySpawnSettings settings;

        // 생성된 뷰를 담아둘 부모 Transform이다.
        private readonly Transform container;

        // 뷰를 꺼내고 되돌리는 Unity 객체 풀이다.
        private readonly ObjectPool<EnemyView> pool;

        // 현재 풀에서 빌려 사용 중인 뷰 목록이다.
        private readonly List<EnemyView> takenViews;

        // 풀이 제거되었는지 기록한다.
        private bool isDisposed;

        // 현재 사용 중인 뷰 수를 반환한다.
        public int UsedCount => pool.CountActive;

        // 현재 꺼낼 수 있는 뷰 수를 반환한다.
        public int AvailableCount => pool.CountInactive;

        // 관리자가 종료 순서에 맞춰 사용 중인 뷰를 정리할 때 읽는다.
        internal IReadOnlyList<EnemyView> TakenViews => takenViews;

        // 설정값으로 풀을 만들고 시작 수만큼 미리 준비한다.
        public EnemyPool(
            EnemyContainer manager,
            EnemySpawnSettings settings,
            Transform container,
            bool warmUp = true)
        {
            this.manager = manager;
            this.settings = settings;
            this.container = container;
            takenViews =
                new List<EnemyView>(settings.MaxSize);

            pool = new ObjectPool<EnemyView>(
                CreateView,
                null,
                StoreView,
                DestroyView,
                true,
                Math.Max(1, settings.InitialSize),
                settings.MaxSize);

            if (warmUp) WarmUp(settings.InitialSize);
        }

        // 풀에서 뷰 하나를 꺼내 위치와 회전을 지정한다.
        public EnemyView Take(Vector3 position, Quaternion rotation)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(EnemyPool));
            }

            EnemyView view = pool.Get();
            view.transform.SetPositionAndRotation(position, rotation);
            view.IsTakenFromPool = true;
            takenViews.Add(view);
            return view;
        }

        // 관리자가 비활성화와 초기화를 마친 뷰를 풀에 보관한다.
        public void Return(EnemyView view)
        {
            if (!view.IsTakenFromPool)
            {
                return;
            }

            view.IsTakenFromPool = false;
            view.IsWaitingForDespawn = false;
            takenViews.Remove(view);
            pool.Release(view);
        }

        // 관리자가 정리한 사용 중인 뷰 하나를 등록 해제하고 제거한다.
        internal void DestroyTakenView(EnemyView view)
        {
            takenViews.Remove(view);
            DestroyView(view);
        }

        // 관리자가 사용 중인 뷰를 정리한 뒤 모든 뷰를 등록 해제하고 제거한다.
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            for (int index = takenViews.Count - 1; index >= 0; index--)
            {
                DestroyView(takenViews[index]);
            }

            takenViews.Clear();
            pool.Clear();
            if (container != null)
            {
                if (Application.isPlaying) Object.Destroy(container.gameObject);
                else Object.DestroyImmediate(container.gameObject);
            }
        }

        // 꺼낼 뷰가 부족할 때 프리팹과 RuntimeObject를 새로 만든다.
        private EnemyView CreateView()
        {
            EnemyView view = Object.Instantiate(settings.Prefab, container);

            view.gameObject.SetActive(false);
            view.Prepare(manager, this);
            manager.Register(view.RuntimeObject);
            return view;
        }

        // 풀에 보관된 뷰의 GameObject를 끈다.
        private static void StoreView(EnemyView view)
        {
            view.gameObject.SetActive(false);
        }

        // 풀에 보관된 뷰를 등록 해제하고 파괴한다.
        private void DestroyView(EnemyView view)
        {
            if (view == null) return;
            manager.Unregister(view.RuntimeObject);
            view.IsTakenFromPool = false;
            view.IsWaitingForDespawn = false;
            DestroyGameObject(view);
        }

        // 실행 중인지에 따라 GameObject를 안전하게 파괴한다.
        private static void DestroyGameObject(EnemyView view)
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

            EnemyView[] warmViews =
                new EnemyView[initialSize];

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
