using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player.States.Attack
{
    // 1~5단 일반 콤보와 별도 달리기 공격의 재생 순서를 관리한다.
    internal sealed class PlayerAttackState : IPlayerState
    {
        private const int LastComboNumber = 5;
        private const int RunAttackNumber = 6;
        private const float AttackCompleteNormalizedTime = 1f;

        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerAnimationController animationController;
        private readonly float attack01NextInputTime;
        private readonly float attack02NextInputTime;
        private readonly float attack03NextInputTime;
        private readonly float attack04NextInputTime;
        private readonly float comboInputBufferDuration;
        private readonly float attack01MoveScale;
        private readonly float attack02MoveScale;
        private readonly float attack03MoveScale;
        private readonly float attack04MoveScale;
        private readonly float attack05MoveScale;
        private readonly float runAttackMoveScale;
        private int comboNumber;
        private bool isRunAttack;
        private bool hasAnimationStarted;
        private bool animationEndedByEvent;
        private bool hasBufferedAttackInput;
        private float bufferedAttackInputAge;

        public bool IsFinished { get; private set; }
        public float CurrentMoveScale => GetCurrentMoveScale();

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
            this.attack01NextInputTime = attack01NextInputTime;
            this.attack02NextInputTime = attack02NextInputTime;
            this.attack03NextInputTime = attack03NextInputTime;
            this.attack04NextInputTime = attack04NextInputTime;
            this.comboInputBufferDuration = comboInputBufferDuration;
            this.attack01MoveScale = attack01MoveScale;
            this.attack02MoveScale = attack02MoveScale;
            this.attack03MoveScale = attack03MoveScale;
            this.attack04MoveScale = attack04MoveScale;
            this.attack05MoveScale = attack05MoveScale;
            this.runAttackMoveScale = runAttackMoveScale;
        }

        public void Prepare(bool startAsRunAttack)
        {
            isRunAttack = startAsRunAttack;
        }

        public void Enter()
        {
            comboNumber = 1;
            hasAnimationStarted = false;
            IsFinished = false;
            ClearBufferedAttackInput();
            PlayCurrentAttack();
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            CaptureAttackInput(deltaTime, input.AttackPressed);
            stateMachine.Movement.UpdateStoppedMove(deltaTime);
            animationController.StopMove();
            stateMachine.UpdateAttackTurn(deltaTime);

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
                return;
            }

            IsFinished = normalizedTime >= AttackCompleteNormalizedTime;
        }

        public void Exit()
        {
            ClearBufferedAttackInput();
            stateMachine.EndAttackHit();
            stateMachine.ClearAttackDirection();
        }

        private void PlayCurrentAttack()
        {
            animationEndedByEvent = false;
            stateMachine.SetAttackDirection();
            animationController.PlayAttack(isRunAttack ? RunAttackNumber : comboNumber);
        }

        internal void NotifyAnimationEnded()
        {
            int attackNumber = isRunAttack
                ? RunAttackNumber
                : comboNumber;
            if (!hasAnimationStarted ||
                !animationController.IsPlayingAttack(attackNumber))
            {
                return;
            }

            animationEndedByEvent = true;
        }

        private bool TryStartNextCombo(float normalizedTime)
        {
            if (isRunAttack || normalizedTime < GetNextInputTime() ||
                !hasBufferedAttackInput || comboNumber >= LastComboNumber)
            {
                return false;
            }

            ClearBufferedAttackInput();
            comboNumber++;
            hasAnimationStarted = false;
            stateMachine.EndAttackHit();
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

        private float GetNextInputTime()
        {
            switch (comboNumber)
            {
                case 1:
                    return attack01NextInputTime;
                case 2:
                    return attack02NextInputTime;
                case 3:
                    return attack03NextInputTime;
                case 4:
                    return attack04NextInputTime;
                default:
                    return 1f;
            }
        }

        private float GetCurrentMoveScale()
        {
            if (isRunAttack)
            {
                return runAttackMoveScale;
            }

            switch (comboNumber)
            {
                case 1:
                    return attack01MoveScale;
                case 2:
                    return attack02MoveScale;
                case 3:
                    return attack03MoveScale;
                case 4:
                    return attack04MoveScale;
                case 5:
                    return attack05MoveScale;
                default:
                    return 0f;
            }
        }
    }
}
