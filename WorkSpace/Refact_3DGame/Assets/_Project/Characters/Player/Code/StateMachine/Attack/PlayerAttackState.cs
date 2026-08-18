using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.States;
using rudIsland.RPG3D.Player.Movement;

namespace rudIsland.RPG3D.Player.States.Attack
{
    // 현재 공격 데이터와 공격·구르기 입력을 관리하는 공격 상태다.
    internal sealed class PlayerAttackState : IPlayerState
    {
        private const int LastComboNumber = 5;
        private const float AttackCompleteNormalizedTime = 1f;

        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerAnimationController animationController;
        private readonly PlayerAttackData[] attackData;
        private readonly PlayerActionMovementCurve movementCurve;

        private PlayerAttackData currentAttackData;
        private bool isRunAttack;
        private bool hasAnimationStarted;
        private bool animationEndedByEvent;
        private bool hasAttackHitEnded;
        private bool hasAttackTime;
        private float currentNormalizedTime;

        public bool IsFinished { get; private set; }
        public float AttackDamage => 
            currentAttackData != null ? currentAttackData.Damage : 0f;
        public float AttackStaggerDamage =>
            currentAttackData != null
                ? currentAttackData.StaggerDamage
                : 0f;
        public float AttackPushDistance =>
            currentAttackData != null
                ? currentAttackData.PushDistance
                : 0f;
        public float AttackHitStopDuration =>
            currentAttackData != null
                ? currentAttackData.HitStopDuration
                : 0f;
        public PlayerAttackData CurrentAttackData => currentAttackData;
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
                hasAttackHitEnded);

        public PlayerAttackState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            PlayerAttackData[] attackData)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
            this.attackData = attackData;
            movementCurve = new PlayerActionMovementCurve();

            ValidateAttackData(attackData);
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
                stateMachine.UpdateAttackDirection();
                stateMachine.UpdateAttackTurn(deltaTime);
            }

            float deltaDistance = movementCurve.EvaluateDeltaDistance(normalizedTime);
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
            stateMachine.SetAttackDirection(false);
            movementCurve.Begin(currentAttackData.MoveDistance, currentAttackData.MovementCurve);
            animationController.PlayAttack(currentAttackData.AttackNumber);
        }

        private static void ValidateAttackData(PlayerAttackData[] attackData)
        {
            if (attackData == null || attackData.Length != LastComboNumber + 1)
            {
                throw new System.ArgumentException("PlayerAttackData는 공격 1~6까지 6개가 필요합니다.", nameof(attackData));
            }

            for (int index = 0; index < attackData.Length; index++)
            {
                if (attackData[index] == null ||
                    attackData[index].AttackNumber != index + 1)
                {
                    throw new System.ArgumentException($"PlayerAttackData[{index}]의 공격 번호가 올바르지 않습니다.", nameof(attackData));
                }
            }
        }
    }
}
