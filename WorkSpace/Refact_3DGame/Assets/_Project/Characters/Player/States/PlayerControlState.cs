using rudIsland.RPG3D.Player.States.Attack;
using rudIsland.RPG3D.Player.States.Block;
using rudIsland.RPG3D.Player.States.Movement;

namespace rudIsland.RPG3D.Player.States
{
    // 플레이어가 조작 가능한 동안 이동, 방어, 구르기, 공격 중 하나를 실행한다.
    internal sealed class PlayerControlState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerMoveState moveState;
        private readonly PlayerBlockState blockState;
        private readonly PlayerRollState rollState;
        private readonly PlayerAttackState attackState;

        private IPlayerState currentState;

        public bool IsBlocking => ReferenceEquals(currentState, blockState);
        public bool IsRolling => ReferenceEquals(currentState, rollState);
        public bool IsAttacking => ReferenceEquals(currentState, attackState);
        public float CurrentAttackAnimationMoveScale =>
            IsAttacking ? attackState.CurrentAnimationMoveScale : 0f;

        public PlayerControlState(
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

        public void Enter()
        {
            ChangeState(moveState);
        }

        public void Update(
            float deltaTime,
            PlayerStateInput input)
        {
            if (currentState == null)
            {
                ChangeState(moveState);
            }

            if (ReferenceEquals(currentState, rollState))
            {
                currentState.Update(deltaTime, input);

                if (!stateMachine.Movement.IsRolling)
                {
                    ChangeToMoveOrBlock(input.IsBlocking);
                }

                return;
            }

            if (ReferenceEquals(currentState, attackState))
            {
                if (input.RollPressed)
                {
                    stateMachine.Movement.StartAttackCancelRoll();
                    rollState.StartAfterAttackCancel();
                    ChangeState(rollState);
                    currentState.Update(deltaTime, input);
                    return;
                }

                currentState.Update(deltaTime, input);

                if (attackState.IsFinished)
                {
                    ChangeToMoveOrBlock(input.IsBlocking);
                }

                return;
            }

            if (input.IsBlocking)
            {
                ChangeState(blockState);
                currentState.Update(deltaTime, input);
                return;
            }

            if (input.RollPressed &&
                stateMachine.Movement.TryStartRoll())
            {
                ChangeState(rollState);
                currentState.Update(deltaTime, input);
                return;
            }

            if (input.AttackPressed &&
                stateMachine.CanStartAttack())
            {
                attackState.Prepare(stateMachine.ShouldPlayRunAttack());
                ChangeState(attackState);

                // 첫 입력은 공격 시작에만 사용하고 다음 콤보로 예약하지 않는다.
                PlayerStateInput attackStartInput = new PlayerStateInput(
                    input.RollPressed,
                    false,
                    input.IsBlocking);
                currentState.Update(deltaTime, attackStartInput);
                return;
            }

            ChangeState(moveState);
            currentState.Update(deltaTime, input);
        }

        public void Exit()
        {
            currentState?.Exit();
            currentState = null;
        }

        private void ChangeToMoveOrBlock(bool isBlocking)
        {
            ChangeState(isBlocking ? blockState : moveState);
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
