using rudIsland.RPG3D.Player.Input;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Movement
{
    // 이동 입력을 방향과 속도로 바꾸고 CharacterController로 이동시킨다.
    public sealed class PlayerMovement
    {
        private readonly Transform playerTransform;
        private readonly Transform moveCamera;
        private readonly CharacterController characterController;
        private readonly PlayerInputReader playerInput;
        private readonly float walkSpeed;
        private readonly float sprintSpeed;
        private readonly float turnSpeed;
        private readonly float attackTurnSpeed;
        private readonly float moveAcceleration;
        private readonly float moveDeceleration;
        private readonly AnimationCurve rollDistanceByStartSpeed;
        private readonly AnimationCurve rollMoveProgressByTime;
        private readonly float sprintRollStartSpeedRatio;
        private readonly float normalRollTotalTime;
        private readonly float sprintRollTotalTime;
        private readonly float rollTurnTime;
        private readonly float gravity;
        private readonly float groundPull;

        private Vector3 rollDirection;
        private Quaternion rollStartRotation;
        private Quaternion rollRotation;
        private float rollElapsedTime;
        private float currentRollTotalTime;
        private float currentRollDistance;
        private float movedRollDistance;
        private Vector3 currentMoveDirection;
        private Vector3 attackDirection;
        private float currentMoveSpeed;
        private float verticalSpeed;
        private bool hasAttackDirection;

        public bool IsRolling { get; private set; }
        public float RollStartSpeedRatio { get; private set; }
        public bool UsesSprintRoll { get; private set; }

        public PlayerMovement(
            Transform playerTransform,
            Transform moveCamera,
            CharacterController characterController,
            PlayerInputReader playerInput,
            float walkSpeed,
            float sprintSpeed,
            float turnSpeed,
            float attackTurnSpeed,
            float moveAcceleration,
            float moveDeceleration,
            AnimationCurve rollDistanceByStartSpeed,
            AnimationCurve rollMoveProgressByTime,
            float sprintRollStartSpeedRatio,
            float normalRollTotalTime,
            float sprintRollTotalTime,
            float rollTurnTime,
            float gravity,
            float groundPull)
        {
            this.playerTransform = playerTransform;
            this.moveCamera = moveCamera;
            this.characterController = characterController;
            this.playerInput = playerInput;
            this.walkSpeed = walkSpeed;
            this.sprintSpeed = sprintSpeed;
            this.turnSpeed = turnSpeed;
            this.attackTurnSpeed = Mathf.Max(0f, attackTurnSpeed);
            this.moveAcceleration = Mathf.Max(0.01f, moveAcceleration);
            this.moveDeceleration = Mathf.Max(0.01f, moveDeceleration);
            this.rollDistanceByStartSpeed = rollDistanceByStartSpeed;
            this.rollMoveProgressByTime = rollMoveProgressByTime;
            this.sprintRollStartSpeedRatio =
                Mathf.Clamp01(sprintRollStartSpeedRatio);
            this.normalRollTotalTime =
                Mathf.Max(0.01f, normalRollTotalTime);
            this.sprintRollTotalTime =
                Mathf.Max(0.01f, sprintRollTotalTime);
            this.rollTurnTime = Mathf.Max(0.01f, rollTurnTime);
            this.gravity = gravity;
            this.groundPull = groundPull;
        }

        // 구르기, 방어, 일반 이동 순서로 현재 상태를 처리한다.
        public void UpdateMove(float deltaTime)
        {

            Vector3 wantedMoveDirection = GetMoveDirection();

            UpdateMoveSpeed(wantedMoveDirection, deltaTime);
            RotateToDirection(wantedMoveDirection, deltaTime);
            UpdateVerticalSpeed(deltaTime);
            MovePlayer(deltaTime);
        }
        // 공격이나 방어를 시작할 때 남은 수평 이동 속도를 지운다.
        public void StopHorizontalMove()
        {
            currentMoveSpeed = 0f;
        }

        // 수평 이동을 멈춘 동안에도 중력과 지면 접지를 갱신한다.
        public void UpdateStoppedMove(float deltaTime)
        {
            UpdateVerticalSpeed(deltaTime);
            MovePlayer(deltaTime);
        }

        public void SetAttackDirection(Transform lockOnTarget)
        {
            if (TryGetLockOnDirection(
                    lockOnTarget,
                    out Vector3 lockOnDirection))
            {
                attackDirection = lockOnDirection;
                hasAttackDirection = true;
                return;
            }

            Vector3 wantedMoveDirection = GetMoveDirection();
            attackDirection =
                wantedMoveDirection.sqrMagnitude > 0.01f
                    ? wantedMoveDirection.normalized
                    : playerTransform.forward;
            attackDirection.y = 0f;
            hasAttackDirection =
                attackDirection.sqrMagnitude > 0.01f;
        }

        public void UpdateAttackTurn(
            Transform lockOnTarget,
            float deltaTime)
        {
            if (TryGetLockOnDirection(
                    lockOnTarget,
                    out Vector3 lockOnDirection))
            {
                attackDirection = lockOnDirection;
                hasAttackDirection = true;
            }

            if (!hasAttackDirection)
            {
                return;
            }

            RotateAttackDirection(
                attackDirection,
                deltaTime);
        }

        public void ClearAttackDirection()
        {
            hasAttackDirection = false;
        }

        // 공격 애니메이션의 수평 이동을 단계별 배율로 캐릭터 루트에 적용한다.
        public void ApplyAttackAnimationMove(Vector3 deltaPosition, float moveScale)
        {
            deltaPosition.y = 0f;
            characterController.Move(deltaPosition * Mathf.Max(0f, moveScale));
        }

        // 구르기 가능 여부를 확인하고 시작 방향과 거리를 저장한다.
        public bool TryStartRoll()
        {
            if (IsRolling ||
                playerInput.IsBlocking ||
                !characterController.isGrounded)
            {
                return false;
            }

            StartRoll();
            return true;
        }

        // 지상 공격 중 구르기 입력은 접지 값을 다시 검사하지 않고 즉시 공격을 취소한다.
        public void StartAttackCancelRoll()
        {
            StartRoll();
        }

        private void StartRoll()
        {
            Vector3 wantedDirection = GetMoveDirection();
            rollDirection = wantedDirection.sqrMagnitude > 0.01f
                ? wantedDirection.normalized
                : playerTransform.forward;

            rollStartRotation = playerTransform.rotation;
            rollRotation = Quaternion.LookRotation(rollDirection);
            // 롤 직전 속도로 총 거리와 롤 애니메이션 종류를 정한다.
            RollStartSpeedRatio = sprintSpeed > 0.01f
                ? Mathf.Clamp01(currentMoveSpeed / sprintSpeed)
                : 0f;
            UsesSprintRoll =
                RollStartSpeedRatio >= sprintRollStartSpeedRatio;
            currentRollTotalTime = UsesSprintRoll
                ? sprintRollTotalTime
                : normalRollTotalTime;
            currentRollDistance = Mathf.Max(
                0f,
                rollDistanceByStartSpeed.Evaluate(
                    RollStartSpeedRatio));
            rollElapsedTime = 0f;
            movedRollDistance = 0f;
            currentMoveSpeed = 0f;
            verticalSpeed = groundPull;
            IsRolling = true;
        }

        // 입력 방향을 카메라가 바라보는 방향 기준으로 바꾼다.
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

            Vector3 moveDirection =
                cameraForward * moveInput.y +
                cameraRight * moveInput.x;

            return moveDirection.normalized * moveInput.magnitude;
        }

        // 걷기·달리기 목표 속도까지 자연스럽게 가속하거나 감속한다.
        private void UpdateMoveSpeed(Vector3 wantedMoveDirection, float deltaTime)
        {
            float wantedMoveSpeed = 0f;

            if (wantedMoveDirection.sqrMagnitude > 0.01f)
            {
                currentMoveDirection = wantedMoveDirection.normalized;

                float maxMoveSpeed = playerInput.IsSprinting
                    ? sprintSpeed
                    : walkSpeed;

                wantedMoveSpeed =
                    maxMoveSpeed *
                    Mathf.Clamp01(wantedMoveDirection.magnitude);
            }

            float speedChange = wantedMoveSpeed > currentMoveSpeed
                ? moveAcceleration
                : moveDeceleration;

            currentMoveSpeed = Mathf.MoveTowards(
                currentMoveSpeed,
                wantedMoveSpeed,
                speedChange * deltaTime);
        }

        // 롤 커브에 맞춰 이번 프레임의 회전과 이동을 처리한다.
        public void UpdateRoll(float deltaTime)
        {
            rollElapsedTime = Mathf.Min(
                rollElapsedTime + deltaTime,
                currentRollTotalTime);

            UpdateVerticalSpeed(deltaTime);
            UpdateRollRotation();

            // 시간 커브로 몸을 구르는 구간에 이동을 집중한다.
            float rollTimeRatio = Mathf.Clamp01(
                rollElapsedTime / currentRollTotalTime);
            float moveProgress = Mathf.Clamp01(
                rollMoveProgressByTime.Evaluate(rollTimeRatio));

            // 이전 프레임 이후 새로 늘어난 거리만 이동한다.
            float wantedDistance = Mathf.Max(
                movedRollDistance,
                currentRollDistance * moveProgress);
            float moveDistance = wantedDistance - movedRollDistance;
            movedRollDistance = wantedDistance;

            Vector3 frameMove = rollDirection * moveDistance;
            frameMove.y = verticalSpeed * deltaTime;
            characterController.Move(frameMove);

            if (rollElapsedTime >= currentRollTotalTime)
            {
                IsRolling = false;
            }
        }

        // 롤 초반에 캐릭터를 구르는 방향으로 빠르게 돌린다.
        private void UpdateRollRotation()
        {
            float turnProgress = Mathf.Clamp01(
                rollElapsedTime / rollTurnTime);
            float smoothTurnProgress = Mathf.SmoothStep(
                0f,
                1f,
                turnProgress);

            playerTransform.rotation = Quaternion.Slerp(
                rollStartRotation,
                rollRotation,
                smoothTurnProgress);
        }

        // 일반 이동 중 캐릭터를 이동 방향으로 회전시킨다.
        private void RotateToDirection(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            Quaternion wantedRotation = Quaternion.LookRotation(direction);
            playerTransform.rotation = Quaternion.RotateTowards(
                playerTransform.rotation,
                wantedRotation,
                turnSpeed * deltaTime);
        }

        private bool TryGetLockOnDirection(
            Transform lockOnTarget,
            out Vector3 direction)
        {
            direction = Vector3.zero;

            if (lockOnTarget == null)
            {
                return false;
            }

            direction =
                lockOnTarget.position - playerTransform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector3.zero;
                return false;
            }

            direction.Normalize();
            return true;
        }

        private void RotateAttackDirection(
            Vector3 direction,
            float deltaTime)
        {
            if (direction.sqrMagnitude < 0.01f ||
                attackTurnSpeed <= 0f)
            {
                return;
            }

            Quaternion wantedRotation =
                Quaternion.LookRotation(direction);
            playerTransform.rotation = Quaternion.RotateTowards(
                playerTransform.rotation,
                wantedRotation,
                attackTurnSpeed * deltaTime);
        }

        // 지면에서는 아래로 붙이고 공중에서는 중력을 누적한다.
        private void UpdateVerticalSpeed(float deltaTime)
        {
            if (characterController.isGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = groundPull;
                return;
            }

            verticalSpeed += gravity * deltaTime;
        }

        // 수평 이동과 수직 속도를 합쳐 충돌 이동을 실행한다.
        private void MovePlayer(float deltaTime)
        {
            Vector3 moveVelocity =
                currentMoveDirection * currentMoveSpeed;
            moveVelocity.y = verticalSpeed;

            characterController.Move(moveVelocity * deltaTime);
        }
    }
}
