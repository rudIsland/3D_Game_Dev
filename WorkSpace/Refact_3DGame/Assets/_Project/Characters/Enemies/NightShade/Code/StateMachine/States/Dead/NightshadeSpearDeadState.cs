namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 사망 애니메이션을 재생하고 시체 유지 시간이 지나면 해제를 요청한다.
    internal sealed class NightshadeSpearDeadState : INightshadeSpearState
    {
        private readonly NightshadeSpearStateMachine stateMachine;
        private float remainingKeepTime;
        private bool hasRequestedRelease;

        public string Name => nameof(NightshadeSpearDeadState);

        internal NightshadeSpearDeadState(
            NightshadeSpearStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            remainingKeepTime = stateMachine.DeadBodyKeepTime;
            hasRequestedRelease = false;
            stateMachine.Animation.PlayDeath();
        }

        public void Update(float deltaTime)
        {
            stateMachine.StayOnGround(deltaTime);
            if (hasRequestedRelease)
            {
                return;
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
