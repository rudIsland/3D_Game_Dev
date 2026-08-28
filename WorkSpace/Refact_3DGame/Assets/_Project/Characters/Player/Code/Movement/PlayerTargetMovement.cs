using UnityEngine;

namespace Characters.Player.Movement
{
    // 타깃 시점의 타깃 기준 이동·구르기·공격 방향을 계산한다.
    internal sealed class PlayerTargetMovement : IPlayerMovementMode
    {
        private const float MinimumDirectionSqrMagnitude = 0.01f;

        private readonly Transform playerTransform;
        private readonly float turnSpeed;
        private Transform target;

        public PlayerTargetMovement(Transform playerTransform, float turnSpeed)
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
                !TryGetTargetMoveBasis(out Vector3 targetForward, out Vector3 targetRight))
            {
                return Vector3.zero;
            }

            Vector3 moveDirection =
                targetForward * moveInput.y +
                targetRight * moveInput.x;

            return Vector3.ClampMagnitude(moveDirection, 1f) * moveInput.magnitude;
        }

        public Vector2 GetRollDirection(Vector2 moveInput)
        {
            Vector3 targetMoveDirection = GetMoveDirection(moveInput);
            if (targetMoveDirection.sqrMagnitude <
                MinimumDirectionSqrMagnitude)
            {
                return moveInput;
            }

            Vector3 localDirection = playerTransform.InverseTransformDirection(targetMoveDirection.normalized);

            return Vector2.ClampMagnitude(new Vector2(localDirection.x, localDirection.z), 1f);
        }

        public Vector3 GetAttackDirection()
        {
            return TryGetTargetDirection(out Vector3 targetDirection)
                ? targetDirection
                : playerTransform.forward;
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

        private bool TryGetTargetMoveBasis(out Vector3 targetForward, out Vector3 targetRight)
        {
            targetForward = Vector3.zero;
            targetRight = Vector3.zero;

            if (target == null)
            {
                return false;
            }

            targetForward = target.position - playerTransform.position;
            targetForward.y = 0f;
            if (targetForward.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                return false;
            }

            targetForward.Normalize();
            targetRight = Vector3.Cross(Vector3.up, targetForward);
            return true;
        }
    }
}
