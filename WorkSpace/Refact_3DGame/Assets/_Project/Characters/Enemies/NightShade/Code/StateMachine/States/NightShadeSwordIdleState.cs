namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 대기 중 중력을 적용하고 탐지 범위에 들어온 대상을 찾는다.
    internal sealed class NightShadeSwordIdleState : INightShadeSwordState
    {
        private readonly NightShadeSwordTargetReader targetReader;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;

        internal NightShadeSwordIdleState(
            NightShadeSwordTargetReader targetReader,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings)
        {
            this.targetReader = targetReader;
            this.movement = movement;
            this.animation = animation;
            this.settings = settings;
        }

        public void Enter()
        {
            animation.PlayIdle();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            movement.StayOnGround(deltaTime);
            if (!targetReader.IsFound(settings.FindRangeSquared))
            {
                return null;
            }

            return targetReader.DistanceSquared <= settings.WalkStartRangeSquared
                ? NightShadeSwordStateId.Walk
                : NightShadeSwordStateId.Chase;
        }

        public void Exit()
        {
        }
    }
}
