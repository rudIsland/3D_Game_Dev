using UnityEngine;

namespace Player
{
    // 구르기 애니메이션이 끝날 때까지 중력과 이동 Blend Tree 값을 갱신한다.
    internal sealed class PlayerRollState : IPlayerState
    {
        private readonly PlayerMovement movement;
        private readonly PlayerInputReader playerInput;
        private readonly PlayerActionStamina actionStamina;
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조
        private readonly float movementDistance;
        private readonly float sprintMovementDistance;
        private readonly AnimationCurve movementCurve;
        private readonly float completeNormalizedTime;
        private readonly PlayerActionMovementCurve movementProgress;
        private bool startsAfterAttackCancel; // 기능 사용 여부
        private bool hasAnimationStarted; // 기능 사용 여부

        public bool IsFinished { get; private set; } // 기능 사용 여부
        internal bool IsInvulnerable { get; private set; }

        /// <summary>입력·이동·소비 규칙과 구르기 설정을 연결한다.</summary>
        public PlayerRollState(
            PlayerMovement movement,
            PlayerInputReader playerInput,
            PlayerActionStamina actionStamina,
            PlayerAnimationController animationController,
            float movementDistance,
            float sprintMovementDistance,
            AnimationCurve movementCurve,
            float completeNormalizedTime)
        {
            this.movement = movement;
            this.playerInput = playerInput;
            this.actionStamina = actionStamina;
            this.animationController = animationController;
            this.movementDistance = Mathf.Max(0f, movementDistance);
            this.sprintMovementDistance = Mathf.Max(0f, sprintMovementDistance);
            this.movementCurve = movementCurve;
            this.completeNormalizedTime = Mathf.Clamp(completeNormalizedTime, 0.01f, 1f);
            movementProgress = new PlayerActionMovementCurve();
        }

        // 일반 구르기는 비용 가능 확인 → 접지·방향 설정 → 소비 순서로 준비한다.
        internal bool TryStartRoll()
        {
            if (!actionStamina.CanConsumeRollStamina() || !movement.TryStartRoll())
            {
                return false;
            }

            return actionStamina.TryConsumeRollStamina();
        }

        // 공격 취소는 접지를 다시 검사하지 않고 비용을 먼저 소비한다.
        internal bool TryStartAttackCancelRoll()
        {
            if (!actionStamina.TryConsumeRollStamina())
            {
                return false;
            }

            movement.StartAttackCancelRoll();
            return true;
        }

        // 상위 상태머신이 현재 구르기임을 확인한 애니메이션 이벤트만 전달한다.
        internal void BeginInvulnerability()
        {
            IsInvulnerable = true;
        }

        // 애니메이션 종료·상태 종료·비활성화 모두 같은 무적 값을 해제한다.
        internal void EndInvulnerability()
        {
            IsInvulnerable = false;
        }

        /// <summary>현재 입력으로 구르기 거리를 고르고 준비한 방향의 애니메이션을 시작한다.</summary>
        public void Enter()
        {
            hasAnimationStarted = false;
            IsFinished = false;
            float selectedMovementDistance =
                playerInput.IsSprinting &&
                playerInput.MoveValue.sqrMagnitude >= 0.95f
                    ? sprintMovementDistance
                    : movementDistance;
            movementProgress.Begin(selectedMovementDistance, movementCurve);
            animationController.PlayRoll(movement.RollDirectionInput, startsAfterAttackCancel);
            startsAfterAttackCancel = false;
        }

        public void StartAfterAttackCancel()
        {
            startsAfterAttackCancel = true;
        }

        /// <summary>중력을 적용한 뒤 애니메이션 진행률에 해당하는 구르기 거리를 이동한다.</summary>
        public void Update(float deltaTime, PlayerStateInput input)
        {
            movement.UpdateStoppedMove(deltaTime);
            animationController.StopMove();

            if (animationController.TryGetRollTime(out float normalizedTime))
            {
                hasAnimationStarted = true;
                float movementTime = normalizedTime / completeNormalizedTime;
                float deltaDistance = movementProgress.EvaluateDeltaDistance(movementTime);
                movement.ApplyRollMovement(deltaDistance);
                IsFinished = normalizedTime >= completeNormalizedTime;
                return;
            }

            IsFinished = hasAnimationStarted;
        }

        /// <summary>다른 행동으로 넘어가기 전에 무적과 이동 진행률을 정리한다.</summary>
        public void Exit()
        {
            EndInvulnerability();
            IsFinished = false;
            movementProgress.Reset();
        }
    }
}
