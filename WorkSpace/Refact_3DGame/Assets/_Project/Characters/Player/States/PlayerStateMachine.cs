using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.States.Attack;
using rudIsland.RPG3D.Player.States.Block;
using rudIsland.RPG3D.Player.States.Movement;
using UnityEngine;

namespace rudIsland.RPG3D.Player.States
{
    // 최상위 상태의 생명주기를 관리하고 상태가 사용할 기능을 제공한다.
    public sealed class PlayerStateMachine
    {
        private static readonly int MoveAmountId = Animator.StringToHash("MoveAmount");
        private static readonly int RollId = Animator.StringToHash("Roll");
        private static readonly int SprintRollId = Animator.StringToHash("SprintRoll");
        private static readonly int IsBlockingId = Animator.StringToHash("IsBlocking");
        private static readonly int BlockImpactId = Animator.StringToHash("BlockImpact");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int AttackIndexId = Animator.StringToHash("AttackIndex");
        private static readonly int PlayerRollStateId =
            Animator.StringToHash("Base Layer.Movement.PlayerRoll");
        private static readonly int PlayerSprintRollStateId =
            Animator.StringToHash("Base Layer.Movement.PlayerSprintRoll");

        private readonly PlayerInputReader playerInput;
        private readonly PlayerMovement playerMovement;
        private readonly CharacterController characterController;
        private readonly Animator playerAnimator;
        private readonly float sprintSpeed;
        private readonly float animationSmoothTime;
        private readonly float runAttackMinimumSprintTime;
        private readonly float runAttackStartSpeedRatio;

        private readonly PlayerControlState controlState;

        private IPlayerState currentState;
        private Transform lockOnTarget;
        private float sprintMoveElapsedTime;
        private bool isEnabled;

        internal PlayerMovement Movement => playerMovement;

        public bool IsBlocking =>
            ReferenceEquals(currentState, controlState) &&
            controlState.IsBlocking;
        public bool IsRolling =>
            ReferenceEquals(currentState, controlState) &&
            controlState.IsRolling;
        public bool IsAttacking =>
            ReferenceEquals(currentState, controlState) &&
            controlState.IsAttacking;

        public PlayerStateMachine(
            PlayerInputReader playerInput,
            PlayerMovement playerMovement,
            CharacterController characterController,
            Animator playerAnimator,
            float sprintSpeed,
            float animationSmoothTime,
            float attack01TotalTime,
            float attack02TotalTime,
            float attack03TotalTime,
            float attack04TotalTime,
            float attack05TotalTime,
            float attack01NextTime,
            float attack02NextTime,
            float attack03NextTime,
            float attack04NextTime,
            float attack01MoveScale,
            float attack02MoveScale,
            float attack03MoveScale,
            float attack04MoveScale,
            float attack05MoveScale,
            float runAttackTotalTime,
            float runAttackMinimumSprintTime,
            float runAttackStartSpeedRatio,
            float runAttackMoveScale)
        {
            this.playerInput = playerInput;
            this.playerMovement = playerMovement;
            this.characterController = characterController;
            this.playerAnimator = playerAnimator;
            this.sprintSpeed = sprintSpeed;
            this.animationSmoothTime = animationSmoothTime;
            this.runAttackMinimumSprintTime =
                Mathf.Max(0f, runAttackMinimumSprintTime);
            this.runAttackStartSpeedRatio =
                Mathf.Clamp01(runAttackStartSpeedRatio);

            PlayerMoveState moveState = new PlayerMoveState(this);
            PlayerBlockState blockState = new PlayerBlockState(this);
            PlayerRollState rollState = new PlayerRollState(this);
            PlayerAttackState attackState = new PlayerAttackState(
                this,
                Mathf.Max(0.01f, attack01TotalTime),
                Mathf.Max(0.01f, attack02TotalTime),
                Mathf.Max(0.01f, attack03TotalTime),
                Mathf.Max(0.01f, attack04TotalTime),
                Mathf.Max(0.01f, attack05TotalTime),
                Mathf.Max(0f, attack01NextTime),
                Mathf.Max(0f, attack02NextTime),
                Mathf.Max(0f, attack03NextTime),
                Mathf.Max(0f, attack04NextTime),
                Mathf.Max(0f, attack01MoveScale),
                Mathf.Max(0f, attack02MoveScale),
                Mathf.Max(0f, attack03MoveScale),
                Mathf.Max(0f, attack04MoveScale),
                Mathf.Max(0f, attack05MoveScale),
                Mathf.Max(0.01f, runAttackTotalTime),
                Mathf.Max(0f, runAttackMoveScale));
            controlState = new PlayerControlState(
                this,
                moveState,
                blockState,
                rollState,
                attackState);
        }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            ChangeState(controlState);
        }

        public void Update(
            float deltaTime,
            bool rollPressed,
            bool attackPressed)
        {
            if (!isEnabled || currentState == null)
            {
                return;
            }

            PlayerStateInput input = new PlayerStateInput(
                rollPressed,
                attackPressed,
                playerInput.IsBlocking);
            UpdateSprintMoveTime(deltaTime);
            currentState.Update(deltaTime, input);
        }

        public void Disable()
        {
            if (!isEnabled)
            {
                return;
            }

            currentState?.Exit();
            currentState = null;
            sprintMoveElapsedTime = 0f;
            isEnabled = false;

            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetBool(IsBlockingId, false);
            playerAnimator.SetFloat(MoveAmountId, 0f);
            playerAnimator.ResetTrigger(RollId);
            playerAnimator.ResetTrigger(SprintRollId);
            playerAnimator.ResetTrigger(BlockImpactId);
            playerAnimator.ResetTrigger(AttackId);
            playerAnimator.SetInteger(AttackIndexId, 0);
        }

        public void PlayBlockImpact()
        {
            if (!IsBlocking || playerAnimator == null)
            {
                return;
            }

            playerAnimator.ResetTrigger(BlockImpactId);
            playerAnimator.SetTrigger(BlockImpactId);
        }

        public void SetLockOnTarget(Transform target)
        {
            lockOnTarget = target;
        }

        private void ChangeState(IPlayerState nextState)
        {
            if (ReferenceEquals(currentState, nextState))
            {
                return;
            }

            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }

        internal void SetBlockingAnimation(bool isBlocking)
        {
            playerAnimator?.SetBool(IsBlockingId, isBlocking);
        }

        internal void UpdateMoveAnimation(float deltaTime)
        {
            if (playerAnimator == null || characterController == null)
            {
                return;
            }

            Vector3 currentVelocity = characterController.velocity;
            currentVelocity.y = 0f;

            if (currentVelocity.sqrMagnitude < 0.0001f)
            {
                playerAnimator.SetFloat(MoveAmountId, 0f);
                return;
            }

            float moveAmount = sprintSpeed > 0f
                ? Mathf.Clamp01(currentVelocity.magnitude / sprintSpeed)
                : 0f;

            playerAnimator.SetFloat(
                MoveAmountId,
                moveAmount,
                animationSmoothTime,
                deltaTime);
        }

        internal void SetMoveAnimationStopped()
        {
            playerAnimator?.SetFloat(MoveAmountId, 0f);
        }

        internal void SetAttackDirection()
        {
            playerMovement.SetAttackDirection(lockOnTarget);
        }

        internal void UpdateAttackTurn(float deltaTime)
        {
            playerMovement.UpdateAttackTurn(
                lockOnTarget,
                deltaTime);
        }

        internal void ClearAttackDirection()
        {
            playerMovement.ClearAttackDirection();
        }

        internal void PlayRollAnimation(bool startsAfterAttackCancel)
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetBool(IsBlockingId, false);
            playerAnimator.SetFloat(MoveAmountId, 0f);
            playerAnimator.ResetTrigger(RollId);
            playerAnimator.ResetTrigger(SprintRollId);

            if (startsAfterAttackCancel)
            {
                playerAnimator.ResetTrigger(AttackId);
                playerAnimator.SetInteger(AttackIndexId, 0);
                playerAnimator.CrossFadeInFixedTime(
                    playerMovement.UsesSprintRoll
                        ? PlayerSprintRollStateId
                        : PlayerRollStateId,
                    0.08f);
                return;
            }

            playerAnimator.SetTrigger(
                playerMovement.UsesSprintRoll
                    ? SprintRollId
                    : RollId);
        }

        internal void ApplyAttackAnimationMove(
            Vector3 deltaPosition)
        {
            if (!IsAttacking)
            {
                return;
            }

            float moveScale =
                controlState.CurrentAttackAnimationMoveScale;
            if (moveScale <= 0f)
            {
                return;
            }

            playerMovement.ApplyAttackAnimationMove(deltaPosition, moveScale);
        }

        internal void PlayAttackAnimation(int attackNumber)
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetBool(IsBlockingId, false);
            playerAnimator.SetFloat(MoveAmountId, 0f);
            playerAnimator.ResetTrigger(AttackId);
            playerAnimator.SetInteger(AttackIndexId, attackNumber);
            playerAnimator.SetTrigger(AttackId);
        }

        internal bool CanStartAttack()
        {
            return characterController != null &&
                characterController.isGrounded;
        }

        internal bool ShouldPlayRunAttack()
        {
            if (!playerInput.IsSprinting ||
                sprintMoveElapsedTime < runAttackMinimumSprintTime ||
                characterController == null)
            {
                return false;
            }

            Vector3 currentVelocity = characterController.velocity;
            currentVelocity.y = 0f;

            float currentSpeedRatio = sprintSpeed > 0.01f
                ? currentVelocity.magnitude / sprintSpeed
                : 0f;

            return currentSpeedRatio >= runAttackStartSpeedRatio;
        }

        private void UpdateSprintMoveTime(float deltaTime)
        {
            if (!playerInput.IsSprinting ||
                playerInput.MoveValue.sqrMagnitude < 0.01f ||
                controlState.IsAttacking ||
                controlState.IsRolling ||
                controlState.IsBlocking)
            {
                sprintMoveElapsedTime = 0f;
                return;
            }

            sprintMoveElapsedTime += deltaTime;
        }
    }
}
