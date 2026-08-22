namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 대기 중 중력을 적용하고 감지된 대상이 있으면 Combat 상위 상태로 넘긴다.
    internal sealed class NightShadeSwordIdleState : INightShadeSwordState
    {
        private readonly NightShadeSwordBehaviorContext context;

        internal NightShadeSwordIdleState(
            NightShadeSwordBehaviorContext context)
        {
            this.context = context;
        }

        public void Enter()
        {
            context.Animation.PlayIdle();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            context.Movement.StayOnGround(deltaTime);
            if (!context.TargetStatus.IsDetected)
            {
                return null;
            }

            return NightShadeSwordStateId.Combat;
        }

        public void Exit()
        {
        }
    }
}
