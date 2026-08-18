using Cinemachine;
using NUnit.Framework;
using rudIsland.RPG3D.Player.Camera;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class PlayerTargetCameraTests
    {
        [Test]
        public void 상태전환_Inspector우선순위를바꾸지않는다()
        {
            var freeCameraObject = new GameObject("FreeCamera");
            var targetCameraObject = new GameObject("TargetCamera");
            var selectedTargetObject = new GameObject("SelectedTarget");

            try
            {
                CinemachineFreeLook freeCamera =
                    freeCameraObject.AddComponent<CinemachineFreeLook>();
                CinemachineFreeLook targetCamera =
                    targetCameraObject.AddComponent<CinemachineFreeLook>();
                freeCamera.Priority = 10;
                targetCamera.Priority = 20;
                targetCamera.enabled = false;

                var playerCamera = new PlayerTargetCamera(
                    freeCamera,
                    targetCamera);

                playerCamera.SetTarget(selectedTargetObject.transform);

                Assert.That(freeCamera.enabled, Is.True);
                Assert.That(targetCamera.enabled, Is.True);
                Assert.That(
                    targetCamera.LookAt,
                    Is.SameAs(selectedTargetObject.transform));
                Assert.That(freeCamera.Priority, Is.EqualTo(10));
                Assert.That(targetCamera.Priority, Is.EqualTo(20));

                playerCamera.SetFreeLook();

                Assert.That(freeCamera.enabled, Is.True);
                Assert.That(targetCamera.enabled, Is.False);
                Assert.That(targetCamera.LookAt, Is.Null);
                Assert.That(freeCamera.Priority, Is.EqualTo(10));
                Assert.That(targetCamera.Priority, Is.EqualTo(20));
            }
            finally
            {
                Object.DestroyImmediate(freeCameraObject);
                Object.DestroyImmediate(targetCameraObject);
                Object.DestroyImmediate(selectedTargetObject);
            }
        }
    }
}
