namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 사망 후에는 행동을 다시 시작하지 않고 중력과 지면만 유지한다.
    internal sealed class ZombieDeadState : IZombieState
    {
        private readonly ZombieStateMachine stateMachine; // 현재 행동 상태
        private float remainingKeepTime; // 시간 설정
        private bool isAnimationComplete; // 기능 사용 여부
        private bool hasRequestedRelease; // 기능 사용 여부

        public ZombieDeadState(ZombieStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            remainingKeepTime = 0f;
            isAnimationComplete = false;
            hasRequestedRelease = false;
            stateMachine.PlayDead();
        }

        public void Update(float deltaTime)
        {
            stateMachine.StayOnGround(deltaTime);

            if (hasRequestedRelease)
            {
                return;
            }

            if (!isAnimationComplete)
            {
                if (!stateMachine.TryGetCurrentAnimationTime(
                        out float normalizedTime) ||
                    stateMachine.IsAnimationTransitioning() ||
                    normalizedTime < 1f)
                {
                    return;
                }

                isAnimationComplete = true;
                remainingKeepTime = stateMachine.DeadBodyKeepTime;

                if (remainingKeepTime > 0f)
                {
                    return;
                }
            }

            remainingKeepTime -= deltaTime;
            if (remainingKeepTime > 0f)
            {
                return;
            }

            hasRequestedRelease = true;
            stateMachine.RequestRelease();
        }

        public void Exit()
        {
        }
    }
}
