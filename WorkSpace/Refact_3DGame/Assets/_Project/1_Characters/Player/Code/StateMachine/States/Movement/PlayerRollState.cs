using Characters.Player.Animation;
using Characters.Player.Movement;
using Characters.Player.StateMachine;
using UnityEngine;

namespace Characters.Player.StateMachine.States.Movement
{
    // 구르기 애니메이션이 끝날 때까지 중력과 이동 Blend Tree 값을 갱신한다.
    internal sealed class PlayerRollState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine; // 현재 행동 상태
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조
        private readonly float movementDistance;
        private readonly float sprintMovementDistance;
        private readonly AnimationCurve movementCurve;
        private readonly float completeNormalizedTime;
        private readonly PlayerActionMovementCurve movementProgress;
        private bool startsAfterAttackCancel; // 기능 사용 여부
        private bool hasAnimationStarted; // 기능 사용 여부

        public bool IsFinished { get; private set; } // 기능 사용 여부

        public PlayerRollState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            float movementDistance,
            float sprintMovementDistance,
            AnimationCurve movementCurve,
            float completeNormalizedTime)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
            this.movementDistance = Mathf.Max(0f, movementDistance);
            this.sprintMovementDistance = Mathf.Max(0f, sprintMovementDistance);
            this.movementCurve = movementCurve;
            this.completeNormalizedTime = Mathf.Clamp(completeNormalizedTime, 0.01f, 1f);
            movementProgress = new PlayerActionMovementCurve();
        }

        public void Enter()
        {
            hasAnimationStarted = false;
            IsFinished = false;
            float selectedMovementDistance =
                stateMachine.Input.IsSprinting &&
                stateMachine.Input.MoveValue.sqrMagnitude >= 0.95f
                    ? sprintMovementDistance
                    : movementDistance;
            movementProgress.Begin(selectedMovementDistance, movementCurve);
            animationController.PlayRoll(stateMachine.Movement.RollDirectionInput, startsAfterAttackCancel);
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
                float movementTime = normalizedTime / completeNormalizedTime;
                float deltaDistance = movementProgress.EvaluateDeltaDistance(movementTime);
                stateMachine.Movement.ApplyRollMovement(deltaDistance);
                IsFinished = normalizedTime >= completeNormalizedTime;
                return;
            }

            IsFinished = hasAnimationStarted;
        }

        public void Exit()
        {
            stateMachine.EndRollInvulnerability();
            IsFinished = false;
            movementProgress.Reset();
        }
    }
}
