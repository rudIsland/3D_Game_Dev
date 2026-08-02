using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // Zombie의 이동, 회전, 중력만 계산한다.
    public sealed class ZombieMovement
    {
        private readonly Transform zombieTransform;
        private readonly CharacterController characterController;
        private readonly float gravity;
        private readonly float groundPull;

        private float verticalSpeed;

        public Vector3 Position => zombieTransform.position;

        public ZombieMovement(
            Transform zombieTransform,
            CharacterController characterController,
            float gravity,
            float groundPull)
        {
            this.zombieTransform = zombieTransform;
            this.characterController = characterController;
            this.gravity = gravity;
            this.groundPull = groundPull;
        }

        public void Reset()
        {
            verticalSpeed = 0f;
        }

        public void MoveTo(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime)
        {
            Vector3 moveDirection =
                targetPosition - zombieTransform.position;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                moveDirection.Normalize();
                TurnToDirection(moveDirection, turnSpeed, deltaTime);
            }

            UpdateVerticalSpeed(deltaTime);

            Vector3 moveVelocity = moveDirection * moveSpeed;
            moveVelocity.y = verticalSpeed;
            characterController.Move(moveVelocity * deltaTime);
        }

        public void TurnTo(
            Vector3 targetPosition,
            float turnSpeed,
            float deltaTime)
        {
            Vector3 lookDirection =
                targetPosition - zombieTransform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                TurnToDirection(lookDirection, turnSpeed, deltaTime);
            }

            StayOnGround(deltaTime);
        }

        public bool IsFacing(
            Vector3 targetPosition,
            float minimumFacingDot)
        {
            Vector3 targetDirection =
                targetPosition - zombieTransform.position;
            targetDirection.y = 0f;

            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            targetDirection.Normalize();
            return Vector3.Dot(
                    zombieTransform.forward,
                    targetDirection) >= minimumFacingDot;
        }

        public void StayOnGround(float deltaTime)
        {
            UpdateVerticalSpeed(deltaTime);
            characterController.Move(
                Vector3.up * (verticalSpeed * deltaTime));
        }

        private void TurnToDirection(
            Vector3 direction,
            float turnSpeed,
            float deltaTime)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);
            zombieTransform.rotation = Quaternion.RotateTowards(
                zombieTransform.rotation,
                targetRotation,
                turnSpeed * deltaTime);
        }

        private void UpdateVerticalSpeed(float deltaTime)
        {
            if (characterController.isGrounded &&
                verticalSpeed < 0f)
            {
                verticalSpeed = groundPull;
                return;
            }

            verticalSpeed += gravity * deltaTime;
        }
    }
}
