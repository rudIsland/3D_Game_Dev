namespace Characters.Enemies.Zombie
{
    // 플레이어를 놓친 뒤 소환 지점으로 돌아간다.
    internal sealed class ZombieReturnState : IZombieState
    {
        private readonly ZombieAliveState aliveState;
        private readonly ZombieStateMachine stateMachine;

        internal ZombieReturnState(
            ZombieAliveState aliveState,
            ZombieStateMachine stateMachine)
        {
            this.aliveState = aliveState;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            stateMachine.EndAttackHit();
            stateMachine.PlayReturn();
        }

        public void Update(float deltaTime)
        {
            if (stateMachine.CanResumeTrackingFromReturn())
            {
                aliveState.ChangeToChaseFromReturn();
                return;
            }

            if (stateMachine.HasArrivedHome())
            {
                aliveState.ChangeToIdleAfterReturn();
                return;
            }

            stateMachine.MoveToHome(deltaTime);
        }

        public void Exit()
        {
            stateMachine.StopPath();
        }
    }
}
