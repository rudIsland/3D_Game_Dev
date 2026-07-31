using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player.States.Attack
{
    // 일반 공격 5단 콤보와 달리기 공격의 재생 순서를 관리한다.
    internal sealed class PlayerAttackState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly float[] attackTotalTimes;
        private readonly float[] nextAttackTimes;
        private readonly float[] attackMoveScales;
        private readonly float runAttackTotalTime;
        private readonly float runAttackMoveScale;

        private float attackElapsedTime;
        private int comboNumber;
        private bool playRunAttack;
        private bool hasNextAttackInput;

        public bool IsFinished { get; private set; }
        public float CurrentAnimationMoveScale => playRunAttack
            ? runAttackMoveScale
            : attackMoveScales[comboNumber - 1];

        public PlayerAttackState(
            PlayerStateMachine stateMachine,
            float attack01TotalTime,
            float attack02TotalTime,
            float attack03TotalTime,
            float attack04TotalTime,
            float attack05TotalTime,
            float attack01NextTime,
            float attack02NextTime,
            float attack03NextTime,
            float attack04NextTime,
            float attack01MoveScale,
            float attack02MoveScale,
            float attack03MoveScale,
            float attack04MoveScale,
            float attack05MoveScale,
            float runAttackTotalTime,
            float runAttackMoveScale)
        {
            this.stateMachine = stateMachine;
            attackTotalTimes = new[]
            {
                attack01TotalTime,
                attack02TotalTime,
                attack03TotalTime,
                attack04TotalTime,
                attack05TotalTime
            };
            nextAttackTimes = new[]
            {
                attack01NextTime,
                attack02NextTime,
                attack03NextTime,
                attack04NextTime
            };
            attackMoveScales = new[]
            {
                attack01MoveScale,
                attack02MoveScale,
                attack03MoveScale,
                attack04MoveScale,
                attack05MoveScale
            };
            this.runAttackTotalTime = runAttackTotalTime;
            this.runAttackMoveScale = runAttackMoveScale;
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
            stateMachine.SetAttackDirection();

            if (playRunAttack)
            {
                stateMachine.PlayAttackAnimation(6);
                return;
            }

            stateMachine.PlayAttackAnimation(comboNumber);
        }

        public void Update(
            float deltaTime,
            PlayerStateInput input)
        {
            UpdateAttack(deltaTime, input.AttackPressed);
        }

        // 추가 공격 입력을 저장하고 이어질 콤보 시점에 다음 공격을 재생한다.
        public void UpdateAttack(float deltaTime, bool attackPressed)
        {
            attackElapsedTime += deltaTime;
            stateMachine.SetMoveAnimationStopped();
            stateMachine.UpdateAttackTurn(deltaTime);

            if (playRunAttack)
            {
                stateMachine.Movement.UpdateStoppedMove(deltaTime);
                IsFinished = attackElapsedTime >= runAttackTotalTime;
                return;
            }

            stateMachine.Movement.UpdateStoppedMove(deltaTime);

            if (attackPressed && comboNumber < attackTotalTimes.Length)
            {
                hasNextAttackInput = true;
            }

            if (hasNextAttackInput &&
                attackElapsedTime >= GetNextAttackTime())
            {
                comboNumber++;
                attackElapsedTime = 0f;
                hasNextAttackInput = false;
                stateMachine.SetAttackDirection();
                stateMachine.PlayAttackAnimation(comboNumber);
                return;
            }

            IsFinished = attackElapsedTime >= GetAttackTotalTime();
        }

        public void Exit()
        {
            hasNextAttackInput = false;
            stateMachine.ClearAttackDirection();
        }

        private float GetNextAttackTime()
        {
            return nextAttackTimes[comboNumber - 1];
        }

        private float GetAttackTotalTime()
        {
            return attackTotalTimes[comboNumber - 1];
        }
    }
}
