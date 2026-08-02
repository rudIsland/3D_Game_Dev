using System;
using NUnit.Framework;
using rudIsland.RPG3D.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace rudIsland.RPG3D.Tests
{
    public sealed class WorldObjectManagerTests
    {
        private GameObject managerObject; // 씬 또는 시스템 참조
        private WorldObjectManager manager; // 씬 또는 시스템 참조
        private GameObject prefabObject; // 씬 또는 시스템 참조
        private SpawnSettings settings; // 행동 설정 참조

        [SetUp]
        public void SetUp()
        {
            managerObject = new GameObject("WorldObjectManager Test");
            manager = managerObject.AddComponent<WorldObjectManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (managerObject != null)
            {
                Object.DestroyImmediate(managerObject);
            }

            if (prefabObject != null)
            {
                Object.DestroyImmediate(prefabObject);
            }

            if (settings != null)
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void RegisterTwice_TicksObjectOncePerManagerTick()
        {
            var worldObject = new FakeWorldObject();

            manager.Register(worldObject);
            manager.Register(worldObject);
            manager.Enable(worldObject);
            manager.Enable(worldObject);

            manager.TickActiveObjects(0.1f);

            Assert.That(worldObject.TickCount, Is.EqualTo(1));
            Assert.That(manager.ActiveCount, Is.EqualTo(1));
            Assert.That(manager.RegisteredCount, Is.EqualTo(1));
        }

        [Test]
        public void DespawnDuringTick_IsAppliedAfterActiveLoop()
        {
            CreatePool(initialSize: 1, maxSize: 2);
            Assert.That(
                manager.TrySpawn(
                    settings,
                    Vector3.zero,
                    Quaternion.identity,
                    out WorldObjectView view),
                Is.True);

            var fakeView = (FakeWorldObjectView)view;
            fakeView.FakeRuntime.TickAction =
                () => manager.Despawn(fakeView);

            manager.TickActiveObjects(0.1f);

            Assert.That(fakeView.FakeRuntime.TickCount, Is.EqualTo(1));
            Assert.That(manager.ActiveCount, Is.Zero);
            Assert.That(fakeView.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SpawnAfterDespawn_ReusesSameViewInstance()
        {
            CreatePool(initialSize: 1, maxSize: 2);
            manager.TrySpawn(
                settings,
                Vector3.zero,
                Quaternion.identity,
                out WorldObjectView firstView);

            manager.Despawn(firstView);

            manager.TrySpawn(
                settings,
                Vector3.one,
                Quaternion.identity,
                out WorldObjectView secondView);

            Assert.That(secondView, Is.SameAs(firstView));
            Assert.That(secondView.transform.position, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void Shutdown_ClearsActiveRegisteredAndPoolCounts()
        {
            CreatePool(initialSize: 2, maxSize: 3);
            manager.TrySpawn(
                settings,
                Vector3.zero,
                Quaternion.identity,
                out _);

            manager.ShutdownForTests();

            Assert.That(manager.ActiveCount, Is.Zero);
            Assert.That(manager.RegisteredCount, Is.Zero);
            Assert.That(manager.PoolCount, Is.Zero);
        }

        [Test]
        public void TickAfterWarmUp_DoesNotAllocateManagedMemory()
        {
            var worldObject = new FakeWorldObject();
            manager.Register(worldObject);
            manager.Enable(worldObject);
            manager.TickActiveObjects(0.1f);

            long beforeBytes =
                GC.GetAllocatedBytesForCurrentThread();

            for (int index = 0; index < 100; index++)
            {
                manager.TickActiveObjects(0.1f);
            }

            long afterBytes =
                GC.GetAllocatedBytesForCurrentThread();

            Assert.That(afterBytes - beforeBytes, Is.Zero);
        }

        [Test]
        public void EnableAndDisableTwice_RaiseLifecycleEventsOnce()
        {
            var worldObject = new FakeWorldObject();
            int enabledCount = 0;
            int disabledCount = 0;
            manager.WorldObjectEnabled += enabled =>
            {
                if (ReferenceEquals(enabled, worldObject))
                {
                    enabledCount++;
                }
            };
            manager.WorldObjectDisabled += disabled =>
            {
                if (ReferenceEquals(disabled, worldObject))
                {
                    disabledCount++;
                }
            };

            manager.Register(worldObject);
            manager.Enable(worldObject);
            manager.Enable(worldObject);
            manager.Disable(worldObject);
            manager.Disable(worldObject);

            Assert.That(enabledCount, Is.EqualTo(1));
            Assert.That(disabledCount, Is.EqualTo(1));
            Assert.That(manager.ActiveObjects, Is.Empty);
        }

        [Test]
        public void SpawnAfterDespawn_RaisesEnabledAgainForReusedObject()
        {
            CreatePool(initialSize: 1, maxSize: 1);
            int enabledCount = 0;
            manager.WorldObjectEnabled += _ => enabledCount++;

            manager.TrySpawn(
                settings,
                Vector3.zero,
                Quaternion.identity,
                out WorldObjectView firstView);
            manager.Despawn(firstView);
            manager.TrySpawn(
                settings,
                Vector3.one,
                Quaternion.identity,
                out WorldObjectView secondView);

            Assert.That(secondView, Is.SameAs(firstView));
            Assert.That(enabledCount, Is.EqualTo(2));
            Assert.That(manager.ActiveObjects, Has.Count.EqualTo(1));
        }

        private void CreatePool(int initialSize, int maxSize)
        {
            prefabObject = new GameObject("Fake World Object Prefab");
            var prefab = prefabObject.AddComponent<FakeWorldObjectView>();

            settings = ScriptableObject.CreateInstance<SpawnSettings>();
            settings.name = "Fake Spawn Settings";
            settings.SetValuesForTests(prefab, initialSize, maxSize);

            Assert.That(manager.AddPoolForTests(settings), Is.True);
        }

        private sealed class FakeWorldObject : WorldObject
        {
            public Action TickAction { get; set; } // 현재 행동 상태
            public int TickCount { get; private set; } // 개수 또는 크기

            protected override void OnTick(float deltaTime)
            {
                TickCount++;
                TickAction?.Invoke();
            }
        }

        private sealed class FakeWorldObjectView : WorldObjectView
        {
            public FakeWorldObject FakeRuntime { get; private set; } // 시간 설정

            protected override IWorldObject CreateRuntimeObject()
            {
                FakeRuntime = new FakeWorldObject();
                return FakeRuntime;
            }
        }
    }
}
