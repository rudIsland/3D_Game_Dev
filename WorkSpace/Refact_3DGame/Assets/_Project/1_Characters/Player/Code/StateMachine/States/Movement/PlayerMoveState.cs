using Characters.Player.Animation;
using Characters.Player.StateMachine;

namespace Characters.Player.StateMachine.States.Movement
{
    // 일반 이동과 이동 Blend Tree 값을 갱신한다.
    internal sealed class PlayerMoveState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine; // 현재 행동 상태
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조

        public PlayerMoveState(PlayerStateMachine stateMachine, PlayerAnimationController animationController)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
        }

        public void Enter()
        {
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            bool isSprinting = stateMachine.TryConsumeSprintStamina(deltaTime);
            stateMachine.Movement.UpdateMove(deltaTime, isSprinting);
            animationController.UpdateLocomotion(
                stateMachine.Movement.GetLocalMoveInput(),
                isSprinting,
                deltaTime);
        }

        public void Exit()
        {
        }
    }
}
