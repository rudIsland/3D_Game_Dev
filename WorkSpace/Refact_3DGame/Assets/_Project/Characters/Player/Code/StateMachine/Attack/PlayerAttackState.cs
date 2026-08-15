using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.States;
using rudIsland.RPG3D.Player.Movement;

namespace rudIsland.RPG3D.Player.States.Attack
{
    // 공격별 상태와 콤보 입력을 관리하는 공격 부모 상태다.
    internal sealed class PlayerAttackState : IPlayerState
    {
        private const int LastComboNumber = 5;
        private const float AttackCompleteNormalizedTime = 1f;

        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerAnimationController animationController;
        private readonly IAttackState[] comboAttackStates;
        private readonly IAttackState runAttackState;
        private readonly float comboInputBufferDuration;
        private readonly PlayerActionMovementCurve movementCurve;

        private IAttackState currentAttackState;
        private bool isRunAttack;
        private bool hasAnimationStarted;
        private bool animationEndedByEvent;
        private bool hasBufferedAttackInput;
        private bool isComboTurnWindowOpen;
        private float bufferedAttackInputAge;

        public bool IsFinished { get; private set; }
        public float AttackDamage => 
            currentAttackState != null ? currentAttackState.Damage : 0f;
        public float AttackStaggerDamage =>
            currentAttackState != null
                ? currentAttackState.StaggerDamage
                : 0f;
        public float AttackPushDistance =>
            currentAttackState != null
                ? currentAttackState.PushDistance
                : 0f;
        public float AttackHitStopDuration =>
            currentAttackState != null
                ? currentAttackState.HitStopDuration
                : 0f;

        public PlayerAttackState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            PlayerAttackData[] attackData,
            float comboInputBufferDuration)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
            this.comboInputBufferDuration = comboInputBufferDuration;
            movementCurve = new PlayerActionMovementCurve();

            ValidateAttackData(attackData);

            // 플레이어 생성 시 공격별 상태 객체를 한 번만 만든다.
            comboAttackStates = new IAttackState[]
            {
                new PlayerComboAttack01State(attackData[0]),
                new PlayerComboAttack02State(attackData[1]),
                new PlayerComboAttack03State(attackData[2]),
                new PlayerComboAttack04State(attackData[3]),
                new PlayerComboAttack05State(attackData[4])
            };
            runAttackState = new PlayerRunAttackState(attackData[5]);
        }

        public void Prepare(bool startAsRunAttack)
        {
            isRunAttack = startAsRunAttack;
        }

        public float GetInitialStaminaCost(bool startAsRunAttack)
        {
            return startAsRunAttack
                ? runAttackState.StaminaCost
                : comboAttackStates[0].StaminaCost;
        }

        public void Enter()
        {
            IsFinished = false;
            hasAnimationStarted = false;
            animationEndedByEvent = false;
            isComboTurnWindowOpen = false;
            ClearBufferedAttackInput();

            currentAttackState = isRunAttack
                ? runAttackState
                : comboAttackStates[0];
            PlayCurrentAttack();
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            CaptureAttackInput(deltaTime, input.AttackPressed);
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
            stateMachine.EndAttackHit();
            stateMachine.ClearAttackDirection();
            movementCurve.Reset();
        }

        internal void OpenComboTurnWindow()
        {
            if (isRunAttack ||
                currentAttackState == null ||
                currentAttackState.AttackNumber >= LastComboNumber)
            {
                return;
            }

            isComboTurnWindowOpen = true;
        }

        internal void NotifyAnimationEnded()
        {
            if (!hasAnimationStarted ||
                !animationController.IsPlayingAttack(
                    currentAttackState.AttackNumber))
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
            stateMachine.SetAttackDirection(
                currentAttackState.AttackNumber == 1);
            movementCurve.Begin(
                currentAttackState.MoveDistance,
                currentAttackState.MovementCurve);
            animationController.PlayAttack(currentAttackState.AttackNumber);
        }

        private bool TryStartNextCombo(float normalizedTime)
        {
            if (isRunAttack ||
                currentAttackState.AttackNumber >= LastComboNumber ||
                normalizedTime < currentAttackState.NextInputTime ||
                !hasBufferedAttackInput)
            {
                return false;
            }

            IAttackState nextAttackState = comboAttackStates[
                currentAttackState.AttackNumber];
            if (!stateMachine.TryConsumeAttackStamina(
                    nextAttackState.StaminaCost))
            {
                ClearBufferedAttackInput();
                return false;
            }

            ClearBufferedAttackInput();
            stateMachine.EndAttackHit();
            currentAttackState = nextAttackState;
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
            if (bufferedAttackInputAge > comboInputBufferDuration)
            {
                ClearBufferedAttackInput();
            }
        }

        private void ClearBufferedAttackInput()
        {
            hasBufferedAttackInput = false;
            bufferedAttackInputAge = 0f;
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
