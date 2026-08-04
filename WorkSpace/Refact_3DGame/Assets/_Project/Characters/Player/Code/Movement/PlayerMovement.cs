using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Player.Input;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Movement
{
    // 현재 이동 모드의 방향 계산과 구르기·방어 루트 이동을 적용한다.
    public sealed class PlayerMovement
    {
        private readonly Transform playerTransform; // 씬 또는 시스템 참조
        private readonly Transform moveCamera; // 이동 정보
        private readonly CharacterController characterController; // 씬 또는 시스템 참조
        private readonly UnitMovementSeparation movementSeparation;
        private readonly PlayerInputReader playerInput; // 입력 또는 행동 여부
        private readonly float turnSpeed; // 이동 속도
        private readonly float walkSpeed; // 이동 속도
        private readonly float sprintSpeed; // 이동 속도
        private readonly float gravity; // 내부에서 사용하는 값
        private readonly float groundPull; // 내부에서 사용하는 값
        private readonly HitPushMovement hitPushMovement; // 피격 또는 피해 관련 값
        private readonly PlayerFreeLookMovement freeLookMovement;
        private readonly PlayerTargetMovement targetMovement;

        private float verticalSpeed; // 이동 속도
        private Vector3 attackDirection; // 공격 관련 설정 또는 상태
        private bool hasAttackDirection; // 기능 사용 여부
        private IPlayerMovementMode currentMovementMode;

        public bool IsGrounded => characterController != null && // 기능 사용 여부
            characterController.isGrounded;
        public Vector3 Forward => playerTransform.forward; // 플레이어가 바라보는 방향
        public Vector3 Right => playerTransform.right; // 플레이어의 오른쪽 방향

        public Vector2 RollDirectionInput { get; private set; } // 입력 또는 행동 여부

        public PlayerMovement(
            Transform playerTransform,
            Transform moveCamera,
            CharacterController characterController,
            UnitMovementSeparation movementSeparation,
            PlayerInputReader playerInput,
            float turnSpeed,
            float walkSpeed,
            float sprintSpeed,
            float gravity,
            float groundPull,
            float hitPushTime)
        {
            this.playerTransform = playerTransform;
            this.moveCamera = moveCamera;
            this.characterController = characterController;
            this.movementSeparation = movementSeparation;
            this.playerInput = playerInput;
            this.turnSpeed = Mathf.Max(0f, turnSpeed);
            this.walkSpeed = Mathf.Max(0f, walkSpeed);
            this.sprintSpeed = Mathf.Max(this.walkSpeed, sprintSpeed);
            this.gravity = gravity;
            this.groundPull = groundPull;
            hitPushMovement = new HitPushMovement(hitPushTime);
            freeLookMovement = new PlayerFreeLookMovement(
                playerTransform,
                moveCamera,
                this.turnSpeed);
            targetMovement = new PlayerTargetMovement(
                playerTransform,
                moveCamera,
                this.turnSpeed);
            currentMovementMode = freeLookMovement;
        }

        public void UpdateMove(float deltaTime)
        {
            Vector3 moveDirection = GetMoveDirection();
            currentMovementMode.UpdateFacing(moveDirection, deltaTime);
            UpdateVerticalSpeed(deltaTime);

            float moveSpeed = playerInput.IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 moveVelocity = moveDirection * moveSpeed;
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
            UpdateVerticalSpeed(deltaTime);
        }

        public void StartHitPush(
            Vector3 hitDirection,
            float pushDistance)
        {
            hitPushMovement.StartPush(hitDirection, pushDistance);
        }

        public void UpdateHitPush(float deltaTime)
        {
            UpdateVerticalSpeed(deltaTime);
            Vector3 hitMove = hitPushMovement.GetNextMove(deltaTime);
            hitMove.y = verticalSpeed * deltaTime;
            ApplyMovement(hitMove);
        }

        public void StopHitPush()
        {
            hitPushMovement.StopPush();
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
            Vector2 rollInput =
                Vector2.ClampMagnitude(playerInput.MoveValue, 1f);
            RollDirectionInput = rollInput.sqrMagnitude < 0.01f
                ? Vector2.down
                : rollInput.normalized;

            if (!ReferenceEquals(currentMovementMode, targetMovement))
            {
                Vector3 cameraForward = moveCamera.forward;
                cameraForward.y = 0f;
                if (cameraForward.sqrMagnitude > 0.01f)
                {
                    playerTransform.rotation =
                        Quaternion.LookRotation(cameraForward);
                }
            }

            verticalSpeed = groundPull;
        }

        // 각 콤보 단계는 카메라가 바라보는 수평 방향을 공격 방향으로 사용한다.
        public void SetAttackDirection(bool rotateImmediately)
        {
            attackDirection = GetAttackDirection();
            hasAttackDirection = true;
            if (rotateImmediately)
            {
                playerTransform.rotation =
                    Quaternion.LookRotation(attackDirection);
            }
        }

        public void UpdateAttackDirection()
        {
            attackDirection = GetAttackDirection();
            hasAttackDirection = true;
        }

        public void UpdateAttackTurn(float deltaTime)
        {
            if (!hasAttackDirection || turnSpeed <= 0f)
            {
                return;
            }

            Quaternion wantedRotation = Quaternion.LookRotation(attackDirection);
            playerTransform.rotation = Quaternion.RotateTowards(
                playerTransform.rotation,
                wantedRotation,
                turnSpeed * deltaTime);
        }

        public void ClearAttackDirection()
        {
            hasAttackDirection = false;
        }

        public void ApplyRootMotion(
            Vector3 deltaPosition,
            Quaternion deltaRotation,
            float horizontalMoveScale)
        {
            deltaPosition.x *= horizontalMoveScale;
            deltaPosition.z *= horizontalMoveScale;
            deltaPosition.y = verticalSpeed * Time.deltaTime;
            ApplyMovement(deltaPosition);
            playerTransform.rotation *= deltaRotation;
            currentMovementMode.UpdateFacing(Vector3.zero, Time.deltaTime);
        }

        public Vector2 GetLocalMoveInput()
        {
            Vector3 moveDirection = GetMoveDirection();
            if (moveDirection.sqrMagnitude < 0.01f)
            {
                return Vector2.zero;
            }

            Vector3 localDirection = playerTransform.InverseTransformDirection(
                moveDirection.normalized);
            return Vector2.ClampMagnitude(
                new Vector2(localDirection.x, localDirection.z),
                1f);
        }

        private Vector3 GetMoveDirection()
        {
            Vector2 moveInput =
                Vector2.ClampMagnitude(playerInput.MoveValue, 1f);
            return currentMovementMode.GetMoveDirection(moveInput);
        }

        private Vector3 GetAttackDirection()
        {
            Vector3 cameraForward = moveCamera.forward;
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude < 0.01f)
            {
                return playerTransform.forward;
            }

            return cameraForward.normalized;
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

        private void ApplyMovement(Vector3 requestedMovement)
        {
            Vector3 limitedMovement =
                movementSeparation.LimitApproachMovement(
                    requestedMovement);
            characterController.Move(limitedMovement);
        }
    }
}
