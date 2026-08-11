using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player.States.Movement
{
    // 구르기 애니메이션이 끝날 때까지 중력과 이동 Blend Tree 값을 갱신한다.
    internal sealed class PlayerRollState : IPlayerState
    {
        // PlayerMovement.controller의 PlayerRoll -> PlayerIdle Exit Time과 맞춘다.
        private const float RollCompleteNormalizedTime = 0.7f;
        private const float InvulnerableStartNormalizedTime = 0.15f;
        private const float InvulnerableEndNormalizedTime = 0.55f;

        private readonly PlayerStateMachine stateMachine; // 현재 행동 상태
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조
        private bool startsAfterAttackCancel; // 기능 사용 여부
        private bool hasAnimationStarted; // 기능 사용 여부

        public bool IsFinished { get; private set; } // 기능 사용 여부
        public bool IsInvulnerable { get; private set; }

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
            IsInvulnerable = false;
            animationController.PlayRoll(
                stateMachine.Movement.RollDirectionInput,
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
                IsInvulnerable =
                    normalizedTime >= InvulnerableStartNormalizedTime &&
                    normalizedTime <= InvulnerableEndNormalizedTime;
                IsFinished = normalizedTime >= RollCompleteNormalizedTime;
                return;
            }

            IsInvulnerable = false;
            IsFinished = hasAnimationStarted;
        }

        public void Exit()
        {
            IsFinished = false;
            IsInvulnerable = false;
        }
    }
}
