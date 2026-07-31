using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player.States.Movement
{
    // 일반 이동과 이동 Blend Tree 값을 갱신한다.
    internal sealed class PlayerMoveState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;

        public PlayerMoveState(PlayerStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            stateMachine.SetBlockingAnimation(false);
        }

        public void Update(
            float deltaTime,
            PlayerStateInput input)
        {
            stateMachine.Movement.UpdateMove(deltaTime);
            stateMachine.UpdateMoveAnimation(deltaTime);
        }

        public void Exit()
        {
        }
    }
}
