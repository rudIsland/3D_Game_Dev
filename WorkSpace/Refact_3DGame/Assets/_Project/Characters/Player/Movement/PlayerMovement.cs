using rudIsland.RPG3D.Player.Input;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Movement
{
    // 입력을 카메라 기준 이동으로 바꾸고, 구르기·방어 루트 이동을 적용한다.
    public sealed class PlayerMovement
    {
        private readonly Transform playerTransform;
        private readonly Transform moveCamera;
        private readonly CharacterController characterController;
        private readonly PlayerInputReader playerInput;
        private readonly float turnSpeed;
        private readonly float walkSpeed;
        private readonly float sprintSpeed;
        private readonly float gravity;
        private readonly float groundPull;

        private float verticalSpeed;
        private Vector3 attackDirection;
        private bool hasAttackDirection;

        public bool IsGrounded => characterController != null &&
            characterController.isGrounded;

        public bool UsesSprintRoll { get; private set; }
        public Vector2 RollDirectionInput { get; private set; }

        public PlayerMovement(
            Transform playerTransform,
            Transform moveCamera,
            CharacterController characterController,
            PlayerInputReader playerInput,
            float turnSpeed,
            float walkSpeed,
            float sprintSpeed,
            float gravity,
            float groundPull)
        {
            this.playerTransform = playerTransform;
            this.moveCamera = moveCamera;
            this.characterController = characterController;
            this.playerInput = playerInput;
            this.turnSpeed = Mathf.Max(0f, turnSpeed);
            this.walkSpeed = Mathf.Max(0f, walkSpeed);
            this.sprintSpeed = Mathf.Max(this.walkSpeed, sprintSpeed);
            this.gravity = gravity;
            this.groundPull = groundPull;
        }

        public void UpdateMove(float deltaTime)
        {
            Vector3 moveDirection = GetMoveDirection();
            RotateToDirection(moveDirection, deltaTime);
            UpdateVerticalSpeed(deltaTime);

            float moveSpeed = playerInput.IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 moveVelocity = moveDirection * moveSpeed;
            moveVelocity.y = verticalSpeed;
            characterController.Move(moveVelocity * deltaTime);
        }

        public void UpdateStoppedMove(float deltaTime)
        {
            UpdateVerticalSpeed(deltaTime);
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
            Vector3 rollDirection = GetMoveDirection();
            if (rollDirection.sqrMagnitude < 0.01f)
            {
                RollDirectionInput = Vector2.down;
            }
            else
            {
                Vector3 localDirection = playerTransform.InverseTransformDirection(
                    rollDirection.normalized);
                RollDirectionInput = new Vector2(localDirection.x, localDirection.z);
            }

            UsesSprintRoll = playerInput.IsSprinting &&
                playerInput.MoveValue.sqrMagnitude > 0.01f;
            verticalSpeed = groundPull;
        }

        // Each combo stage captures the latest camera-relative move direction.
        public void SetAttackDirection()
        {
            attackDirection = GetMoveDirection();
            if (attackDirection.sqrMagnitude < 0.01f)
            {
                attackDirection = playerTransform.forward;
            }

            attackDirection.y = 0f;
            hasAttackDirection = attackDirection.sqrMagnitude > 0.01f;
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
            characterController.Move(deltaPosition);
            playerTransform.rotation *= deltaRotation;
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
            Vector2 moveInput = Vector2.ClampMagnitude(playerInput.MoveValue, 1f);
            if (moveInput.sqrMagnitude < 0.01f)
            {
                return Vector3.zero;
            }

            Vector3 cameraForward = moveCamera.forward;
            Vector3 cameraRight = moveCamera.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            return (cameraForward * moveInput.y + cameraRight * moveInput.x)
                .normalized * moveInput.magnitude;
        }

        private void RotateToDirection(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude < 0.01f || turnSpeed <= 0f)
            {
                return;
            }

            Quaternion wantedRotation = Quaternion.LookRotation(direction);
            playerTransform.rotation = Quaternion.RotateTowards(
                playerTransform.rotation,
                wantedRotation,
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
