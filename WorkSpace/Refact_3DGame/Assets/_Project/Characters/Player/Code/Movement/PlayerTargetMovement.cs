using UnityEngine;

namespace rudIsland.RPG3D.Player.Movement
{
    // 타깃 시점에서는 카메라 기준으로 이동하고 몸만 타깃을 바라본다.
    internal sealed class PlayerTargetMovement : IPlayerMovementMode
    {
        private const float MinimumDirectionSqrMagnitude = 0.01f;

        private readonly Transform playerTransform;
        private readonly Transform moveCamera;
        private readonly float turnSpeed;
        private Transform target;

        public PlayerTargetMovement(
            Transform playerTransform,
            Transform moveCamera,
            float turnSpeed)
        {
            this.playerTransform = playerTransform;
            this.moveCamera = moveCamera;
            this.turnSpeed = turnSpeed;
        }

        public void SetTarget(Transform nextTarget)
        {
            target = nextTarget;
        }

        public void ClearTarget()
        {
            target = null;
        }

        public Vector3 GetMoveDirection(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < MinimumDirectionSqrMagnitude ||
                !TryGetCameraDirection(
                    out Vector3 cameraForward,
                    out Vector3 cameraRight))
            {
                return Vector3.zero;
            }

            return (cameraForward * moveInput.y + cameraRight * moveInput.x)
                .normalized * moveInput.magnitude;
        }

        public void UpdateFacing(Vector3 moveDirection, float deltaTime)
        {
            if (turnSpeed <= 0f ||
                !TryGetTargetDirection(out Vector3 targetDirection))
            {
                return;
            }

            Quaternion wantedRotation = Quaternion.LookRotation(targetDirection);
            playerTransform.rotation = Quaternion.RotateTowards(
                playerTransform.rotation,
                wantedRotation,
                turnSpeed * deltaTime);
        }

        private bool TryGetTargetDirection(out Vector3 direction)
        {
            direction = Vector3.zero;
            if (target == null)
            {
                return false;
            }

            direction = target.position - playerTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }

        private bool TryGetCameraDirection(
            out Vector3 forward,
            out Vector3 right)
        {
            forward = Vector3.zero;
            right = Vector3.zero;
            if (moveCamera == null)
            {
                return false;
            }

            forward = moveCamera.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                return false;
            }

            forward.Normalize();
            right = Vector3.Cross(Vector3.up, forward);
            return true;
        }
    }
}
