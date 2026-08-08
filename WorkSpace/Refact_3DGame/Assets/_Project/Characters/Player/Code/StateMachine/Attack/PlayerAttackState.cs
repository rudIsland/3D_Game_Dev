using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.States;

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

        private IAttackState currentAttackState;
        private bool isRunAttack;
        private bool hasAnimationStarted;
        private bool animationEndedByEvent;
        private bool hasBufferedAttackInput;
        private bool isComboTurnWindowOpen;
        private float bufferedAttackInputAge;

        public bool IsFinished { get; private set; }
        public float CurrentMoveScale =>
            currentAttackState != null
                ? currentAttackState.MoveScale
                : 0f;

        public PlayerAttackState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            float attack01NextInputTime,
            float attack02NextInputTime,
            float attack03NextInputTime,
            float attack04NextInputTime,
            float comboInputBufferDuration,
            float attack01MoveScale,
            float attack02MoveScale,
            float attack03MoveScale,
            float attack04MoveScale,
            float attack05MoveScale,
            float runAttackMoveScale)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
            this.comboInputBufferDuration = comboInputBufferDuration;

            // 플레이어 생성 시 공격별 상태 객체를 한 번만 만든다.
            comboAttackStates = new IAttackState[]
            {
                new PlayerComboAttack01State(
                    attack01NextInputTime,
                    attack01MoveScale),
                new PlayerComboAttack02State(
                    attack02NextInputTime,
                    attack02MoveScale),
                new PlayerComboAttack03State(
                    attack03NextInputTime,
                    attack03MoveScale),
                new PlayerComboAttack04State(
                    attack04NextInputTime,
                    attack04MoveScale),
                new PlayerComboAttack05State(attack05MoveScale)
            };
            runAttackState = new PlayerRunAttackState(runAttackMoveScale);
        }

        public void Prepare(bool startAsRunAttack)
        {
            isRunAttack = startAsRunAttack;
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

            ClearBufferedAttackInput();
            stateMachine.EndAttackHit();
            currentAttackState = comboAttackStates[
                currentAttackState.AttackNumber];
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
    }
}
