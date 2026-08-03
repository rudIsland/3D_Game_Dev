using Cinemachine;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Camera
{
    // 기존 FreeLook 카메라를 Target 상태에서만 적 방향으로 정렬한다.
    public sealed class PlayerTargetCamera
    {
        private const float MinimumDirectionSqrMagnitude = 0.01f;

        private readonly Transform playerTransform;
        private readonly CinemachineFreeLook freeLookCamera;
        private readonly CinemachineInputProvider inputProvider;
        private readonly float turnSpeed;
        private readonly float targetVerticalValue;
        private Transform target;

        public PlayerTargetCamera(
            Transform playerTransform,
            CinemachineFreeLook freeLookCamera,
            float turnSpeed,
            float targetVerticalValue)
        {
            this.playerTransform = playerTransform;
            this.freeLookCamera = freeLookCamera;
            this.turnSpeed = Mathf.Max(0f, turnSpeed);
            this.targetVerticalValue = Mathf.Clamp01(targetVerticalValue);
            if (freeLookCamera != null)
            {
                inputProvider =
                    freeLookCamera.GetComponent<CinemachineInputProvider>();
            }
        }

        public void SetFreeLook()
        {
            target = null;
            if (inputProvider != null)
            {
                inputProvider.enabled = true;
            }
        }

        public void SetTarget(Transform nextTarget)
        {
            target = nextTarget;
            if (inputProvider != null)
            {
                inputProvider.enabled = false;
            }
        }

        public void Update(float deltaTime)
        {
            if (freeLookCamera == null || target == null)
            {
                return;
            }

            Vector3 targetDirection =
                target.position - playerTransform.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude <
                MinimumDirectionSqrMagnitude)
            {
                return;
            }

            float targetHeading = Mathf.Atan2(
                targetDirection.x,
                targetDirection.z) * Mathf.Rad2Deg;
            freeLookCamera.m_XAxis.Value = Mathf.MoveTowardsAngle(
                freeLookCamera.m_XAxis.Value,
                targetHeading,
                turnSpeed * deltaTime);
            freeLookCamera.m_YAxis.Value = Mathf.MoveTowards(
                freeLookCamera.m_YAxis.Value,
                targetVerticalValue,
                deltaTime);
        }
    }
}
