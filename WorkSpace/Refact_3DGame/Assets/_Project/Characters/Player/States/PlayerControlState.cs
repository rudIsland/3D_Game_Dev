using rudIsland.RPG3D.Player.States.Attack;
using rudIsland.RPG3D.Player.States.Block;
using rudIsland.RPG3D.Player.States.Movement;

namespace rudIsland.RPG3D.Player.States
{
    // 플레이어의 이동, 방어, 구르기, 공격 애니메이션 상태를 전환한다.
    internal sealed class PlayerControlState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine; // 현재 행동 상태
        private readonly PlayerMoveState moveState; // 이동 정보
        private readonly PlayerBlockState blockState; // 현재 행동 상태
        private readonly PlayerRollState rollState; // 현재 행동 상태
        private readonly PlayerAttackState attackState; // 공격 관련 설정 또는 상태
        private IPlayerState currentState; // 현재 행동 상태

        public bool IsBlocking => ReferenceEquals(currentState, blockState); // 기능 사용 여부
        public bool IsRolling => ReferenceEquals(currentState, rollState); // 기능 사용 여부
        public bool IsAttacking => ReferenceEquals(currentState, attackState); // 기능 사용 여부

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

        public void Update(float deltaTime, PlayerStateInput input)
        {
            if (currentState == null)
            {
                ChangeState(moveState);
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

            if (input.RollPressed && stateMachine.Movement.TryStartRoll())
            {
                ChangeState(rollState);
                currentState.Update(deltaTime, input);
                return;
            }

            if (input.AttackPressed && stateMachine.CanStartAttack())
            {
                attackState.Prepare(stateMachine.ShouldStartRunAttack());
                ChangeState(attackState);
                currentState.Update(deltaTime, new PlayerStateInput(
                    false,
                    false,
                    input.IsBlocking));
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
