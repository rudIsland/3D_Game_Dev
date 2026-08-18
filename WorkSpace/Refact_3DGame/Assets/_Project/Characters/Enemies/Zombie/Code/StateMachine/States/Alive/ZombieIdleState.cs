namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 목표가 탐지되지 않는 동안 제자리에서 기다린다.
    internal sealed class ZombieIdleState : IZombieState
    {
        private readonly ZombieAliveState aliveState; // 현재 행동 상태
        private readonly ZombieStateMachine stateMachine; // 현재 행동 상태
        private float timeUntilTargetCheck; // 대상 참조

        public ZombieIdleState(ZombieAliveState aliveState, ZombieStateMachine stateMachine)
        {
            this.aliveState = aliveState;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            stateMachine.PlayIdle();
            timeUntilTargetCheck =
                stateMachine.IdleTargetCheckInterval;
            stateMachine.UpdateTargetSnapshot();
            aliveState.ChooseIdleNextState();
        }

        public void Update(float deltaTime)
        {
            stateMachine.StayOnGround(deltaTime);

            timeUntilTargetCheck -= deltaTime;
            if (timeUntilTargetCheck > 0f)
            {
                return;
            }

            timeUntilTargetCheck =
                stateMachine.IdleTargetCheckInterval;
            stateMachine.UpdateTargetSnapshot();
            aliveState.ChooseIdleNextState();
        }

        public void Exit()
        {
        }

    }
}
