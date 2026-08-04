namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 대상이 탐지되기 전까지 제자리에서 대기한다.
    internal sealed class NightshadeSpearIdleState : INightshadeSpearState
    {
        private readonly NightshadeSpearStateMachine stateMachine;

        public string Name => nameof(NightshadeSpearIdleState);

        internal NightshadeSpearIdleState(
            NightshadeSpearStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            stateMachine.Animation.SetMovement(0f, 0f);
        }

        public void Update(float deltaTime)
        {
            stateMachine.StayOnGround(deltaTime);
            if (stateMachine.IsTargetFound())
            {
                stateMachine.ChangeToChaseState();
            }
        }

        public void Exit()
        {
        }
    }
}
