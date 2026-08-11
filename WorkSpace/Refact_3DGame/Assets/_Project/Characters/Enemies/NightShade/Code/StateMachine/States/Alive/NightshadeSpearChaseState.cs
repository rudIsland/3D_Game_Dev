namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 공격 조건을 확인하고, 조건이 맞지 않으면 거리와 속도를 조절한다.
    internal sealed class NightshadeSpearChaseState : INightshadeSpearState
    {
        private readonly NightshadeSpearStateMachine stateMachine;

        public string Name => nameof(NightshadeSpearChaseState);

        internal NightshadeSpearChaseState(
            NightshadeSpearStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
        }

        public void Update(float deltaTime)
        {
            if (!stateMachine.IsTargetFound())
            {
                stateMachine.ChangeToIdleState();
                return;
            }

            if (stateMachine.GetTargetDistanceSquared() <=
                stateMachine.MaximumAttackRangeSquared)
            {
                if (stateMachine.TryChangeToContextAttackState())
                {
                    return;
                }
            }

            stateMachine.MoveToTarget(deltaTime);
        }

        public void Exit()
        {
        }
    }
}
