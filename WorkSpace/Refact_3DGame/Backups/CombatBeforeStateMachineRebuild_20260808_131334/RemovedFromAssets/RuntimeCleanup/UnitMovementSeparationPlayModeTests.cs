using System.Collections;
using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using UnityEngine;
using UnityEngine.TestTools;

namespace rudIsland.RPG3D.Tests
{
    public sealed class UnitMovementSeparationPlayModeTests
    {
        private const int PlayerLayer = 6;
        private const int EnemyLayer = 7;
        private const float ControllerRadius = 0.3f;
        private const float MinimumSeparation = 0.2f;

        private GameObject ownerObject;
        private GameObject targetObject;

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(ownerObject);
            Object.Destroy(targetObject);
        }

        [UnityTest]
        public IEnumerator CharacterControllerMove_KeepsMinimumSeparation()
        {
            CharacterController ownerController = CreateUnit(
                "Owner",
                Vector3.zero,
                PlayerLayer,
                out ownerObject);
            CreateUnit(
                "Target",
                new Vector3(0f, 0f, 2f),
                EnemyLayer,
                out targetObject);
            var separation = new UnitMovementSeparation(
                ownerController,
                (1 << PlayerLayer) | (1 << EnemyLayer),
                MinimumSeparation);

            yield return null;

            Vector3 limitedMovement =
                separation.LimitApproachMovement(
                    new Vector3(0f, 0f, 2f));
            ownerController.Move(limitedMovement);

            yield return null;

            Vector3 centerOffset =
                targetObject.transform.position -
                ownerObject.transform.position;
            centerOffset.y = 0f;
            float surfaceDistance = centerOffset.magnitude -
                ControllerRadius * 2f;
            Assert.That(
                surfaceDistance,
                Is.GreaterThanOrEqualTo(
                    MinimumSeparation - 0.01f));
        }

        private static CharacterController CreateUnit(
            string name,
            Vector3 position,
            int layer,
            out GameObject unitObject)
        {
            unitObject = new GameObject(name)
            {
                layer = layer
            };
            unitObject.transform.position = position;

            CharacterController controller =
                unitObject.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            controller.height = 2f;
            controller.radius = ControllerRadius;
            return controller;
        }
    }
}
