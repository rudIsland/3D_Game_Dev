using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player.States.Movement
{
    // 구르기 애니메이션이 끝날 때까지 중력과 이동 Blend Tree 값을 갱신한다.
    internal sealed class PlayerRollState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine; // 현재 행동 상태
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조
        private bool startsAfterAttackCancel; // 기능 사용 여부
        private bool hasAnimationStarted; // 기능 사용 여부

        public bool IsFinished { get; private set; } // 기능 사용 여부

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
