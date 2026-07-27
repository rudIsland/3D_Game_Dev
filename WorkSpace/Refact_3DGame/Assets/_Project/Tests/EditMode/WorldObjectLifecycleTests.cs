using System;
using NUnit.Framework;
using rudIsland.RPG3D.World;

namespace rudIsland.RPG3D.Tests
{
    public sealed class WorldObjectLifecycleTests
    {
        [Test]
        public void Lifecycle_DuplicateCalls_RunEachCallbackOnce()
        {
            var worldObject = new FakeWorldObject();

            worldObject.Create();
            worldObject.Create();
            worldObject.Enable();
            worldObject.Enable();
            worldObject.Tick(0.25f);
            worldObject.Disable();
            worldObject.Disable();
            worldObject.Dispose();
            worldObject.Dispose();

            Assert.That(worldObject.CreateCount, Is.EqualTo(1));
            Assert.That(worldObject.EnableCount, Is.EqualTo(1));
            Assert.That(worldObject.TickCount, Is.EqualTo(1));
            Assert.That(worldObject.DisableCount, Is.EqualTo(1));
            Assert.That(worldObject.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Enable_BeforeCreate_ThrowsClearException()
        {
            var worldObject = new FakeWorldObject();

            Assert.Throws<InvalidOperationException>(
                worldObject.Enable);
        }

        [Test]
        public void Tick_WhileDisabled_DoesNotRun()
        {
            var worldObject = new FakeWorldObject();
            worldObject.Create();

            worldObject.Tick(0.25f);

            Assert.That(worldObject.TickCount, Is.Zero);
        }

        [Test]
        public void Enable_AfterDispose_IsRejected()
        {
            var worldObject = new FakeWorldObject();
            worldObject.Create();
            worldObject.Dispose();

            Assert.Throws<ObjectDisposedException>(
                worldObject.Enable);
        }

        private sealed class FakeWorldObject : WorldObject
        {
            public int CreateCount { get; private set; }
            public int EnableCount { get; private set; }
            public int TickCount { get; private set; }
            public int DisableCount { get; private set; }
            public int DisposeCount { get; private set; }

            protected override void OnCreate()
            {
                CreateCount++;
            }

            protected override void OnEnable()
            {
                EnableCount++;
            }

            protected override void OnTick(float deltaTime)
            {
                TickCount++;
            }

            protected override void OnDisable()
            {
                DisableCount++;
            }

            protected override void OnDispose()
            {
                DisposeCount++;
            }
        }
    }
}
