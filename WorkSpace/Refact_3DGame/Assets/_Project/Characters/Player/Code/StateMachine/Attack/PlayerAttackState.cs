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
        private readonly float inputBufferDuration;
        private readonly PlayerActionMovementCurve movementCurve;

        private PlayerAttackData currentAttackData;
        private bool isRunAttack;
        private bool hasAnimationStarted;
        private bool animationEndedByEvent;
        private bool hasBufferedAttackInput;
        private bool hasBufferedRollInput;
        private bool isComboTurnWindowOpen;
        private bool canStartRoll;
        private float bufferedAttackInputAge;
        private float bufferedRollInputAge;

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

        public PlayerAttackState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            PlayerAttackData[] attackData,
            float inputBufferDuration)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
            this.attackData = attackData;
            this.inputBufferDuration = inputBufferDuration;
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
            isComboTurnWindowOpen = false;
            ClearBufferedAttackInput();
            ClearBufferedRollInput();

            currentAttackData = isRunAttack
                ? attackData[LastComboNumber]
                : attackData[0];
            PlayCurrentAttack();
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            CaptureAttackInput(deltaTime, input.AttackPressed);
            CaptureRollInput(deltaTime, input.RollPressed);
            stateMachine.Movement.UpdateStoppedMove(deltaTime);
            animationController.StopMove();

            if (isComboTurnWindowOpen)
            {
                stateMachine.UpdateAttackDirection();
                stateMachine.UpdateAttackTurn(deltaTime);
            }

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
            canStartRoll =
                normalizedTime >= currentAttackData.RollCancelStartTime;

            // 같은 프레임에는 구르기를 콤보보다 먼저 처리한다.
            if (canStartRoll && hasBufferedRollInput)
            {
                return;
            }

            float deltaDistance =
                movementCurve.EvaluateDeltaDistance(normalizedTime);
            stateMachine.Movement.ApplyAttackMovement(deltaDistance);

            if (TryStartNextCombo(normalizedTime))
            {
                IsFinished = false;
                return;
            }

            IsFinished = normalizedTime >= AttackCompleteNormalizedTime;
        }

        public void Exit()
        {
            isComboTurnWindowOpen = false;
            ClearBufferedAttackInput();
            ClearBufferedRollInput();
            stateMachine.EndAttackHit();
            stateMachine.ClearAttackDirection();
            movementCurve.Reset();
        }

        internal bool TryTakeRollRequest()
        {
            if (!canStartRoll || !hasBufferedRollInput)
            {
                return false;
            }

            ClearBufferedRollInput();
            return true;
        }

        internal void OpenComboTurnWindow()
        {
            if (isRunAttack ||
                currentAttackData == null ||
                currentAttackData.AttackNumber >= LastComboNumber)
            {
                return;
            }

            isComboTurnWindowOpen = true;
        }

        internal void NotifyAnimationEnded()
        {
            if (!hasAnimationStarted ||
                !animationController.TryGetAttackTime(
                    out float normalizedTime) ||
                normalizedTime < AttackCompleteNormalizedTime ||
                !animationController.IsPlayingAttack(
                    currentAttackData.AttackNumber))
            {
                return;
            }

            animationEndedByEvent = true;
        }

        private void PlayCurrentAttack()
        {
            hasAnimationStarted = false;
            animationEndedByEvent = false;
            isComboTurnWindowOpen = false;
            canStartRoll = false;
            stateMachine.SetAttackDirection(
                currentAttackData.AttackNumber == 1);
            movementCurve.Begin(
                currentAttackData.MoveDistance,
                currentAttackData.MovementCurve);
            animationController.PlayAttack(currentAttackData.AttackNumber);
        }

        private bool TryStartNextCombo(float normalizedTime)
        {
            if (isRunAttack ||
                currentAttackData.AttackNumber >= LastComboNumber ||
                normalizedTime < currentAttackData.NextInputTime ||
                !hasBufferedAttackInput)
            {
                return false;
            }

            PlayerAttackData nextAttackData = attackData[
                currentAttackData.AttackNumber];
            if (!stateMachine.TryConsumeAttackStamina(
                    nextAttackData.StaminaCost))
            {
                ClearBufferedAttackInput();
                return false;
            }

            ClearBufferedAttackInput();
            stateMachine.EndAttackHit();
            currentAttackData = nextAttackData;
            PlayCurrentAttack();
            return true;
        }

        private void CaptureAttackInput(float deltaTime, bool attackPressed)
        {
            if (attackPressed)
            {
                hasBufferedAttackInput = true;
                bufferedAttackInputAge = 0f;
                return;
            }

            if (!hasBufferedAttackInput)
            {
                return;
            }

            bufferedAttackInputAge += deltaTime;
            if (bufferedAttackInputAge > inputBufferDuration)
            {
                ClearBufferedAttackInput();
            }
        }

        private void ClearBufferedAttackInput()
        {
            hasBufferedAttackInput = false;
            bufferedAttackInputAge = 0f;
        }

        private void CaptureRollInput(float deltaTime, bool rollPressed)
        {
            if (rollPressed)
            {
                hasBufferedRollInput = true;
                bufferedRollInputAge = 0f;
                return;
            }

            if (!hasBufferedRollInput)
            {
                return;
            }

            bufferedRollInputAge += deltaTime;
            if (bufferedRollInputAge > inputBufferDuration)
            {
                ClearBufferedRollInput();
            }
        }

        private void ClearBufferedRollInput()
        {
            hasBufferedRollInput = false;
            bufferedRollInputAge = 0f;
        }

        private static void ValidateAttackData(PlayerAttackData[] attackData)
        {
            if (attackData == null || attackData.Length != LastComboNumber + 1)
            {
                throw new System.ArgumentException(
                    "PlayerAttackData는 공격 1~6까지 6개가 필요합니다.",
                    nameof(attackData));
            }

            for (int index = 0; index < attackData.Length; index++)
            {
                if (attackData[index] == null ||
                    attackData[index].AttackNumber != index + 1)
                {
                    throw new System.ArgumentException(
                        $"PlayerAttackData[{index}]의 공격 번호가 올바르지 않습니다.",
                        nameof(attackData));
                }
            }
        }
    }
}
