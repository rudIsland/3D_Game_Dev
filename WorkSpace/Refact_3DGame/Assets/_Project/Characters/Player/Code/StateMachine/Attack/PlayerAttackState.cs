using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player.States.Attack
{
    // 1~5단 일반 콤보와 별도 달리기 공격의 재생 순서를 관리한다.
    internal sealed class PlayerAttackState : IPlayerState
    {
        private const int LastComboNumber = 5; // 내부에서 사용하는 값
        private const int RunAttackNumber = 6; // 공격 관련 설정 또는 상태
        private const float AttackCompleteNormalizedTime = 1f; // 공격 관련 설정 또는 상태

        private readonly PlayerStateMachine stateMachine; // 현재 행동 상태
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조
        private readonly float attack01NextInputTime; // 공격 관련 설정 또는 상태
        private readonly float attack02NextInputTime; // 공격 관련 설정 또는 상태
        private readonly float attack03NextInputTime; // 공격 관련 설정 또는 상태
        private readonly float attack04NextInputTime; // 공격 관련 설정 또는 상태
        private readonly float comboInputBufferDuration; // 시간 설정
        private readonly float attack01MoveScale; // 공격 관련 설정 또는 상태
        private readonly float attack02MoveScale; // 공격 관련 설정 또는 상태
        private readonly float attack03MoveScale; // 공격 관련 설정 또는 상태
        private readonly float attack04MoveScale; // 공격 관련 설정 또는 상태
        private readonly float attack05MoveScale; // 공격 관련 설정 또는 상태
        private readonly float runAttackMoveScale; // 공격 관련 설정 또는 상태
        private int comboNumber; // 내부에서 사용하는 값
        private bool isRunAttack; // 기능 사용 여부
        private bool hasAnimationStarted; // 기능 사용 여부
        private bool animationEndedByEvent; // 기능 사용 여부
        private bool hasBufferedAttackInput; // 기능 사용 여부
        private bool isComboTurnWindowOpen; // 기능 사용 여부
        private float bufferedAttackInputAge; // 공격 관련 설정 또는 상태

        public bool IsFinished { get; private set; } // 기능 사용 여부
        public float CurrentMoveScale => GetCurrentMoveScale(); // 이동 정보

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
            isComboTurnWindowOpen = false;
            ClearBufferedAttackInput();
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

        private void PlayCurrentAttack()
        {
            animationEndedByEvent = false;
            isComboTurnWindowOpen = false;
            stateMachine.SetAttackDirection(comboNumber == 1);
            animationController.PlayAttack(isRunAttack ? RunAttackNumber : comboNumber);
        }

        internal void OpenComboTurnWindow()
        {
            if (isRunAttack || comboNumber >= LastComboNumber)
            {
                return;
            }

            isComboTurnWindowOpen = true;
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
