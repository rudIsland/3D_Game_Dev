using rudIsland.RPG3D.Player.Animations;

namespace rudIsland.RPG3D.Player.States
{
    // 피격 중에는 조작을 무시하고 Hit 애니메이션이 끝나기를 기다린다.
    internal sealed class PlayerHitState : IPlayerState
    {
        private const float ControlReturnNormalizedTime = 0.9f;

        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerAnimationController animationController;

        public PlayerHitState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
        }

        public void Enter()
        {
            Restart();
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            stateMachine.Movement.UpdateStoppedMove(deltaTime);
            animationController.StopMove();

            if (animationController.TryGetHitTime(
                    out float normalizedTime) &&
                normalizedTime >= ControlReturnNormalizedTime)
            {
                stateMachine.ChangeToControlState();
            }
        }

        public void Exit()
        {
        }

        internal void Restart()
        {
            stateMachine.EndAttackHit();
            animationController.PlayHitFromStart();
        }
    }
}
