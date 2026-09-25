using UnityEngine;

namespace Player
{
    // 공통 이동과 Root Motion을 관리하고 방향 계산은 현재 이동 모드에 맡긴다.
    public sealed class PlayerMovement
    {
        private readonly Transform playerTransform; // 씬 또는 시스템 참조
        private readonly CharacterController characterController; // 씬 또는 시스템 참조
        private readonly PlayerInputReader playerInput; // 입력 또는 행동 여부
        private readonly PlayerMovementRuntimeConfig settings;
        private readonly PlayerFreeLookMovement freeLookMovement;
        private readonly PlayerTargetMovement targetMovement;

        private float verticalSpeed; // 이동 속도
        private Vector3 horizontalVelocity;
        private Vector3 actualMoveVelocity;
        private Quaternion guardStartRotation;
        private Quaternion guardEndRotation;
        private Vector3 rollWorldDirection;
        private Vector3 attackDirection; // 공격 관련 설정 또는 상태
        private bool hasAttackDirection; // 기능 사용 여부
        private IPlayerMovementMode currentMovementMode;

        public bool IsGrounded => characterController != null && // 기능 사용 여부
            characterController.isGrounded;
        public Vector3 Position => playerTransform.position;
        public Vector3 Forward => playerTransform.forward; // 플레이어가 바라보는 방향
        public float WalkSpeed => settings.WalkSpeed;
        public float SprintSpeed => settings.SprintSpeed;
        public float GuardMoveSpeed => settings.GuardMoveSpeed;

        public Vector2 RollDirectionInput { get; private set; } // 입력 또는 행동 여부

        internal PlayerMovement(
            Transform playerTransform,
            Transform moveCamera,
            CharacterController characterController,
            PlayerInputReader playerInput,
            PlayerMovementRuntimeConfig settings)
        {
            this.playerTransform = playerTransform;
            this.characterController = characterController;
            this.playerInput = playerInput;
            this.settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            freeLookMovement = new PlayerFreeLookMovement(
                playerTransform, moveCamera, settings.FreeMoveTurnSpeed);
            targetMovement = new PlayerTargetMovement(
                playerTransform, settings.TargetMoveTurnSpeed);
            currentMovementMode = freeLookMovement;
        }

        internal void MoveToPosition(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = characterController.enabled;
            characterController.enabled = false;
            try
            {
                playerTransform.SetPositionAndRotation(position, rotation);
                horizontalVelocity = Vector3.zero;
                actualMoveVelocity = Vector3.zero;
                verticalSpeed = 0f;
                rollWorldDirection = Vector3.zero;
                RollDirectionInput = Vector2.zero;
                ClearAttackDirection();
            }
            finally
            {
                characterController.enabled = wasEnabled;
            }
        }

        public void UpdateMove(float deltaTime, bool isSprinting)
        {
            Vector3 moveDirection = GetMoveDirection();
            currentMovementMode.UpdateFacing(moveDirection, deltaTime);
            UpdateVerticalSpeed(deltaTime);

            float moveSpeed = isSprinting ? settings.SprintSpeed : settings.WalkSpeed;
            UpdateHorizontalVelocity(moveDirection * moveSpeed, deltaTime);
            Vector3 moveVelocity = horizontalVelocity;
            moveVelocity.y = verticalSpeed;
            ApplyWalkingMovement(moveVelocity, deltaTime);
        }

        public void UpdateGuardMove(float deltaTime)
        {
            Vector3 moveDirection = GetMoveDirection();
            UpdateVerticalSpeed(deltaTime);

            UpdateHorizontalVelocity(
                moveDirection * settings.GuardMoveSpeed,
                deltaTime);
            Vector3 moveVelocity = horizontalVelocity;
            moveVelocity.y = verticalSpeed;
            ApplyWalkingMovement(moveVelocity, deltaTime);
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
            horizontalVelocity = Vector3.zero;
            actualMoveVelocity = Vector3.zero;
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
            actualMoveVelocity = Vector3.zero;
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

            verticalSpeed = settings.GroundPull;
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

        public void BeginGuardTurn()
        {
            guardStartRotation = playerTransform.rotation;
            guardEndRotation = Quaternion.LookRotation(attackDirection);
        }

        public void UpdateGuardTurn(float progress)
        {
            // 방어 준비가 끝나는 순간 외형과 실제 방어 방향이 함께 일치한다.
            float amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            playerTransform.rotation = Quaternion.Slerp(guardStartRotation, guardEndRotation, amount);
        }

        public void UpdateAttackTurn(float deltaTime)
        {
            if (!hasAttackDirection || settings.AttackTurnSpeed <= 0f)
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
                settings.AttackTurnSpeed <= 0f)
            {
                return;
            }

            Quaternion wantedRotation = Quaternion.LookRotation(
                wantedDirection);
            playerTransform.rotation = Quaternion.RotateTowards(
                playerTransform.rotation,
                wantedRotation,
                settings.AttackTurnSpeed * deltaTime);
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
            actualMoveVelocity = Vector3.zero;
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
                verticalSpeed = settings.GroundPull;
                return;
            }

            verticalSpeed += settings.Gravity * deltaTime;
        }

        public Vector2 GetLocalMoveVelocity()
        {
            Vector3 localVelocity = playerTransform.InverseTransformDirection(actualMoveVelocity);
            return new Vector2(localVelocity.x, localVelocity.z);
        }

        private void ApplyWalkingMovement(Vector3 wantedVelocity, float deltaTime)
        {
            actualMoveVelocity = Vector3.zero;
            if (deltaTime <= 0f)
                return;

            Vector3 previousPosition = playerTransform.position;
            ApplyMovement(wantedVelocity * deltaTime);
            Vector3 displacement = playerTransform.position - previousPosition;
            displacement.y = 0f;
            wantedVelocity.y = 0f;
            // 충돌 때문에 이동하지 못한 속도나 접촉 보정량을 애니메이션에 누적하지 않는다.
            actualMoveVelocity = Vector3.ClampMagnitude(displacement / deltaTime, wantedVelocity.magnitude);
            horizontalVelocity = actualMoveVelocity;
        }

        private void UpdateHorizontalVelocity(
            Vector3 wantedVelocity,
            float deltaTime)
        {
            bool isChangingDirection = Vector3.Dot(wantedVelocity, horizontalVelocity) < 0f;
            bool isSlowingDown = wantedVelocity.sqrMagnitude < horizontalVelocity.sqrMagnitude;
            float speedChange = isChangingDirection
                ? settings.DirectionChangeAcceleration
                : isSlowingDown ? settings.MoveDeceleration : settings.MoveAcceleration;
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                wantedVelocity,
                speedChange * Mathf.Max(0f, deltaTime));
        }

        private void ApplyMovement(Vector3 requestedMovement)
        {
            characterController.Move(requestedMovement);
        }
    }
}
