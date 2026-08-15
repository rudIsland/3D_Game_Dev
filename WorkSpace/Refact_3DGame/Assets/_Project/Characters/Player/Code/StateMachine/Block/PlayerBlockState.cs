using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.Runtime.Hit;
using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player.States.Block
{
    // 방어 입력이 유지되는 동안 방패 걷기 Blend Tree와 방어 상태를 유지한다.
    internal sealed class PlayerBlockState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine; // 현재 행동 상태
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조
        private readonly PlayerGuardHitBox guardHitBox;

        public PlayerBlockState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            PlayerGuardHitBox guardHitBox)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
            this.guardHitBox = guardHitBox;
        }

        public void Enter()
        {
            animationController.StopMove();
            stateMachine.SetAttackDirection(true);
            animationController.SetBlocking(true);
            guardHitBox?.SetGuardActive(false);
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            stateMachine.Movement.UpdateStoppedMove(deltaTime);
            animationController.UpdateBlockMove(
                stateMachine.Movement.GetLocalMoveInput(),
                deltaTime);
            guardHitBox?.SetGuardActive(
                animationController.IsPlayingBlockIdle());
        }

        public void Exit()
        {
            animationController.StopMove();
            animationController.SetBlocking(false);
            guardHitBox?.SetGuardActive(false);
            stateMachine.ClearAttackDirection();
        }
    }
}
