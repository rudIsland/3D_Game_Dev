namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    // 공포와 분노 연출을 거쳐 2페이즈로 바꾼다.
    internal sealed class DemonSwordsmanPhaseChangeState :
        IDemonSwordsmanState
    {
        private const float LocomotionFadeTime = 0.15f; // 시간 설정
        private readonly DemonSwordsmanStateMachine stateMachine; // 현재 행동 상태
        private bool rageStarted; // 기능 사용 여부
        private bool swordStored; // 기능 사용 여부
        private bool phaseCompleted; // 기능 사용 여부

        public string Name => // 표시 이름
            nameof(DemonSwordsmanActionState.PhaseChange);

        internal DemonSwordsmanPhaseChangeState(
            DemonSwordsmanStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            rageStarted = false;
            swordStored = false;
            phaseCompleted = false;
            stateMachine.Movement.SetAttackRootMove(false, 1f);
            stateMachine.Animation.SetAnimationSpeed(1f);
            stateMachine.BeginPhaseChange();
            stateMachine.Animation.ShowStyle(
                DemonSwordsmanStyle.Sword);
            stateMachine.Animation.PlayPhaseFear();
        }

        public void Update(float deltaTime)
        {
            stateMachine.Movement.Stop(deltaTime);

            if (stateMachine.Target.HasTarget)
            {
                stateMachine.Movement.TurnTo(
                    stateMachine.Target.Position,
                    stateMachine.Settings.PhaseOneTurnSpeed,
                    deltaTime);
            }

            stateMachine.UpdateMoveAnimation(deltaTime);

            if (!rageStarted &&
                stateMachine.StateTime >=
                    stateMachine.Settings.PhaseFearTime)
            {
                rageStarted = true;
                stateMachine.Animation.PlayPhaseRage();
            }

            float swordStoreTime =
                stateMachine.Settings.PhaseFearTime +
                stateMachine.Settings.PhaseRageTime *
                stateMachine.Settings.PhaseSwordStoreNormalizedTime;

            if (!swordStored &&
                stateMachine.StateTime >= swordStoreTime)
            {
                swordStored = true;
                stateMachine.Animation.ShowStyle(
                    DemonSwordsmanStyle.Beast);
            }

            float phaseAnimationEnd =
                stateMachine.Settings.PhaseFearTime +
                stateMachine.Settings.PhaseRageTime;

            if (stateMachine.StateTime < phaseAnimationEnd)
            {
                return;
            }

            if (!phaseCompleted)
            {
                phaseCompleted = true;
                stateMachine.CompletePhaseChange();
                stateMachine.Animation.ShowStyle(stateMachine.Style);
                stateMachine.Animation.PlayLocomotion(
                    stateMachine.Style,
                    LocomotionFadeTime);
            }

            if (stateMachine.StateTime <
                phaseAnimationEnd +
                stateMachine.Settings.PhaseRepositionTime)
            {
                return;
            }

            stateMachine.ChangeToMove(
                DemonSwordsmanMoveMode.Approach);
        }

        public void Exit()
        {
        }
    }
}
