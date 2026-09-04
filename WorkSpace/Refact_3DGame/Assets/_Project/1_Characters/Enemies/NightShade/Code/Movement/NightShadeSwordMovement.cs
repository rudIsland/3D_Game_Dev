using UnityEngine;

namespace Characters.Enemies.NightShade
{
    // NightShade 정예 적의 이동, 회전, 중력만 계산한다.
    internal sealed class NightShadeSwordMovement : INightShadeSwordMovement
    {
        private readonly Transform enemyTransform;
        private readonly CharacterController characterController;
        private readonly float walkSpeed;
        private readonly float chaseSpeed;
        private readonly float turnSpeed;
        private readonly float attackTurnSpeed;
        private readonly float recoveryMoveSpeed;
        private readonly float gravity;
        private readonly float groundPull;

        private float verticalSpeed;

        public Vector3 Position => enemyTransform.position;
        public Vector3 Forward => enemyTransform.forward;

        internal NightShadeSwordMovement(
            Transform enemyTransform,
            CharacterController characterController,
            NightShadeSwordMovementRuntimeConfig settings,
            float recoveryMoveSpeed)
        {
            this.enemyTransform = enemyTransform;
            this.characterController = characterController;
            walkSpeed = settings.WalkSpeed;
            chaseSpeed = settings.ChaseSpeed;
            turnSpeed = settings.TurnSpeed;
            attackTurnSpeed = settings.AttackTurnSpeed;
            this.recoveryMoveSpeed = Mathf.Max(0f, recoveryMoveSpeed);
            gravity = settings.Gravity;
            groundPull = settings.GroundPull;
        }

        public void Reset()
        {
            verticalSpeed = 0f;
        }

        public void ChaseTarget(Vector3 targetPosition, float deltaTime)
        {
            MoveToTarget(targetPosition, chaseSpeed, deltaTime);
        }

        public void WalkToTarget(Vector3 targetPosition, float deltaTime)
        {
            MoveToTarget(targetPosition, walkSpeed, deltaTime);
        }

        public void TurnToTarget(Vector3 targetPosition, float deltaTime)
        {
            Vector3 direction = targetPosition - enemyTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                TurnToDirection(direction, turnSpeed, deltaTime);
            }

            StayOnGround(deltaTime);
        }

        private void MoveToTarget(
            Vector3 targetPosition,
            float moveSpeed,
            float deltaTime)
        {
            Vector3 moveDirection = targetPosition - enemyTransform.position;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                moveDirection.Normalize();
                TurnToDirection(moveDirection, turnSpeed, deltaTime);
            }

            UpdateVerticalSpeed(deltaTime);
            Vector3 movement = moveDirection * (moveSpeed * deltaTime);
            movement.y = verticalSpeed * deltaTime;
            characterController.Move(movement);
        }

        public void MoveForRecovery(
            Vector3 targetPosition,
            NightShadeCombatMoveType moveType,
            float deltaTime)
        {
            Vector3 targetDirection = targetPosition - enemyTransform.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                StayOnGround(deltaTime);
                return;
            }

            targetDirection.Normalize();
            Vector3 rightDirection = Vector3.Cross(Vector3.up, targetDirection);
            Vector3 moveDirection;
            switch (moveType)
            {
                case NightShadeCombatMoveType.Left:
                    moveDirection = -rightDirection;
                    break;
                case NightShadeCombatMoveType.Right:
                    moveDirection = rightDirection;
                    break;
                default:
                    moveDirection = -targetDirection;
                    break;
            }

            TurnToDirection(targetDirection, turnSpeed, deltaTime);
            UpdateVerticalSpeed(deltaTime);
            Vector3 movement =
                moveDirection * (recoveryMoveSpeed * deltaTime);
            movement.y = verticalSpeed * deltaTime;
            characterController.Move(movement);
        }

        public void StayOnGround(float deltaTime)
        {
            UpdateVerticalSpeed(deltaTime);
            characterController.Move(Vector3.up * (verticalSpeed * deltaTime));
        }

        public void ApplyAttackMovement(
            Vector3 wantedTurnDirection,
            bool canTurn,
            float deltaDistance,
            float deltaTime)
        {
            wantedTurnDirection.y = 0f;
            if (canTurn &&
                wantedTurnDirection.sqrMagnitude > 0.0001f)
            {
                TurnToDirection(
                    wantedTurnDirection,
                    attackTurnSpeed,
                    deltaTime);
            }

            UpdateVerticalSpeed(deltaTime);
            Vector3 movement = enemyTransform.forward * deltaDistance;
            movement.y = verticalSpeed * deltaTime;
            characterController.Move(movement);
        }

        public void ApplyHitMovement(Vector3 horizontalMovement, float deltaTime)
        {
            horizontalMovement.y = 0f;
            UpdateVerticalSpeed(deltaTime);
            horizontalMovement.y = verticalSpeed * deltaTime;
            characterController.Move(horizontalMovement);
        }

        private void TurnToDirection(
            Vector3 direction,
            float turnSpeed,
            float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            enemyTransform.rotation = Quaternion.RotateTowards(
                enemyTransform.rotation,
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
