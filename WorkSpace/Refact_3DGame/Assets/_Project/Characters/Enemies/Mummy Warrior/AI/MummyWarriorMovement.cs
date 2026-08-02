using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.MummyWarrior
{
    // Mummy Warrior의 수평 이동, 회전, 중력만 계산한다.
    public sealed class MummyWarriorMovement
    {
        private readonly Transform mummyTransform;
        private readonly CharacterController characterController;
        private readonly float gravity;
        private readonly float groundPull;
        private float verticalSpeed;

        public Vector3 Position => mummyTransform.position;
        public Vector3 Forward => mummyTransform.forward;
        public Vector3 Right => mummyTransform.right;

        public MummyWarriorMovement(
            Transform mummyTransform,
            CharacterController characterController,
            float gravity,
            float groundPull)
        {
            this.mummyTransform = mummyTransform;
            this.characterController = characterController;
            this.gravity = gravity;
            this.groundPull = groundPull;
        }

        public void Reset() => verticalSpeed = 0f;

        public Vector3 MoveTo(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime)
        {
            Vector3 moveDirection = targetPosition - mummyTransform.position;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude > 0.0001f)
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
            Vector3 direction = targetPosition - mummyTransform.position;
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

        private void TurnToDirection(Vector3 direction, float turnSpeed, float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            mummyTransform.rotation = Quaternion.RotateTowards(
                mummyTransform.rotation,
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
