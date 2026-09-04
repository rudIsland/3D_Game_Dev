using Cinemachine;
using UnityEngine;

namespace Characters.Player.Camera
{
    // 상태가 선택한 Cinemachine 카메라와 런타임 LookAt 대상만 연결한다.
    public sealed class PlayerTargetCamera
    {
        private readonly CinemachineFreeLook freeLookCamera;
        private readonly CinemachineFreeLook targetLookCamera;

        public PlayerTargetCamera(
            CinemachineFreeLook freeLookCamera,
            CinemachineFreeLook targetLookCamera)
        {
            this.freeLookCamera = freeLookCamera;
            this.targetLookCamera = targetLookCamera;
        }

        public void SetFreeLook()
        {
            if (freeLookCamera != null)
            {
                freeLookCamera.enabled = true;
            }

            if (targetLookCamera == null)
            {
                return;
            }

            targetLookCamera.LookAt = null;
            targetLookCamera.enabled = false;
        }

        public void SetTarget(Transform nextTarget)
        {
            if (nextTarget == null ||
                freeLookCamera == null ||
                targetLookCamera == null)
            {
                SetFreeLook();
                return;
            }

            targetLookCamera.LookAt = nextTarget;
            targetLookCamera.enabled = true;
        }
    }
}
