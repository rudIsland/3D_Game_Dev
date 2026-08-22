using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.States;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.Config;
using UnityEngine;

namespace rudIsland.RPG3D.Player.States.Attack
{
    // 현재 공격 데이터와 공격·구르기 입력을 관리하는 공격 상태다.
    internal sealed class PlayerAttackState : IPlayerState
    {
        private const int LastComboNumber = 5;
        private const float AttackCompleteNormalizedTime = 1f;
        internal const float HeavyProtectionStartNormalizedTime = 0.20f;
        internal const float HeavyProtectionEndNormalizedTime = 0.42f;

        private readonly PlayerStateMachine stateMachine;
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
        public float AttackDamage => 
            currentAttackData != null
                ? currentAttackData.Damage.HealthDamage
                : 0f;
        public float AttackStaggerDamage =>
            currentAttackData != null
                ? currentAttackData.Damage.StaggerDamage
                : 0f;
        public AttackStrength CurrentAttackStrength =>
            currentAttackData != null
                ? currentAttackData.Damage.Strength
                : AttackStrength.Light;
        public float AttackPushDistance =>
            currentAttackData != null
                ? currentAttackData.Damage.PushDistance
                : 0f;
        public float AttackHitStopDuration =>
            currentAttackData != null
                ? currentAttackData.Damage.HitStopDuration
                : 0f;
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

        public PlayerAttackState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            PlayerAttackRuntimeConfig attackConfig)
        {
            this.stateMachine = stateMachine;
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

        public void Prepare(bool startAsRunAttack)
        {
            isRunAttack = startAsRunAttack;
        }

        public float GetInitialStaminaCost(bool startAsRunAttack)
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

        public void Update(float deltaTime, PlayerStateInput input)
        {
            stateMachine.Movement.UpdateStoppedMove(deltaTime);
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
            stateMachine.Movement.ApplyAttackMovement(deltaDistance);

            IsFinished = normalizedTime >= AttackCompleteNormalizedTime;
        }

        public void Exit()
        {
            hasAttackHitEnded = false;
            hasAttackTime = false;
            stateMachine.EndAttackHit();
            stateMachine.ClearAttackDirection();
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

        internal bool TryStartNextCombo()
        {
            if (!CanStartNextCombo)
            {
                return false;
            }

            PlayerAttackData nextAttackData = attackData[
                currentAttackData.AttackNumber];
            if (!stateMachine.TryConsumeAttackStamina(
                    nextAttackData.StaminaCost))
            {
                return false;
            }

            stateMachine.EndAttackHit();
            currentAttackData = nextAttackData;
            PlayCurrentAttack();
            IsFinished = false;
            return true;
        }

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
            stateMachine.SetAttackDirection(false);
            if (stateMachine.TryGetCurrentAttackTarget(out correctionTarget))
            {
                usesTargetCorrection = true;
                targetCorrection.Begin(
                    stateMachine.Movement.Position,
                    stateMachine.Movement.Forward,
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

        private void UpdateAttackTurn(float deltaTime)
        {
            if (!usesTargetCorrection)
            {
                stateMachine.UpdateAttackDirection();
                stateMachine.UpdateAttackTurn(deltaTime);
                return;
            }

            if (stateMachine.IsAttackTargetAvailable(correctionTarget))
            {
                targetCorrection.UpdateTargetDirection(
                    stateMachine.Movement.Position,
                    correctionTarget.position);
            }

            stateMachine.Movement.UpdateAttackTurnTowards(
                targetCorrection.TurnDirection,
                deltaTime);
        }

    }
}
