using UnityEngine;

namespace rudIsland.RPG3D.Player.Movement
{
    // 자유 시점에서는 카메라 기준으로 이동하고 이동 방향을 바라본다.
    internal sealed class PlayerFreeLookMovement : IPlayerMovementMode
    {
        private const float MinimumDirectionSqrMagnitude = 0.01f;

        private readonly Transform playerTransform;
        private readonly Transform moveCamera;
        private readonly float turnSpeed;

        public PlayerFreeLookMovement(
            Transform playerTransform,
            Transform moveCamera,
            float turnSpeed)
        {
            this.playerTransform = playerTransform;
            this.moveCamera = moveCamera;
            this.turnSpeed = turnSpeed;
        }

        public Vector3 GetMoveDirection(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                return Vector3.zero;
            }

            Vector3 cameraForward = moveCamera.forward;
            Vector3 cameraRight = moveCamera.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            return (cameraForward * moveInput.y + cameraRight * moveInput.x)
                .normalized * moveInput.magnitude;
        }

        public void UpdateFacing(Vector3 moveDirection, float deltaTime)
        {
            if (moveDirection.sqrMagnitude < MinimumDirectionSqrMagnitude ||
                turnSpeed <= 0f)
            {
                return;
            }

            Quaternion wantedRotation = Quaternion.LookRotation(moveDirection);
            playerTransform.rotation = Quaternion.RotateTowards(
                playerTransform.rotation,
                wantedRotation,
                turnSpeed * deltaTime);
        }
    }
}
