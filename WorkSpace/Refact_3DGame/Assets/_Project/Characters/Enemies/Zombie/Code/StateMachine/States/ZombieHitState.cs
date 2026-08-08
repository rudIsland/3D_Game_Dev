
namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 피격 중에는 공격 방향으로 밀리며 Hit 애니메이션 종료를 기다린다.
    internal sealed class ZombieHitState : IZombieState
    {
        private readonly ZombieStateMachine stateMachine; // 현재 행동 상태


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
