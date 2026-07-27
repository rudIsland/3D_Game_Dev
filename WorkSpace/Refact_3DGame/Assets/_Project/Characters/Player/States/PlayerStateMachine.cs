using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.Movement;
using UnityEngine;

namespace rudIsland.RPG3D.Player.States
{
    // 현재 행동 하나만 실행하고 이동, 방어, 구르기 전환 순서를 관리한다.
    public sealed class PlayerStateMachine
    {
        private static readonly int MoveAmountId = Animator.StringToHash("MoveAmount");
        private static readonly int RollId = Animator.StringToHash("Roll");
        private static readonly int SprintRollId = Animator.StringToHash("SprintRoll");
        private static readonly int IsBlockingId = Animator.StringToHash("IsBlocking");
        private static readonly int BlockImpactId = Animator.StringToHash("BlockImpact");

        private readonly PlayerInputReader playerInput;
        private readonly PlayerMovement playerMovement;
        private readonly CharacterController characterController;
        private readonly Animator playerAnimator;
        private readonly float sprintSpeed;
        private readonly float animationSmoothTime;
        private readonly float sprintRollStartSpeedRatio;

        private readonly PlayerMoveState moveState;
        private readonly PlayerBlockState blockState;
        private readonly PlayerRollState rollState;

        private IPlayerState currentState;
        private bool isEnabled;

        internal PlayerMovement Movement => playerMovement;

        public bool IsBlocking => ReferenceEquals(currentState, blockState);
        public bool IsRolling => ReferenceEquals(currentState, rollState);

        public PlayerStateMachine(
            PlayerInputReader playerInput,
            PlayerMovement playerMovement,
            CharacterController characterController,
            Animator playerAnimator,
            float sprintSpeed,
            float animationSmoothTime,
            float sprintRollStartSpeedRatio)
        {
            this.playerInput = playerInput;
            this.playerMovement = playerMovement;
            this.characterController = characterController;
            this.playerAnimator = playerAnimator;
            this.sprintSpeed = sprintSpeed;
            this.animationSmoothTime = animationSmoothTime;
            this.sprintRollStartSpeedRatio = sprintRollStartSpeedRatio;

            moveState = new PlayerMoveState(this);
            blockState = new PlayerBlockState(this);
            rollState = new PlayerRollState(this);
        }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            ChangeState(moveState);
        }

        public void Update(float deltaTime, bool rollPressed)
        {
            if (!isEnabled || currentState == null)
            {
                return;
            }

            if (ReferenceEquals(currentState, rollState))
            {
                currentState.Update(deltaTime);

                if (!playerMovement.IsRolling)
                {
                    if (playerInput.IsBlocking)
                    {
                        ChangeState(blockState);
                    }
                    else
                    {
                        ChangeState(moveState);
                    }
                }

                return;
            }

            if (playerInput.IsBlocking)
            {
                ChangeState(blockState);
                currentState.Update(deltaTime);
                return;
            }

            if (rollPressed && playerMovement.TryStartRoll())
            {
                ChangeState(rollState);
                currentState.Update(deltaTime);
                return;
            }

            ChangeState(moveState);
            currentState.Update(deltaTime);
        }

        public void Disable()
        {
            if (!isEnabled)
            {
                return;
            }

            currentState?.Exit();
            currentState = null;
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

        internal void PlayRollAnimation()
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetBool(IsBlockingId, false);
            playerAnimator.SetFloat(MoveAmountId, 0f);
            playerAnimator.ResetTrigger(RollId);
            playerAnimator.ResetTrigger(SprintRollId);

            bool useSprintRoll =
                playerMovement.RollStartSpeedRatio >=
                sprintRollStartSpeedRatio;

            playerAnimator.SetTrigger(
                useSprintRoll ? SprintRollId : RollId);
        }

    }
}
