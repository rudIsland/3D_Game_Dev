using rudIsland.RPG3D.Player.Input;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Movement
{
    // 공통 이동과 Root Motion을 관리하고 방향 계산은 현재 이동 모드에 맡긴다.
    public sealed class PlayerMovement
    {
        private readonly Transform playerTransform; // 씬 또는 시스템 참조
        private readonly CharacterController characterController; // 씬 또는 시스템 참조
        private readonly PlayerInputReader playerInput; // 입력 또는 행동 여부
        private readonly float attackTurnSpeed;
        private readonly float walkSpeed; // 이동 속도
        private readonly float guardMoveSpeed;
        private readonly float sprintSpeed; // 이동 속도
        private readonly float moveAcceleration;
        private readonly float moveDeceleration;
        private readonly float gravity; // 내부에서 사용하는 값
        private readonly float groundPull; // 내부에서 사용하는 값
        private readonly PlayerFreeLookMovement freeLookMovement;
        private readonly PlayerTargetMovement targetMovement;

        private float verticalSpeed; // 이동 속도
        private Vector3 horizontalVelocity;
        private Vector3 rollWorldDirection;
        private Vector3 attackDirection; // 공격 관련 설정 또는 상태
        private bool hasAttackDirection; // 기능 사용 여부
        private IPlayerMovementMode currentMovementMode;

        public bool IsGrounded => characterController != null && // 기능 사용 여부
            characterController.isGrounded;
        public Vector3 Position => playerTransform.position;
        public Vector3 Forward => playerTransform.forward; // 플레이어가 바라보는 방향
        public Vector3 Right => playerTransform.right; // 플레이어의 오른쪽 방향

        public Vector2 RollDirectionInput { get; private set; } // 입력 또는 행동 여부

        public PlayerMovement(
            Transform playerTransform,
            Transform moveCamera,
            CharacterController characterController,
            PlayerInputReader playerInput,
            float freeMoveTurnSpeed,
            float targetMoveTurnSpeed,
            float attackTurnSpeed,
            float walkSpeed,
            float guardMoveSpeed,
            float sprintSpeed,
            float moveAcceleration,
            float moveDeceleration,
            float gravity,
            float groundPull)
        {
            this.playerTransform = playerTransform;
            this.characterController = characterController;
            this.playerInput = playerInput;
            this.attackTurnSpeed = Mathf.Max(0f, attackTurnSpeed);
            this.walkSpeed = Mathf.Max(0f, walkSpeed);
            this.guardMoveSpeed = Mathf.Clamp(guardMoveSpeed, 0f, this.walkSpeed);
            this.sprintSpeed = Mathf.Max(this.walkSpeed, sprintSpeed);
            this.moveAcceleration = Mathf.Max(0f, moveAcceleration);
            this.moveDeceleration = Mathf.Max(0f, moveDeceleration);
            this.gravity = gravity;
            this.groundPull = groundPull;
            freeLookMovement = new PlayerFreeLookMovement(
                playerTransform,
                moveCamera,
                Mathf.Max(0f, freeMoveTurnSpeed));
            targetMovement = new PlayerTargetMovement(
                playerTransform,
                Mathf.Max(0f, targetMoveTurnSpeed));
            currentMovementMode = freeLookMovement;
        }

        public void UpdateMove(float deltaTime, bool isSprinting)
        {
            Vector3 moveDirection = GetMoveDirection();
            currentMovementMode.UpdateFacing(moveDirection, deltaTime);
            UpdateVerticalSpeed(deltaTime);

            float moveSpeed = isSprinting ? sprintSpeed : walkSpeed;
            UpdateHorizontalVelocity(moveDirection * moveSpeed, deltaTime);
            Vector3 moveVelocity = horizontalVelocity;
            moveVelocity.y = verticalSpeed;
            ApplyMovement(moveVelocity * deltaTime);
        }

        public void UpdateGuardMove(float deltaTime)
        {
            Vector3 moveDirection = GetMoveDirection();
            UpdateVerticalSpeed(deltaTime);

            UpdateHorizontalVelocity(
                moveDirection * guardMoveSpeed,
                deltaTime);
            Vector3 moveVelocity = horizontalVelocity;
            moveVelocity.y = verticalSpeed;
            ApplyMovement(moveVelocity * deltaTime);
        }

        public void SetFreeLookMovement()
        {
            targetMovement.ClearTarget();
            currentMovementMode = freeLookMovement;
        }

        public void SetTargetMovement(Transform target)
        {
            targetMovement.SetTarget(target);
            currentMovementMode = targetMovement;
        }

        public void UpdateStoppedMove(float deltaTime)
        {
            UpdateHorizontalVelocity(Vector3.zero, deltaTime);
            UpdateVerticalSpeed(deltaTime);
            ApplyMovement(Vector3.up * (verticalSpeed * deltaTime));
        }

        public bool TryStartRoll()
        {
            if (!characterController.isGrounded)
            {
                return false;
            }

            SetRollDirection();
            return true;
        }

        // 지상에서 시작한 공격은 접지 값이 잠시 흔들려도 즉시 구르기로 취소한다.
        public void StartAttackCancelRoll()
        {
            SetRollDirection();
        }

        private void SetRollDirection()
        {
            horizontalVelocity = Vector3.zero;
            Vector2 rollInput = Vector2.ClampMagnitude(playerInput.MoveValue, 1f);

            RollDirectionInput = rollInput.sqrMagnitude < 0.01f ? Vector2.down
                : currentMovementMode.GetRollDirection(rollInput.normalized);

            rollWorldDirection =
                playerTransform.right * RollDirectionInput.x +
                playerTransform.forward * RollDirectionInput.y;
            rollWorldDirection.y = 0f;
            if (rollWorldDirection.sqrMagnitude <= 0.000001f)
            {
                rollWorldDirection = -playerTransform.forward;
                rollWorldDirection.y = 0f;
            }

            rollWorldDirection.Normalize();

            verticalSpeed = groundPull;
        }

        // 자유 시점은 카메라 방향을, 타깃 시점은 타깃 방향을 공격 방향으로 사용한다.
        public void SetAttackDirection(bool rotateImmediately)
        {
            attackDirection = currentMovementMode.GetAttackDirection();
            hasAttackDirection = true;
            if (rotateImmediately)
            {
                playerTransform.rotation = Quaternion.LookRotation(attackDirection);
            }
        }

        public void UpdateAttackDirection()
        {
            attackDirection = currentMovementMode.GetAttackDirection();
            hasAttackDirection = true;
        }

        public void UpdateAttackTurn(float deltaTime)
        {
            if (!hasAttackDirection || attackTurnSpeed <= 0f)
            {
                return;
            }

            UpdateAttackTurnTowards(attackDirection, deltaTime);
        }

        public void UpdateAttackTurnTowards(
            Vector3 wantedDirection,
            float deltaTime)
        {
            wantedDirection.y = 0f;
            if (wantedDirection.sqrMagnitude <= 0.000001f ||
                attackTurnSpeed <= 0f)
            {
                return;
            }

            Quaternion wantedRotation = Quaternion.LookRotation(
                wantedDirection);
            playerTransform.rotation = Quaternion.RotateTowards(
                playerTransform.rotation,
                wantedRotation,
                attackTurnSpeed * deltaTime);
        }

        public void ClearAttackDirection()
        {
            hasAttackDirection = false;
        }


        public void ApplyAttackMovement(float deltaDistance)
        {
            if (Mathf.Abs(deltaDistance) <= 0.000001f)
            {
                return;
            }

            Vector3 movement = playerTransform.forward * deltaDistance;
            movement.y = 0f;
            ApplyMovement(movement);
        }

        public void ApplyRollMovement(float deltaDistance)
        {
            if (Mathf.Abs(deltaDistance) <= 0.000001f)
            {
                return;
            }

            ApplyMovement(rollWorldDirection * deltaDistance);
        }

        public void ApplyHitMovement(Vector3 horizontalMovement, float deltaTime)
        {
            horizontalVelocity = Vector3.zero;
            UpdateVerticalSpeed(deltaTime);
            horizontalMovement.y = verticalSpeed * deltaTime;
            ApplyMovement(horizontalMovement);
        }

        public Vector2 GetLocalMoveInput()
        {
            Vector3 moveDirection = GetMoveDirection();
            if (moveDirection.sqrMagnitude < 0.01f)
            {
                return Vector2.zero;
            }

            Vector3 localDirection = playerTransform.InverseTransformDirection(moveDirection.normalized);
            return Vector2.ClampMagnitude(new Vector2(localDirection.x, localDirection.z), 1f);
        }

        private Vector3 GetMoveDirection()
        {
            Vector2 moveInput = Vector2.ClampMagnitude(playerInput.MoveValue, 1f);
            return currentMovementMode.GetMoveDirection(moveInput);
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

        private void UpdateHorizontalVelocity(
            Vector3 wantedVelocity,
            float deltaTime)
        {
            bool isSlowingDown =
                wantedVelocity.sqrMagnitude < horizontalVelocity.sqrMagnitude ||
                Vector3.Dot(wantedVelocity, horizontalVelocity) < 0f;
            float speedChange = isSlowingDown
                ? moveDeceleration
                : moveAcceleration;
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                wantedVelocity,
                speedChange * deltaTime);
        }

        private void ApplyMovement(Vector3 requestedMovement)
        {
            characterController.Move(requestedMovement);
        }
    }
}
