namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 목표를 처음 찾았을 때 바라보고 발견 애니메이션을 재생한다.
    internal sealed class ZombieAlertState : IZombieState
    {
        private readonly ZombieAliveState aliveState; // 현재 행동 상태
        private readonly ZombieStateMachine stateMachine; // 현재 행동 상태
        private bool animationEndedByEvent; // 기능 사용 여부
        public ZombieAlertState(ZombieAliveState aliveState, ZombieStateMachine stateMachine)
        {
            this.aliveState = aliveState;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            animationEndedByEvent = false;
            stateMachine.PlayAlert();
        }

        public void Update(float deltaTime)
        {
            stateMachine.TurnToTarget(deltaTime);

            if (animationEndedByEvent)
            {
                aliveState.FinishAlert();
                return;
            }

            if (stateMachine.TryGetCurrentAnimationTime(out float normalizedTime) &&
                !stateMachine.IsAnimationTransitioning() &&
                normalizedTime >= 1f)
            {
                aliveState.FinishAlert();
            }
        }

        public void Exit()
        {
        }

        internal void NotifyAnimationEnded()
        {
            animationEndedByEvent = true;
        }
    }
}
