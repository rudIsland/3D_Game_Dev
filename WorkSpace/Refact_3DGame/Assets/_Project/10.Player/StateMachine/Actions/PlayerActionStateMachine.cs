namespace Player
{
    // 이동, 방어, 구르기와 공격 사이의 행동 전환을 한곳에서 관리한다.
    internal sealed class PlayerActionStateMachine
    {
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
        public bool IsGuardReady =>
            IsBlocking && blockState.IsGuardReady;

        /// <summary>전환할 행동들과 입력 예약 시간을 전달받는다.</summary>
        public PlayerActionStateMachine(
            PlayerMoveState moveState,
            PlayerBlockState blockState,
            PlayerRollState rollState,
            PlayerAttackState attackState,
            float inputBufferDuration)
        {
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

        /// <summary>예약 입력을 먼저 갱신하고 기존 우선순위대로 행동을 진행·전환한다.</summary>
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
                    if (rollState.TryStartAttackCancelRoll())
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
                    if (rollState.TryStartRoll())
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

        // 이동 상태에서만 예약 입력을 소비한다. 준비 실패 시에도 입력을 되돌리지 않는다.
        private void TryStartReadyAction(float deltaTime, PlayerStateInput input)
        {
            if (!ReferenceEquals(currentState, moveState))
            {
                return;
            }

            if (inputBuffer.TryTake(PlayerBufferedAction.Roll))
            {
                if (rollState.TryStartRoll())
                {
                    ChangeState(rollState);
                    currentState.Update(deltaTime, input);
                }

                return;
            }

            if (inputBuffer.TryTake(PlayerBufferedAction.Attack))
            {
                if (attackState.TryPrepareAttack())
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
