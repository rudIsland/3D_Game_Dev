using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    // 보스의 모든 실제 위치 변경을 CharacterController.Move 한곳으로 모은다.
    public sealed class DemonSwordsmanMovement : IDemonSwordsmanMovement
    {
        private const float DirectionEpsilon = 0.0001f; // 이동 정보

        private readonly Transform bossTransform; // 씬 또는 시스템 참조
        private readonly CharacterController characterController; // 씬 또는 시스템 참조
        private readonly float gravity; // 내부에서 사용하는 값
        private readonly float groundPull; // 내부에서 사용하는 값

        private float verticalSpeed; // 이동 속도
        private bool useAttackRootMove; // 기능 사용 여부
        private float attackRootMoveMultiplier; // 공격 관련 설정 또는 상태

        public Vector3 Position => bossTransform.position; // 이동 정보
        public float MoveForward { get; private set; } // 이동 정보
        public float MoveSide { get; private set; } // 이동 정보
        public float MoveAmount { get; private set; } // 이동 정보

        public DemonSwordsmanMovement(
            Transform bossTransform,
            CharacterController characterController,
            float gravity,
            float groundPull)
        {
            this.bossTransform = bossTransform;
            this.characterController = characterController;
            this.gravity = gravity;
            this.groundPull = groundPull;
        }

        public void ResetMovement()
        {
            verticalSpeed = 0f;
            useAttackRootMove = false;
            attackRootMoveMultiplier = 1f;
            ClearMoveValues();
        }

        public void MoveTo(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime)
        {
            Vector3 direction = GetFlatDirection(targetPosition);

            if (direction.sqrMagnitude <= DirectionEpsilon)
            {
                Stop(deltaTime);
                return;
            }

            direction.Normalize();
            RotateTowards(direction, turnSpeed, deltaTime);
            MoveHorizontal(direction * moveSpeed, moveSpeed, deltaTime);
        }

        public void CircleAround(
            Vector3 targetPosition,
            float moveSpeed,
            float preferredDistance,
            float sideDirection,
            float turnSpeed,
            float deltaTime)
        {
            Vector3 toTarget = GetFlatDirection(targetPosition);
            float distance = toTarget.magnitude;

            if (distance <= DirectionEpsilon)
            {
                Stop(deltaTime);
                return;
            }

            Vector3 targetDirection = toTarget / distance;
            Vector3 sideMove = Vector3.Cross(Vector3.up, targetDirection) *
                Mathf.Sign(sideDirection);
            float distanceError = distance - preferredDistance;
            Vector3 distanceCorrection = targetDirection *
                Mathf.Clamp(distanceError, -1f, 1f) * 0.55f;
            Vector3 moveDirection = sideMove + distanceCorrection;

            if (moveDirection.sqrMagnitude > DirectionEpsilon)
            {
                moveDirection.Normalize();
            }

            RotateTowards(targetDirection, turnSpeed, deltaTime);
            MoveHorizontal(moveDirection * moveSpeed, moveSpeed, deltaTime);
        }

        public void BackAwayFrom(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime)
        {
            Vector3 toTarget = GetFlatDirection(targetPosition);

            if (toTarget.sqrMagnitude <= DirectionEpsilon)
            {
                Stop(deltaTime);
                return;
            }

            toTarget.Normalize();
            RotateTowards(toTarget, turnSpeed, deltaTime);
            MoveHorizontal(-toTarget * moveSpeed, moveSpeed, deltaTime);
        }

        public void TurnTo(
            Vector3 targetPosition,
            float turnSpeed,
            float deltaTime)
        {
            Vector3 direction = GetFlatDirection(targetPosition);

            if (direction.sqrMagnitude > DirectionEpsilon)
            {
                RotateTowards(direction.normalized, turnSpeed, deltaTime);
            }
        }

        public void StayOnGround(float deltaTime)
        {
            ClearMoveValues();
            MoveCharacter(Vector3.zero, deltaTime);
        }

        public void Stop(float deltaTime)
        {
            ClearMoveValues();
            MoveCharacter(Vector3.zero, deltaTime);
        }

        public void SetAttackRootMove(
            bool isEnabled,
            float moveMultiplier)
        {
            useAttackRootMove = isEnabled;
            attackRootMoveMultiplier = Mathf.Max(0f, moveMultiplier);
        }

        public void ApplyAttackAnimationMove(Vector3 animationMove)
        {
            if (!useAttackRootMove || !characterController.enabled)
            {
                return;
            }

            animationMove.y = 0f;
            animationMove *= attackRootMoveMultiplier;
            characterController.Move(animationMove);
        }

        public float GetSignedTargetAngle(Vector3 targetPosition)
        {
            Vector3 targetDirection = GetFlatDirection(targetPosition);

            if (targetDirection.sqrMagnitude <= DirectionEpsilon)
            {
                return 0f;
            }

            return Vector3.SignedAngle(
                bossTransform.forward,
                targetDirection,
                Vector3.up);
        }

        private void MoveHorizontal(
            Vector3 horizontalVelocity,
            float maximumSpeed,
            float deltaTime)
        {
            Vector3 localVelocity =
                bossTransform.InverseTransformDirection(horizontalVelocity);
            float safeMaximumSpeed = Mathf.Max(0.01f, maximumSpeed);

            MoveForward = Mathf.Clamp(
                localVelocity.z / safeMaximumSpeed,
                -1f,
                1f);
            MoveSide = Mathf.Clamp(
                localVelocity.x / safeMaximumSpeed,
                -1f,
                1f);
            MoveAmount = Mathf.Clamp01(
                horizontalVelocity.magnitude / safeMaximumSpeed);

            MoveCharacter(horizontalVelocity, deltaTime);
        }

        private void MoveCharacter(
            Vector3 horizontalVelocity,
            float deltaTime)
        {
            if (!characterController.enabled)
            {
                return;
            }

            if (characterController.isGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = groundPull;
            }
            else
            {
                verticalSpeed += gravity * deltaTime;
            }

            Vector3 move = horizontalVelocity * deltaTime;
            move.y = verticalSpeed * deltaTime;
            characterController.Move(move);
        }

        private Vector3 GetFlatDirection(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - bossTransform.position;
            direction.y = 0f;
            return direction;
        }

        private void RotateTowards(
            Vector3 direction,
            float turnSpeed,
            float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                direction,
                Vector3.up);
            bossTransform.rotation = Quaternion.RotateTowards(
                bossTransform.rotation,
                targetRotation,
                turnSpeed * deltaTime);
        }

        private void ClearMoveValues()
        {
            MoveForward = 0f;
            MoveSide = 0f;
            MoveAmount = 0f;
        }
    }
}
