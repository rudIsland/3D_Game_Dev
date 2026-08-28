using Characters.Player.StateMachine.States.Attack;
using Characters.Player.StateMachine.States.Block;
using Characters.Player.StateMachine.States.Movement;

namespace Characters.Player.StateMachine.Actions
{
    // 이동, 방어, 구르기와 공격 사이의 행동 전환을 한곳에서 관리한다.
    internal sealed class PlayerActionStateMachine
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerMoveState moveState;
        private readonly PlayerBlockState blockState;
        private readonly PlayerRollState rollState;
        private readonly PlayerAttackState attackState;
        private readonly PlayerActionInputBuffer inputBuffer;

        private IPlayerState currentState;
        private bool isEnabled;

        public bool IsBlocking => ReferenceEquals(currentState, blockState);
        public bool IsRolling => ReferenceEquals(currentState, rollState);
        public bool IsAttacking => ReferenceEquals(currentState, attackState);
        public bool IsMoving => ReferenceEquals(currentState, moveState);
        public bool IsGuardReady =>
            IsBlocking && blockState.IsGuardReady;

        public PlayerActionStateMachine(
            PlayerStateMachine stateMachine,
            PlayerMoveState moveState,
            PlayerBlockState blockState,
            PlayerRollState rollState,
            PlayerAttackState attackState,
            float inputBufferDuration)
        {
            this.stateMachine = stateMachine;
            this.moveState = moveState;
            this.blockState = blockState;
            this.rollState = rollState;
            this.attackState = attackState;
            inputBuffer = new PlayerActionInputBuffer(inputBufferDuration);
        }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            inputBuffer.Clear();
            ChangeState(moveState);
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            if (!isEnabled || currentState == null)
            {
                return;
            }

            inputBuffer.Update(deltaTime, input.RollPressed, input.AttackPressed);

            if (ReferenceEquals(currentState, rollState))
            {
                currentState.Update(deltaTime, input);
                if (rollState.IsFinished)
                {
                    ChangeState(input.IsBlocking ? blockState : moveState);
                    TryStartReadyAction(deltaTime, input);
                }

                return;
            }

            if (ReferenceEquals(currentState, attackState))
            {
                currentState.Update(deltaTime, input);
                if (attackState.CanCancelToRoll && inputBuffer.TryTake(PlayerBufferedAction.Roll))
                {
                    if (stateMachine.TryStartAttackCancelRoll())
                    {
                        rollState.StartAfterAttackCancel();
                        ChangeState(rollState);
                        currentState.Update(deltaTime, input);
                    }

                    return;
                }

                if (attackState.CanStartNextCombo && inputBuffer.TryTake(PlayerBufferedAction.Attack))
                {
                    attackState.TryStartNextCombo();
                    return;
                }

                if (attackState.IsFinished)
                {
                    ChangeState(input.IsBlocking ? blockState : moveState);
                    TryStartReadyAction(deltaTime, input);
                }

                return;
            }

            if (ReferenceEquals(currentState, blockState))
            {
                if (inputBuffer.TryTake(PlayerBufferedAction.Roll))
                {
                    if (stateMachine.TryStartRoll())
                    {
                        ChangeState(rollState);
                        currentState.Update(deltaTime, input);
                    }

                    return;
                }

                currentState.Update(deltaTime, input);
                if (!input.IsBlocking)
                {
                    ChangeState(moveState);
                }

                return;
            }

            if (input.IsBlocking)
            {
                ChangeState(blockState);
                currentState.Update(deltaTime, input);
                return;
            }

            TryStartReadyAction(deltaTime, input);
        }

        public void Disable()
        {
            inputBuffer.Clear();
            if (!isEnabled)
            {
                return;
            }

            currentState?.Exit();
            currentState = null;
            isEnabled = false;
        }

        private void TryStartReadyAction(float deltaTime, PlayerStateInput input)
        {
            if (!ReferenceEquals(currentState, moveState))
            {
                return;
            }

            if (inputBuffer.TryTake(PlayerBufferedAction.Roll))
            {
                if (stateMachine.TryStartRoll())
                {
                    ChangeState(rollState);
                    currentState.Update(deltaTime, input);
                }

                return;
            }

            if (inputBuffer.TryTake(PlayerBufferedAction.Attack))
            {
                if (stateMachine.TryPrepareAttack())
                {
                    ChangeState(attackState);
                    currentState.Update(deltaTime, input);
                }

                return;
            }

            currentState.Update(deltaTime, input);
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
    }
}
