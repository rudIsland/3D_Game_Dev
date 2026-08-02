namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 피격 중에는 수평 이동을 멈추고 Hit 애니메이션이 끝나기를 기다린다.
    internal sealed class ZombieHitState : IZombieState
    {
        private readonly ZombieStateMachine stateMachine;

        public ZombieHitState(ZombieStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            Restart();
        }

        public void Update(float deltaTime)
        {
            stateMachine.StayOnGround(deltaTime);

            if (stateMachine.TryGetCurrentAnimationTime(
                    out float normalizedTime) &&
                !stateMachine.IsAnimationTransitioning() &&
                normalizedTime >= 1f)
            {
                stateMachine.ChangeToAliveState();
            }
        }

        public void Exit()
        {
        }

        public void Restart()
        {
            stateMachine.PlayHitFromStart();
        }
    }
}
