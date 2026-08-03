using rudIsland.RPG3D.Player.Animations;

namespace rudIsland.RPG3D.Player.States.Death
{
    // 사망 후에는 다른 상태로 돌아가지 않고 중력과 지면만 유지한다.
    internal sealed class PlayerDeadState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerAnimationController animationController;

        public PlayerDeadState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
        }

        public void Enter()
        {
            animationController.PlayDeath();
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            stateMachine.Movement.UpdateStoppedMove(deltaTime);
            animationController.StopMove();
        }

        public void Exit()
        {
        }
    }
}
