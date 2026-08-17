using rudIsland.RPG3D.Player.States.Attack;
using rudIsland.RPG3D.Player.States.Block;
using rudIsland.RPG3D.Player.States.Movement;

namespace rudIsland.RPG3D.Player.States.Actions
{
    // 이동, 방어, 구르기와 공격 사이의 행동 전환을 한곳에서 관리한다.
    internal sealed class PlayerActionStateMachine
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerMoveState moveState;
        private readonly PlayerBlockState blockState;
        private readonly PlayerRollState rollState;
        private readonly PlayerAttackState attackState;

        private IPlayerState currentState;
        private bool isEnabled;

        public bool IsBlocking => ReferenceEquals(currentState, blockState);
        public bool IsRolling => ReferenceEquals(currentState, rollState);
        public bool IsAttacking => ReferenceEquals(currentState, attackState);
        public bool IsMoving => ReferenceEquals(currentState, moveState);

        public PlayerActionStateMachine(
            PlayerStateMachine stateMachine,
            PlayerMoveState moveState,
            PlayerBlockState blockState,
            PlayerRollState rollState,
            PlayerAttackState attackState)
        {
            this.stateMachine = stateMachine;
            this.moveState = moveState;
            this.blockState = blockState;
            this.rollState = rollState;
            this.attackState = attackState;
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

        public void Update(float deltaTime, PlayerStateInput input)
        {
            if (!isEnabled || currentState == null)
            {
                return;
            }

            if (ReferenceEquals(currentState, rollState))
            {
                currentState.Update(deltaTime, input);
                if (rollState.IsFinished)
                {
                    ChangeState(input.IsBlocking ? blockState : moveState);
                }

                return;
            }

            if (ReferenceEquals(currentState, attackState))
            {
                currentState.Update(deltaTime, input);
                if (attackState.TryTakeRollRequest() &&
                    stateMachine.TryStartAttackCancelRoll())
                {
                    rollState.StartAfterAttackCancel();
                    ChangeState(rollState);
                    currentState.Update(deltaTime, input);
                    return;
                }

                if (attackState.IsFinished)
                {
                    ChangeState(input.IsBlocking ? blockState : moveState);
                }

                return;
            }

            if (ReferenceEquals(currentState, blockState))
            {
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

            if (input.RollPressed && stateMachine.TryStartRoll())
            {
                ChangeState(rollState);
                currentState.Update(deltaTime, input);
                return;
            }

            if (input.AttackPressed && stateMachine.TryPrepareAttack())
            {
                ChangeState(attackState);
                currentState.Update(deltaTime, new PlayerStateInput(
                    false,
                    false,
                    false,
                    input.IsBlocking));
                return;
            }

            ChangeState(moveState);
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
            isEnabled = false;
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
