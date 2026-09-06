using Characters.Combat;
using NUnit.Framework;
using UnityEngine;
using World.Zones;

namespace Tests.World
{
    public sealed class EnemyZoneAreaTests
    {
        private GameObject zoneObject;
        private BoxCollider zoneCollider;

        [SetUp]
        public void SetUp()
        {
            zoneObject = new GameObject("EnemyZoneAreaTests");
            zoneCollider = zoneObject.AddComponent<BoxCollider>();
            zoneCollider.center = new Vector3(1f, 2f, -1f);
            zoneCollider.size = new Vector3(10f, 4f, 8f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(zoneObject);
        }

        [Test]
        public void Contains_안쪽과경계는참이고바깥은거짓이다()
        {
            var area = new EnemyZoneArea(zoneCollider);

            Assert.That(area.Contains(new Vector3(1f, 0f, -1f)), Is.True);
            Assert.That(area.Contains(new Vector3(6f, 0f, -1f)), Is.True);
            Assert.That(area.Contains(new Vector3(6.01f, 0f, -1f)), Is.False);
        }

        [Test]
        public void Contains_바깥여유거리만큼추가로허용한다()
        {
            var area = new EnemyZoneArea(zoneCollider);

            Assert.That(area.Contains(new Vector3(6.9f, 0f, -1f), 1f), Is.True);
            Assert.That(area.Contains(new Vector3(7.01f, 0f, -1f), 1f), Is.False);
        }

        [Test]
        public void Contains_Y높이가달라도XZ가같으면안쪽이다()
        {
            var area = new EnemyZoneArea(zoneCollider);

            Assert.That(area.Contains(new Vector3(1f, 1000f, -1f)), Is.True);
            Assert.That(area.Contains(new Vector3(1f, -1000f, -1f)), Is.True);
        }

        [Test]
        public void Contains_회전과크기가바뀐Collider의로컬경계를사용한다()
        {
            zoneObject.transform.SetPositionAndRotation(
                new Vector3(12f, 5f, -7f),
                Quaternion.Euler(0f, 37f, 0f));
            zoneObject.transform.localScale = new Vector3(2f, 3f, 0.5f);
            var area = new EnemyZoneArea(zoneCollider);
            Vector3 insidePoint = zoneObject.transform.TransformPoint(
                zoneCollider.center + new Vector3(4.9f, 100f, 3.9f));
            Vector3 outsidePoint = zoneObject.transform.TransformPoint(
                zoneCollider.center + new Vector3(5.1f, 0f, 0f));

            Assert.That(area.Contains(insidePoint), Is.True);
            Assert.That(area.Contains(outsidePoint), Is.False);
        }

        [Test]
        public void StopPointRecover_0아래로내리지않고변경여부를반환한다()
        {
            var stopPoint = new StopPoint(10f, 3f, 5f);
            stopPoint.TryAccumulate(4f);

            Assert.That(stopPoint.Recover(1.5f), Is.True);
            Assert.That(stopPoint.CurrentPoint, Is.EqualTo(2.5f));
            Assert.That(stopPoint.Recover(10f), Is.True);
            Assert.That(stopPoint.CurrentPoint, Is.Zero);
            Assert.That(stopPoint.Recover(1f), Is.False);
            Assert.That(stopPoint.CurrentPoint, Is.Zero);
        }
    }
}
