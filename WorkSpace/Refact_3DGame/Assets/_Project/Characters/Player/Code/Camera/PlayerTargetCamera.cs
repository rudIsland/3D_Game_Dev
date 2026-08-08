using Cinemachine;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Camera
{
    // FreeLook과 TargetLook 카메라의 시점을 이어서 전환한다.
    public sealed class PlayerTargetCamera
    {
        private const float MinimumDirectionSqrMagnitude = 0.01f;
        private const float TargetRootHeightOffset = 0f;
        private const int FreeLookRigCount = 3;

        private readonly Transform playerTransform;
        private readonly CinemachineFreeLook freeLookCamera;
        private readonly CinemachineFreeLook targetLookCamera;
        private readonly CinemachineComposer[] targetLookComposers;
        private readonly Vector3[] targetLookComposerOffsets;
        private readonly int activePriority;
        private readonly int inactivePriority;
        private readonly float turnSpeed;
        private readonly float targetVerticalValue;
        private Transform targetRoot;
        private bool isTargetLookActive;

        public PlayerTargetCamera(
            Transform playerTransform,
            CinemachineFreeLook freeLookCamera,
            CinemachineFreeLook targetLookCamera,
            int activePriority,
            int inactivePriority,
            float turnSpeed,
            float targetVerticalValue)
        {
            this.playerTransform = playerTransform;
            this.freeLookCamera = freeLookCamera;
            this.targetLookCamera = targetLookCamera;
            this.activePriority = activePriority;
            this.inactivePriority = inactivePriority;
            this.turnSpeed = Mathf.Max(0f, turnSpeed);
            this.targetVerticalValue = Mathf.Clamp01(targetVerticalValue);

            targetLookComposers =
                new CinemachineComposer[FreeLookRigCount];
            targetLookComposerOffsets =
                new Vector3[FreeLookRigCount];
            CacheTargetLookComposers();

            if (freeLookCamera != null && targetLookCamera != null)
            {
                targetLookCamera.Follow = freeLookCamera.Follow;
            }

            SetFreeLook();
        }

        public void SetFreeLook()
        {
            if (isTargetLookActive &&
                freeLookCamera != null &&
                targetLookCamera != null)
            {
                CopyAxisValues(targetLookCamera, freeLookCamera);
            }

            isTargetLookActive = false;
            targetRoot = null;

            SetCameraPriorities(
                activePriority,
                inactivePriority);

            if (targetLookCamera == null)
            {
                return;
            }

            targetLookCamera.LookAt = null;
            ApplyTargetHeightOffset(0f);
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

            targetRoot = nextTarget;
            targetLookCamera.LookAt = nextTarget;
            ApplyTargetHeightOffset(TargetRootHeightOffset);

            CopyAxisValues(freeLookCamera, targetLookCamera);
            SetCameraPriorities(
                inactivePriority,
                activePriority);
            isTargetLookActive = true;
        }

        public void Update(float deltaTime)
        {
            if (!isTargetLookActive ||
                targetLookCamera == null ||
                targetRoot == null)
            {
                return;
            }

            Vector3 targetDirection =
                targetRoot.position - playerTransform.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude <
                MinimumDirectionSqrMagnitude)
            {
                return;
            }

            float targetHeading = Mathf.Atan2(
                targetDirection.x,
                targetDirection.z) * Mathf.Rad2Deg;
            targetLookCamera.m_XAxis.Value = Mathf.MoveTowardsAngle(
                targetLookCamera.m_XAxis.Value,
                targetHeading,
                turnSpeed * deltaTime);
            targetLookCamera.m_YAxis.Value = Mathf.MoveTowards(
                targetLookCamera.m_YAxis.Value,
                targetVerticalValue,
                deltaTime);
        }

        private void CacheTargetLookComposers()
        {
            if (targetLookCamera == null)
            {
                return;
            }

            for (int index = 0; index < FreeLookRigCount; index++)
            {
                CinemachineVirtualCamera rig =
                    targetLookCamera.GetRig(index);
                if (rig == null)
                {
                    continue;
                }

                CinemachineComposer composer =
                    rig.GetCinemachineComponent<CinemachineComposer>();
                targetLookComposers[index] = composer;
                if (composer != null)
                {
                    targetLookComposerOffsets[index] =
                        composer.m_TrackedObjectOffset;
                }
            }
        }

        private void ApplyTargetHeightOffset(float height)
        {
            for (int index = 0; index < FreeLookRigCount; index++)
            {
                CinemachineComposer composer =
                    targetLookComposers[index];
                if (composer == null)
                {
                    continue;
                }

                Vector3 offset = targetLookComposerOffsets[index];
                offset.y += height;
                composer.m_TrackedObjectOffset = offset;
            }
        }

        private void SetCameraPriorities(
            int freeLookPriority,
            int targetLookPriority)
        {
            if (freeLookCamera != null)
            {
                freeLookCamera.Priority = freeLookPriority;
            }

            if (targetLookCamera != null)
            {
                targetLookCamera.Priority = targetLookPriority;
            }
        }

        private static void CopyAxisValues(
            CinemachineFreeLook source,
            CinemachineFreeLook destination)
        {
            destination.m_XAxis.Value = source.m_XAxis.Value;
            destination.m_YAxis.Value = source.m_YAxis.Value;
        }
    }
}
