using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player.States.Movement
{
    // 구르기 애니메이션이 끝날 때까지 중력과 이동 Blend Tree 값을 갱신한다.
    internal sealed class PlayerRollState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerAnimationController animationController;
        private bool startsAfterAttackCancel;
        private bool hasAnimationStarted;

        public bool IsFinished { get; private set; }

        public PlayerRollState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
        }

        public void Enter()
        {
            hasAnimationStarted = false;
            IsFinished = false;
            animationController.PlayRoll(
                stateMachine.Movement.RollDirectionInput,
                stateMachine.Movement.UsesSprintRoll,
                startsAfterAttackCancel);
            startsAfterAttackCancel = false;
        }

        public void StartAfterAttackCancel()
        {
            startsAfterAttackCancel = true;
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            stateMachine.Movement.UpdateStoppedMove(deltaTime);
            animationController.StopMove();

            if (animationController.TryGetRollTime(out float normalizedTime))
            {
                hasAnimationStarted = true;
                IsFinished = normalizedTime >= 1f;
                return;
            }

            IsFinished = hasAnimationStarted;
        }

        public void Exit()
        {
            IsFinished = false;
        }
    }
}
