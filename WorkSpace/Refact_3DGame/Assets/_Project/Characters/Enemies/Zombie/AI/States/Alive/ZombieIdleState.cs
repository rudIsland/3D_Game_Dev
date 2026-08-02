namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 목표가 탐지되지 않는 동안 제자리에서 기다린다.
    internal sealed class ZombieIdleState : IZombieState
    {
        private readonly ZombieAliveState aliveState;
        private readonly ZombieStateMachine stateMachine;
        private float timeUntilTargetCheck;

        public ZombieIdleState(
            ZombieAliveState aliveState,
            ZombieStateMachine stateMachine)
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
