using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class UnitMovementSeparationTests
    {
        private const int PlayerLayer = 6;
        private const int EnemyLayer = 7;
        private const float MinimumSeparation = 0.2f;

        private GameObject ownerObject;
        private GameObject nearUnitObject;
        private GameObject farUnitObject;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(farUnitObject);
            Object.DestroyImmediate(nearUnitObject);
            Object.DestroyImmediate(ownerObject);
        }

        [Test]
        public void ApproachMovement_StopsAtMinimumSeparation()
        {
            UnitMovementSeparation separation = CreateSeparation(
                out _,
                new Vector3(0f, 0f, 2f));

            Vector3 limitedMovement =
                separation.LimitApproachMovement(
                    new Vector3(0f, 0f, 2f));

            Assert.That(limitedMovement.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(limitedMovement.z, Is.EqualTo(1.199f).Within(0.01f));
        }

        [Test]
        public void SideMovement_IsPreserved()
        {
            UnitMovementSeparation separation = CreateSeparation(
                out _,
                new Vector3(0f, 0f, 0.75f));

            Vector3 limitedMovement =
                separation.LimitApproachMovement(Vector3.right);

            Assert.That(limitedMovement, Is.EqualTo(Vector3.right));
        }

        [Test]
        public void RetreatMovement_IsPreserved()
        {
            UnitMovementSeparation separation = CreateSeparation(
                out _,
                new Vector3(0f, 0f, 0.75f));

            Vector3 limitedMovement =
                separation.LimitApproachMovement(Vector3.back);

            Assert.That(limitedMovement, Is.EqualTo(Vector3.back));
        }

        [Test]
        public void VerticalMovement_IsPreserved()
        {
            UnitMovementSeparation separation = CreateSeparation(
                out _,
                new Vector3(0f, 0f, 0.75f));

            Vector3 limitedMovement =
                separation.LimitApproachMovement(Vector3.down);

            Assert.That(limitedMovement, Is.EqualTo(Vector3.down));
        }

        [Test]
        public void MultipleUnits_UsesNearestAllowedDistance()
        {
            UnitMovementSeparation separation = CreateSeparation(
                out _,
                new Vector3(0f, 0f, 2f));
            farUnitObject = CreateUnit(
                "FarUnit",
                new Vector3(0f, 0f, 3f),
                EnemyLayer);
            Physics.SyncTransforms();

            Vector3 limitedMovement =
                separation.LimitApproachMovement(
                    new Vector3(0f, 0f, 3f));

            Assert.That(limitedMovement.z, Is.EqualTo(1.199f).Within(0.01f));
        }

        [Test]
        public void MovementSources_UseSameSeparationRule()
        {
            UnitMovementSeparation separation = CreateSeparation(
                out _,
                new Vector3(0f, 0f, 2f));
            Vector3 requestedMovement = new Vector3(0.25f, -0.1f, 2f);

            Vector3 normalMovement =
                separation.LimitApproachMovement(requestedMovement);
            Vector3 rootMovement =
                separation.LimitApproachMovement(requestedMovement);

            Assert.That(rootMovement, Is.EqualTo(normalMovement));
        }

        private UnitMovementSeparation CreateSeparation(
            out CharacterController ownerController,
            Vector3 otherPosition)
        {
            ownerObject = CreateUnit(
                "Owner",
                Vector3.zero,
                PlayerLayer);
            nearUnitObject = CreateUnit(
                "NearUnit",
                otherPosition,
                EnemyLayer);
            ownerController =
                ownerObject.GetComponent<CharacterController>();
            Physics.SyncTransforms();

            return new UnitMovementSeparation(
                ownerController,
                (1 << PlayerLayer) | (1 << EnemyLayer),
                MinimumSeparation);
        }

        private static GameObject CreateUnit(
            string name,
            Vector3 position,
            int layer)
        {
            var unitObject = new GameObject(name)
            {
                layer = layer
            };
            unitObject.transform.position = position;

            CharacterController controller =
                unitObject.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            controller.height = 2f;
            controller.radius = 0.3f;
            return unitObject;
        }
    }
}
