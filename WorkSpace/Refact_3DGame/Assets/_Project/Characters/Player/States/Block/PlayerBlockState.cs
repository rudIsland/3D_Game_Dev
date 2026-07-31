using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player.States.Block
{
    // 방어 중 수평 이동을 멈추고 방어 애니메이션을 유지한다.
    internal sealed class PlayerBlockState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;

        public PlayerBlockState(PlayerStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            stateMachine.Movement.StopHorizontalMove();
            stateMachine.SetMoveAnimationStopped();
            stateMachine.SetBlockingAnimation(true);
        }

        public void Update(
            float deltaTime,
            PlayerStateInput input)
        {
            stateMachine.Movement.UpdateStoppedMove(deltaTime);
            stateMachine.SetMoveAnimationStopped();
        }

        public void Exit()
        {
            stateMachine.SetBlockingAnimation(false);
        }
    }
}
