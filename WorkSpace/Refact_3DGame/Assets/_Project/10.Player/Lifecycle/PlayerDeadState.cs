namespace Player
{
    // 사망 후에는 다른 상태로 돌아가지 않고 중력과 지면만 유지한다.
    internal sealed class PlayerDeadState : IPlayerState
    {
        private readonly PlayerMovement movement;
        private readonly PlayerAnimationController animationController;

        /// <summary>사망 후 지면 유지와 애니메이션에 필요한 참조만 연결한다.</summary>
        public PlayerDeadState(PlayerMovement movement, PlayerAnimationController animationController)
        {
            this.movement = movement;
            this.animationController = animationController;
        }

        public void Enter()
        {
            animationController.PlayDeath();
        }

        /// <summary>입력은 사용하지 않고 중력과 정지 표시를 유지한다.</summary>
        public void Update(float deltaTime, PlayerStateInput input)
        {
            movement.UpdateStoppedMove(deltaTime);
            animationController.StopMove();
        }

        public void Exit()
        {
        }
    }
}
