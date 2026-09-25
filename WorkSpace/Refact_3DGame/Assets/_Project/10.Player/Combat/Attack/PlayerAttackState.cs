using UnityEngine;
using Core;

namespace Player
{
    // 현재 공격 데이터와 공격·구르기 입력을 관리하는 공격 상태다.
    internal sealed class PlayerAttackState : IPlayerState
    {
        private const int LastComboNumber = 5;
        private const float AttackCompleteNormalizedTime = 1f;
        internal const float HeavyProtectionStartNormalizedTime = 0.20f;
        internal const float HeavyProtectionEndNormalizedTime = 0.42f;

        private readonly PlayerMovement movement;
        private readonly PlayerActionStamina actionStamina;
        private readonly PlayerAttackHit attackHit;
        private readonly IPlayerAttackTarget attackTarget;
        private readonly PlayerAnimationController animationController;
        private readonly PlayerAttackData[] attackData;
        private readonly float targetStopDistance;
        private readonly float maximumAddedMoveDistance;
        private readonly float maximumTurnAngle;
        private readonly float comboCloseNormalizedTime;
        private readonly PlayerActionMovementCurve movementCurve;
        private readonly AttackTargetCorrection targetCorrection;

        private PlayerAttackData currentAttackData;
        private Transform correctionTarget;
        private bool isRunAttack;
        private bool usesTargetCorrection;
        private bool hasAnimationStarted;
        private bool animationEndedByEvent;
        private bool hasAttackHitEnded;
        private bool hasAttackTime;
        private float currentNormalizedTime;

        public bool IsFinished { get; private set; }
        public PlayerAttackData CurrentAttackData => currentAttackData;
        internal bool ProtectsSmallHit =>
            currentAttackData != null &&
            IsHeavyProtectionActive(
                currentAttackData.AttackNumber,
                hasAttackTime,
                currentNormalizedTime);
        internal bool CanCancelToRoll =>
            currentAttackData != null &&
            hasAttackTime &&
            currentAttackData.CanCancelToRollAt(currentNormalizedTime);
        internal bool CanStartNextCombo =>
            !isRunAttack &&
            currentAttackData != null &&
            currentAttackData.AttackNumber < LastComboNumber &&
            hasAttackTime &&
            currentAttackData.CanStartComboAt(
                currentNormalizedTime,
                hasAttackHitEnded,
                comboCloseNormalizedTime);

        /// <summary>공격 진행에 필요한 이동·소비·판정과 대상 조회만 연결한다.</summary>
        public PlayerAttackState(
            PlayerMovement movement,
            PlayerActionStamina actionStamina,
            PlayerAttackHit attackHit,
            IPlayerAttackTarget attackTarget,
            PlayerAnimationController animationController,
            PlayerAttackRuntimeConfig attackConfig)
        {
            this.movement = movement;
            this.actionStamina = actionStamina;
            this.attackHit = attackHit;
            this.attackTarget = attackTarget;
            this.animationController = animationController;
            attackData = attackConfig.Attacks;
            targetStopDistance = attackConfig.TargetStopDistance;
            maximumAddedMoveDistance =
                attackConfig.MaximumAddedMoveDistance;
            maximumTurnAngle = attackConfig.MaximumTurnAngle;
            comboCloseNormalizedTime =
                attackConfig.ComboCloseNormalizedTime;
            movementCurve = new PlayerActionMovementCurve();
            targetCorrection = new AttackTargetCorrection();

        }

        // 접지 확인 → 직전 달리기로 공격 선택 → 비용 소비 → 준비 순서를 유지한다.
        internal bool TryPrepareAttack()
        {
            if (!movement.IsGrounded)
            {
                return false;
            }

            bool startAsRunAttack = actionStamina.ShouldStartRunAttack;
            if (!actionStamina.TryConsumeAttackStamina(GetInitialStaminaCost(startAsRunAttack)))
            {
                return false;
            }

            Prepare(startAsRunAttack);
            return true;
        }

        // 비용 소비에 성공한 공격 종류를 Enter까지 보관한다.
        private void Prepare(bool startAsRunAttack)
        {
            isRunAttack = startAsRunAttack;
        }

        // 일반 공격은 첫 공격, 달리기 공격은 여섯 번째 데이터의 비용을 사용한다.
        private float GetInitialStaminaCost(bool startAsRunAttack)
        {
            return startAsRunAttack
                ? attackData[LastComboNumber].StaminaCost
                : attackData[0].StaminaCost;
        }

        public void Enter()
        {
            IsFinished = false;
            hasAnimationStarted = false;
            animationEndedByEvent = false;
            hasAttackHitEnded = false;
            hasAttackTime = false;
            currentNormalizedTime = 0f;

            currentAttackData = isRunAttack
                ? attackData[LastComboNumber]
                : attackData[0];
            PlayCurrentAttack();
        }

        /// <summary>중력을 적용하고 기존 애니메이션 구간에 따라 회전·전진·완료를 갱신한다.</summary>
        public void Update(float deltaTime, PlayerStateInput input)
        {
            movement.UpdateStoppedMove(deltaTime);
            animationController.StopMove();

            if (animationEndedByEvent)
            {
                IsFinished = true;
                return;
            }

            if (!animationController.TryGetAttackTime(out float normalizedTime))
            {
                if (animationController.IsChangingAttackState())
                {
                    return;
                }

                IsFinished = hasAnimationStarted;
                return;
            }

            if (animationController.IsChangingAttackState())
            {
                return;
            }

            hasAnimationStarted = true;
            hasAttackTime = true;
            currentNormalizedTime = normalizedTime;
            if (currentAttackData.CanTurnAt(normalizedTime))
            {
                UpdateAttackTurn(deltaTime);
            }

            float deltaDistance = usesTargetCorrection
                ? targetCorrection.EvaluateDeltaDistance(normalizedTime)
                : movementCurve.EvaluateDeltaDistance(normalizedTime);
            movement.ApplyAttackMovement(deltaDistance);

            IsFinished = normalizedTime >= AttackCompleteNormalizedTime;
        }

        /// <summary>공격 판정과 방향을 닫은 뒤 이동 곡선·대상 보정을 정리한다.</summary>
        public void Exit()
        {
            hasAttackHitEnded = false;
            hasAttackTime = false;
            attackHit.Close();
            movement.ClearAttackDirection();
            movementCurve.Reset();
            targetCorrection.Reset();
            correctionTarget = null;
            usesTargetCorrection = false;
        }

        internal void NotifyAttackHitEnded()
        {
            hasAttackHitEnded = true;
        }

        internal void NotifyAnimationEnded(int attackNumber)
        {
            int currentAttackNumber = currentAttackData != null
                ? currentAttackData.AttackNumber
                : 0;
            if (!CanAcceptAnimationEnd(
                    attackNumber,
                    currentAttackNumber,
                    hasAnimationStarted,
                    animationController.IsPlayingAttack(attackNumber)))
            {
                return;
            }

            animationEndedByEvent = true;
        }

        internal static bool CanAcceptAnimationEnd(
            int eventAttackNumber,
            int currentAttackNumber,
            bool hasAnimationStarted,
            bool isPlayingCurrentAttack)
        {
            return hasAnimationStarted &&
                eventAttackNumber == currentAttackNumber &&
                isPlayingCurrentAttack;
        }

        internal static bool IsHeavyProtectionTime(float normalizedTime)
        {
            return normalizedTime >=
                    HeavyProtectionStartNormalizedTime &&
                normalizedTime <
                    HeavyProtectionEndNormalizedTime;
        }

        internal static bool IsHeavyProtectionActive(
            int attackNumber,
            bool hasAttackTime,
            float normalizedTime)
        {
            return attackNumber == LastComboNumber &&
                hasAttackTime &&
                IsHeavyProtectionTime(normalizedTime);
        }

        // 콤보 구간과 비용을 확인한 뒤 이전 판정을 닫고 다음 공격을 시작한다.
        internal bool TryStartNextCombo()
        {
            if (!CanStartNextCombo)
            {
                return false;
            }

            PlayerAttackData nextAttackData = attackData[
                currentAttackData.AttackNumber];
            if (!actionStamina.TryConsumeAttackStamina(
                    nextAttackData.StaminaCost))
            {
                return false;
            }

            attackHit.Close();
            currentAttackData = nextAttackData;
            PlayCurrentAttack();
            IsFinished = false;
            return true;
        }

        // 현재 공격의 진행 상태를 초기화하고 대상 보정 또는 기본 전진 곡선을 준비한다.
        private void PlayCurrentAttack()
        {
            hasAnimationStarted = false;
            animationEndedByEvent = false;
            hasAttackHitEnded = false;
            hasAttackTime = false;
            currentNormalizedTime = 0f;
            movementCurve.Reset();
            targetCorrection.Reset();
            correctionTarget = null;
            usesTargetCorrection = false;
            movement.SetAttackDirection(false);
            if (attackTarget.TryGetCurrentAttackTarget(out correctionTarget))
            {
                usesTargetCorrection = true;
                targetCorrection.Begin(
                    movement.Position,
                    movement.Forward,
                    true,
                    correctionTarget.position,
                    currentAttackData.MoveDistance,
                    targetStopDistance,
                    maximumAddedMoveDistance,
                    maximumTurnAngle,
                    currentAttackData.MovementCurve);
            }
            else
            {
                movementCurve.Begin(
                    currentAttackData.MoveDistance,
                    currentAttackData.MovementCurve);
            }

            animationController.PlayAttack(currentAttackData.AttackNumber);
        }

        // 자유 공격은 현재 시점 방향을, 대상 보정 공격은 유지 중인 대상 방향을 따른다.
        private void UpdateAttackTurn(float deltaTime)
        {
            if (!usesTargetCorrection)
            {
                movement.UpdateAttackDirection();
                movement.UpdateAttackTurn(deltaTime);
                return;
            }

            if (attackTarget.IsAttackTargetAvailable(correctionTarget))
            {
                targetCorrection.UpdateTargetDirection(
                    movement.Position,
                    correctionTarget.position);
            }

            movement.UpdateAttackTurnTowards(
                targetCorrection.TurnDirection,
                deltaTime);
        }

    }
}
