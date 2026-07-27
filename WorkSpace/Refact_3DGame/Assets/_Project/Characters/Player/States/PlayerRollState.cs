namespace rudIsland.RPG3D.Player.States
{
    // 구르기 이동 커브와 구르기 애니메이션을 함께 갱신한다.
    internal sealed class PlayerRollState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;

        public PlayerRollState(PlayerStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            stateMachine.PlayRollAnimation();
        }

        public void Update(float deltaTime)
        {
            stateMachine.Movement.UpdateRoll(deltaTime);
            stateMachine.SetMoveAnimationStopped();
        }

        public void Exit()
        {
        }
    }
}
