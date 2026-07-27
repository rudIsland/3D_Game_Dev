namespace rudIsland.RPG3D.Player.States
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
            stateMachine.Movement.StartBlock();
            stateMachine.SetMoveAnimationStopped();
            stateMachine.SetBlockingAnimation(true);
        }

        public void Update(float deltaTime)
        {
            stateMachine.Movement.UpdateBlock(deltaTime);
            stateMachine.SetMoveAnimationStopped();
        }

        public void Exit()
        {
            stateMachine.SetBlockingAnimation(false);
        }
    }
}
