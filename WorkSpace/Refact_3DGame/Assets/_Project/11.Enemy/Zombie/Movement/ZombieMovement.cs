using Characters.Enemies.Navigation;
using UnityEngine;

namespace Characters.Enemies.Zombie
{
    // Zombie의 이동, 회전, 중력만 계산한다.
    public sealed class ZombieMovement
    {
        private readonly Transform zombieTransform; // 씬 또는 시스템 참조
        private readonly CharacterController characterController; // 씬 또는 시스템 참조
        private readonly IEnemyPathGuide pathGuide;
        private readonly float gravity; // 내부에서 사용하는 값
        private readonly float groundPull; // 내부에서 사용하는 값

        private float verticalSpeed; // 이동 속도

        public Vector3 Position => zombieTransform.position; // 이동 정보
        public Vector3 Forward => zombieTransform.forward; // 좀비가 바라보는 방향
        public Vector3 Right => zombieTransform.right; // 좀비의 오른쪽 방향

        internal ZombieMovement(
            Transform zombieTransform,
            CharacterController characterController,
            IEnemyPathGuide pathGuide,
            float gravity,
            float groundPull)
        {
            this.zombieTransform = zombieTransform;
            this.characterController = characterController;
            this.pathGuide = pathGuide;
            this.gravity = gravity;
            this.groundPull = groundPull;
        }

        public void Reset()
        {
            verticalSpeed = 0f;
            pathGuide.Reset();
        }

        public void StopPath()
        {
            pathGuide.Stop();
        }

        public void MoveTo(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime)
        {
            if (pathGuide.TryGetMoveDirection(
                    targetPosition,
                    deltaTime,
                    out Vector3 moveDirection))
            {
                TurnToDirection(moveDirection, turnSpeed, deltaTime);
            }

            UpdateVerticalSpeed(deltaTime);

            Vector3 moveVelocity = moveDirection * moveSpeed;
            moveVelocity.y = verticalSpeed;
            ApplyMovement(moveVelocity * deltaTime);
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

        public bool IsFacing(Vector3 targetPosition, float minimumFacingDot)
        {
            Vector3 targetDirection =
                targetPosition - zombieTransform.position;
            targetDirection.y = 0f;

            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            targetDirection.Normalize();
            return Vector3.Dot(zombieTransform.forward, targetDirection) >= minimumFacingDot;
        }

        public void StayOnGround(float deltaTime)
        {
            UpdateVerticalSpeed(deltaTime);
            ApplyMovement(Vector3.up * (verticalSpeed * deltaTime));
        }

        internal void ApplyHitMovement(Vector3 horizontalMovement, float deltaTime)
        {
            horizontalMovement.y = 0f;
            UpdateVerticalSpeed(deltaTime);
            horizontalMovement.y = verticalSpeed * deltaTime;
            ApplyMovement(horizontalMovement);
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

        private void ApplyMovement(Vector3 requestedMovement)
        {
            characterController.Move(requestedMovement);
        }
    }
}
