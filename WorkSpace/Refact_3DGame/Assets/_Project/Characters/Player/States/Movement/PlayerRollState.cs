using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player.States.Movement
{
    // 구르기 이동 커브와 구르기 애니메이션을 함께 갱신한다.
    internal sealed class PlayerRollState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private bool startsAfterAttackCancel;

        public PlayerRollState(PlayerStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            stateMachine.PlayRollAnimation(startsAfterAttackCancel);
            startsAfterAttackCancel = false;
        }

        public void StartAfterAttackCancel()
        {
            startsAfterAttackCancel = true;
        }

        public void Update(
            float deltaTime,
            PlayerStateInput input)
        {
            stateMachine.Movement.UpdateRoll(deltaTime);
            stateMachine.SetMoveAnimationStopped();
        }

        public void Exit()
        {
        }
    }
}
