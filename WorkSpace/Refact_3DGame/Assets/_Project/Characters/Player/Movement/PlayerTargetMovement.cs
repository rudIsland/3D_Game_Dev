using UnityEngine;

namespace rudIsland.RPG3D.Player.Movement
{
    // 타깃 시점에서는 타깃 기준으로 이동하고 입력과 관계없이 타깃을 바라본다.
    internal sealed class PlayerTargetMovement : IPlayerMovementMode
    {
        private const float MinimumDirectionSqrMagnitude = 0.01f;

        private readonly Transform playerTransform;
        private readonly float turnSpeed;
        private Transform target;

        public PlayerTargetMovement(
            Transform playerTransform,
            float turnSpeed)
        {
            this.playerTransform = playerTransform;
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
                !TryGetTargetDirection(out Vector3 targetForward))
            {
                return Vector3.zero;
            }

            Vector3 targetRight = Vector3.Cross(Vector3.up, targetForward);
            return (targetForward * moveInput.y + targetRight * moveInput.x)
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
    }
}
