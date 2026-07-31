namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 사망 후에는 이동을 멈추고 다른 상태로 돌아가지 않는다.
    internal sealed class ZombieDeadState : IZombieState
    {
        private readonly ZombieStateMachine stateMachine;

        public string Name => "Dead";

        public ZombieDeadState(ZombieStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            stateMachine.SetMoveSpeed(0f);
            stateMachine.PlayDeath();
        }

        public void Update(float deltaTime)
        {
            stateMachine.StayOnGround(deltaTime);
        }

        public void Exit()
        {
        }
    }
}
