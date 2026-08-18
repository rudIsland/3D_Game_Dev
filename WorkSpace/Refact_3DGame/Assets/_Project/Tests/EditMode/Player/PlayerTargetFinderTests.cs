using System.Collections.Generic;
using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Player.States.Target;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class PlayerTargetFinderTests
    {
        private const int TargetLayer = 7;
        private readonly List<GameObject> createdObjects =
            new List<GameObject>();
        private Vector3 testOrigin;

        [SetUp]
        public void SetUp()
        {
            testOrigin = new Vector3(4000f, 100f, 4000f);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
            {
                Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void TryFindTarget_가까운옆대상보다화면중앙대상을고른다()
        {
            Transform player = CreateObject("Player", testOrigin).transform;
            Transform view = CreateObject(
                "View",
                testOrigin + Vector3.up * 1.2f).transform;
            Transform centeredTarget = CreateTarget(
                "CenteredTarget",
                testOrigin + Vector3.forward * 8f);
            CreateTarget(
                "NearSideTarget",
                testOrigin + new Vector3(3f, 0f, 4f));
            Physics.SyncTransforms();

            PlayerTargetFinder finder = CreateFinder(player, view);

            Assert.That(finder.TryFindTarget(out Transform selected), Is.True);
            Assert.That(selected, Is.SameAs(centeredTarget));
        }

        [Test]
        public void IsTargetAliveAndInRange_사망과거리초과를즉시거부한다()
        {
            Transform player = CreateObject("Player", testOrigin).transform;
            Transform view = CreateObject(
                "View",
                testOrigin + Vector3.up * 1.2f).transform;
            Transform target = CreateTarget(
                "Target",
                testOrigin + Vector3.forward * 5f);
            Physics.SyncTransforms();
            PlayerTargetFinder finder = CreateFinder(player, view);
            Assert.That(finder.TryFindTarget(out _), Is.True);

            target.GetComponent<PlayerTargetTestDeathState>().IsDead = true;
            Assert.That(
                finder.IsTargetAliveAndInRange(target, 15f),
                Is.False);

            target.GetComponent<PlayerTargetTestDeathState>().IsDead = false;
            target.position = testOrigin + Vector3.forward * 16f;
            Physics.SyncTransforms();
            Assert.That(
                finder.IsTargetAliveAndInRange(target, 15f),
                Is.False);
        }

        [Test]
        public void IsTargetVisible_장애물이사이에있으면거부한다()
        {
            Transform player = CreateObject("Player", testOrigin).transform;
            Transform view = CreateObject(
                "View",
                testOrigin + Vector3.up * 1.2f).transform;
            Transform target = CreateTarget(
                "Target",
                testOrigin + Vector3.forward * 6f);
            PlayerTargetFinder finder = CreateFinder(player, view);
            Assert.That(finder.IsTargetVisible(target), Is.True);

            GameObject wall = CreateObject(
                "Wall",
                testOrigin + new Vector3(0f, 1.2f, 3f));
            wall.layer = 0;
            wall.transform.localScale = new Vector3(2f, 3f, 0.5f);
            wall.AddComponent<BoxCollider>();
            Physics.SyncTransforms();

            Assert.That(finder.IsTargetVisible(target), Is.False);
        }

        [Test]
        public void VisibilityGrace_잠깐가려지면유지하고시간초과면해제한다()
        {
            var grace = new PlayerTargetVisibilityGrace(0.35f);

            Assert.That(grace.CanKeepTarget(false, 0.2f), Is.True);
            Assert.That(grace.CanKeepTarget(false, 0.15f), Is.True);
            Assert.That(grace.CanKeepTarget(false, 0.001f), Is.False);
            Assert.That(grace.CanKeepTarget(true, 0f), Is.True);
            Assert.That(grace.CanKeepTarget(false, 0.2f), Is.True);
        }

        private PlayerTargetFinder CreateFinder(
            Transform player,
            Transform view)
        {
            return new PlayerTargetFinder(
                player,
                view,
                1 << TargetLayer,
                12f,
                70f,
                1 << 0,
                1.2f);
        }

        private Transform CreateTarget(string name, Vector3 position)
        {
            GameObject target = CreateObject(name, position);
            target.layer = TargetLayer;
            target.AddComponent<SphereCollider>();
            target.AddComponent<PlayerTargetTestDeathState>();
            return target.transform;
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            var createdObject = new GameObject(name);
            createdObject.transform.position = position;
            createdObjects.Add(createdObject);
            return createdObject;
        }
    }

    public sealed class PlayerTargetTestDeathState :
        MonoBehaviour,
        IUnitDeathState
    {
        public bool IsDead { get; set; }
    }
}
