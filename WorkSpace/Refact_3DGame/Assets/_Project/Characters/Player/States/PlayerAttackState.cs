namespace rudIsland.RPG3D.Player.States
{
    // 일반 공격 3단 콤보와 달리기 공격의 재생 순서를 관리한다.
    internal sealed class PlayerAttackState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly float attack01TotalTime;
        private readonly float attack02TotalTime;
        private readonly float attack03TotalTime;
        private readonly float attack01NextTime;
        private readonly float attack02NextTime;
        private readonly float runAttackTotalTime;

        private float attackElapsedTime;
        private int comboNumber;
        private bool playRunAttack;
        private bool hasNextAttackInput;

        public bool IsFinished { get; private set; }
        public bool UsesAnimationMove => playRunAttack;

        public PlayerAttackState(
            PlayerStateMachine stateMachine,
            float attack01TotalTime,
            float attack02TotalTime,
            float attack03TotalTime,
            float attack01NextTime,
            float attack02NextTime,
            float runAttackTotalTime)
        {
            this.stateMachine = stateMachine;
            this.attack01TotalTime = attack01TotalTime;
            this.attack02TotalTime = attack02TotalTime;
            this.attack03TotalTime = attack03TotalTime;
            this.attack01NextTime = attack01NextTime;
            this.attack02NextTime = attack02NextTime;
            this.runAttackTotalTime = runAttackTotalTime;
        }

        // 상태 진입 전에 일반 콤보와 달리기 공격 중 하나를 선택한다.
        public void Prepare(bool useRunAttack)
        {
            playRunAttack = useRunAttack;
        }

        public void Enter()
        {
            attackElapsedTime = 0f;
            comboNumber = 1;
            hasNextAttackInput = false;
            IsFinished = false;

            stateMachine.SetMoveAnimationStopped();
            stateMachine.SetBlockingAnimation(false);
            stateMachine.Movement.StopHorizontalMove();

            if (playRunAttack)
            {
                stateMachine.PlayAttackAnimation(4);
                return;
            }

            stateMachine.PlayAttackAnimation(comboNumber);
        }

        public void Update(float deltaTime)
        {
            UpdateAttack(deltaTime, false);
        }

        // 추가 공격 입력을 저장하고 이어질 콤보 시점에 다음 공격을 재생한다.
        public void UpdateAttack(float deltaTime, bool attackPressed)
        {
            attackElapsedTime += deltaTime;
            stateMachine.SetMoveAnimationStopped();

            if (playRunAttack)
            {
                stateMachine.Movement.UpdateStoppedMove(deltaTime);
                IsFinished = attackElapsedTime >= runAttackTotalTime;
                return;
            }

            stateMachine.Movement.UpdateStoppedMove(deltaTime);

            if (attackPressed && comboNumber < 3)
            {
                hasNextAttackInput = true;
            }

            if (hasNextAttackInput &&
                attackElapsedTime >= GetNextAttackTime())
            {
                comboNumber++;
                attackElapsedTime = 0f;
                hasNextAttackInput = false;
                stateMachine.PlayAttackAnimation(comboNumber);
                return;
            }

            IsFinished = attackElapsedTime >= GetAttackTotalTime();
        }

        public void Exit()
        {
            hasNextAttackInput = false;
        }

        private float GetNextAttackTime()
        {
            return comboNumber == 1
                ? attack01NextTime
                : attack02NextTime;
        }

        private float GetAttackTotalTime()
        {
            switch (comboNumber)
            {
                case 1:
                    return attack01TotalTime;
                case 2:
                    return attack02TotalTime;
                default:
                    return attack03TotalTime;
            }
        }
    }
}
