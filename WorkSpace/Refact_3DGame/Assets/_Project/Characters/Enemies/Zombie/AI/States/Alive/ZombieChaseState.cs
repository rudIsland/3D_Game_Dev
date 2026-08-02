namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 목표가 탐지 범위에 있는 동안 목표를 따라간다.
    internal sealed class ZombieChaseState : IZombieState
    {
        private readonly ZombieAliveState aliveState;
        private readonly ZombieStateMachine stateMachine;

        public ZombieChaseState(
            ZombieAliveState aliveState,
            ZombieStateMachine stateMachine)
        {
            this.aliveState = aliveState;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            stateMachine.PlayChase();
        }

        public void Update(float deltaTime)
        {
            float targetDistanceSquared =
                stateMachine.GetTargetDistanceSquared();
            if (targetDistanceSquared > stateMachine.FindRangeSquared)
            {
                aliveState.ChangeToIdleAfterLostTarget();
                return;
            }

            if (targetDistanceSquared <= stateMachine.AttackRangeSquared)
            {
                if (stateMachine.IsFacingTarget())
                {
                    aliveState.ChangeToAttack();
                    return;
                }

                stateMachine.TurnToTarget(deltaTime);
                return;
            }

            stateMachine.MoveToTarget(deltaTime);
        }

        public void Exit()
        {
        }
    }
}
