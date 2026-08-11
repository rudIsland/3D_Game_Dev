using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // Nightshade의 수평 이동, 회전, 중력만 계산한다.
    public sealed class NightshadeSpearMovement
    {
        private readonly Transform nightshadeTransform; // 씬 또는 시스템 참조
        private readonly CharacterController characterController; // 씬 또는 시스템 참조
        private readonly float gravity; // 내부에서 사용하는 값
        private readonly float groundPull; // 내부에서 사용하는 값
        private readonly float maximumAttackRootMotionSpeed;
        private readonly float maximumAttackRootMotionTurnSpeed;
        private float verticalSpeed; // 이동 속도

        public Vector3 Position => nightshadeTransform.position; // 이동 정보
        public Vector3 Forward => nightshadeTransform.forward; // 외부에 제공하는 읽기 값
        public Vector3 Right => nightshadeTransform.right; // 외부에 제공하는 읽기 값

        public NightshadeSpearMovement(
            Transform nightshadeTransform,
            CharacterController characterController,
            float gravity,
            float groundPull,
            float maximumAttackRootMotionSpeed = 6f,
            float maximumAttackRootMotionTurnSpeed = 360f)
        {
            this.nightshadeTransform = nightshadeTransform;
            this.characterController = characterController;
            this.gravity = gravity;
            this.groundPull = groundPull;
            this.maximumAttackRootMotionSpeed = Mathf.Max(
                0f,
                maximumAttackRootMotionSpeed);
            this.maximumAttackRootMotionTurnSpeed = Mathf.Max(
                0f,
                maximumAttackRootMotionTurnSpeed);
        }

        public void Reset()
        {
            verticalSpeed = 0f;
        }

        public Vector3 MoveTo(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime,
            bool shouldTurn = true)
        {
            Vector3 moveDirection = targetPosition - nightshadeTransform.position;
            moveDirection.y = 0f;

            if (shouldTurn && moveDirection.sqrMagnitude > 0.0001f)
            {
                moveDirection.Normalize();
                TurnToDirection(moveDirection, turnSpeed, deltaTime);
            }

            UpdateVerticalSpeed(deltaTime);
            Vector3 velocity = moveDirection * moveSpeed;
            velocity.y = verticalSpeed;
            characterController.Move(velocity * deltaTime);
            return moveDirection;
        }

        public Vector3 MoveAwayFrom(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime,
            bool shouldTurn = true)
        {
            Vector3 moveDirection = nightshadeTransform.position -
                targetPosition;
            moveDirection.y = 0f;

            if (shouldTurn && moveDirection.sqrMagnitude > 0.0001f)
            {
                moveDirection.Normalize();
                TurnToDirection(moveDirection, turnSpeed, deltaTime);
            }

            UpdateVerticalSpeed(deltaTime);
            Vector3 velocity = moveDirection * moveSpeed;
            velocity.y = verticalSpeed;
            characterController.Move(velocity * deltaTime);
            return moveDirection;
        }

        public void TurnTo(Vector3 targetPosition, float turnSpeed, float deltaTime)
        {
            Vector3 direction = targetPosition - nightshadeTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                TurnToDirection(direction, turnSpeed, deltaTime);
            }

            StayOnGround(deltaTime);
        }

        public void StayOnGround(float deltaTime)
        {
            UpdateVerticalSpeed(deltaTime);
            characterController.Move(Vector3.up * (verticalSpeed * deltaTime));
        }

        public void ApplyAttackRootMotion(
            Vector3 deltaPosition,
            Quaternion deltaRotation,
            float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            deltaPosition.y = 0f;

            float maximumDistance =
                maximumAttackRootMotionSpeed * safeDeltaTime;
            if (deltaPosition.sqrMagnitude >
                maximumDistance * maximumDistance)
            {
                deltaPosition = deltaPosition.normalized * maximumDistance;
            }

            characterController.Move(deltaPosition);

            Quaternion targetRotation =
                nightshadeTransform.rotation * deltaRotation;
            nightshadeTransform.rotation = Quaternion.RotateTowards(
                nightshadeTransform.rotation,
                targetRotation,
                maximumAttackRootMotionTurnSpeed * safeDeltaTime);
        }

        private void TurnToDirection(Vector3 direction, float turnSpeed, float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            nightshadeTransform.rotation = Quaternion.RotateTowards(
                nightshadeTransform.rotation,
                targetRotation,
                turnSpeed * deltaTime);
        }

        private void UpdateVerticalSpeed(float deltaTime)
        {
            if (characterController.isGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = groundPull;
                return;
            }

            verticalSpeed += gravity * deltaTime;
        }
    }
}
