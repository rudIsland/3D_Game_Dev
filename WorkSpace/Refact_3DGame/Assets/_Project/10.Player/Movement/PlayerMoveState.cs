namespace Player
{
    // 일반 이동과 이동 Blend Tree 값을 갱신한다.
    internal sealed class PlayerMoveState : IPlayerState
    {
        private readonly PlayerMovement movement;
        private readonly PlayerActionStamina actionStamina;
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조

        /// <summary>이동·스태미나·애니메이션을 전달받아 일반 이동에 사용한다.</summary>
        public PlayerMoveState(
            PlayerMovement movement,
            PlayerActionStamina actionStamina,
            PlayerAnimationController animationController)
        {
            this.movement = movement;
            this.actionStamina = actionStamina;
            this.animationController = animationController;
        }

        public void Enter()
        {
        }

        /// <summary>달리기 비용을 소비하고 이동한 실제 속도를 애니메이션에 반영한다.</summary>
        public void Update(float deltaTime, PlayerStateInput input)
        {
            bool isSprinting = actionStamina.TryConsumeSprintStamina(deltaTime);
            movement.UpdateMove(deltaTime, isSprinting);
            animationController.UpdateLocomotion(
                movement.GetLocalMoveVelocity(),
                movement.WalkSpeed,
                movement.SprintSpeed,
                deltaTime);
        }

        public void Exit()
        {
        }
    }
}
