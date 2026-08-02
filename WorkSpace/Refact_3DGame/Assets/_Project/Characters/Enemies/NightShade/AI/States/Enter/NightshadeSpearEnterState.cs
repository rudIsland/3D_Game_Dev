namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 등장 애니메이션을 끝까지 재생한 뒤 대기 상태로 넘긴다.
    internal sealed class NightshadeSpearEnterState : INightshadeSpearState
    {
        private readonly NightshadeSpearStateMachine stateMachine;
        private bool hasEnteredAnimation;

        public string Name => nameof(NightshadeSpearEnterState);

        internal NightshadeSpearEnterState(
            NightshadeSpearStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            hasEnteredAnimation = false;
            stateMachine.Animation.SetMovement(0f, 0f);
            stateMachine.Animation.PlayEnter();
        }

        public void Update(float deltaTime)
        {
            stateMachine.StayOnGround(deltaTime);
            bool hasActionTime = stateMachine.TryGetCurrentActionTime(
                out float normalizedTime);
            if (hasActionTime)
            {
                hasEnteredAnimation = true;
            }

            if (!hasEnteredAnimation ||
                stateMachine.IsActionTransitioning() ||
                !hasActionTime ||
                normalizedTime < 1f)
            {
                return;
            }

            stateMachine.Animation.ResetActionSpeed();
            stateMachine.ChangeToIdleState();
        }

        public void Exit()
        {
        }
    }
}
