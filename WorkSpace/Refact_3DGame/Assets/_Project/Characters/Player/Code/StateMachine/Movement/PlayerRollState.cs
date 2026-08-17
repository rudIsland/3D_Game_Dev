using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.States;
using UnityEngine;

namespace rudIsland.RPG3D.Player.States.Movement
{
    // 구르기 애니메이션이 끝날 때까지 중력과 이동 Blend Tree 값을 갱신한다.
    internal sealed class PlayerRollState : IPlayerState
    {
        // PlayerMovement.controller의 PlayerRoll -> PlayerIdle Exit Time과 맞춘다.
        private const float RollCompleteNormalizedTime = 0.7f;

        private readonly PlayerStateMachine stateMachine; // 현재 행동 상태
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조
        private readonly float movementDistance;
        private readonly float sprintMovementDistance;
        private readonly AnimationCurve movementCurve;
        private readonly PlayerActionMovementCurve movementProgress;
        private bool startsAfterAttackCancel; // 기능 사용 여부
        private bool hasAnimationStarted; // 기능 사용 여부

        public bool IsFinished { get; private set; } // 기능 사용 여부

        public PlayerRollState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            float movementDistance,
            float sprintMovementDistance,
            AnimationCurve movementCurve)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
            this.movementDistance = Mathf.Max(0f, movementDistance);
            this.sprintMovementDistance = Mathf.Max(0f, sprintMovementDistance);
            this.movementCurve = movementCurve;
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
            movementProgress.Begin(
                selectedMovementDistance,
                movementCurve);
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
                float movementTime =
                    normalizedTime / RollCompleteNormalizedTime;
                float deltaDistance =
                    movementProgress.EvaluateDeltaDistance(movementTime);
                stateMachine.Movement.ApplyRollMovement(deltaDistance);
                IsFinished = normalizedTime >= RollCompleteNormalizedTime;
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
