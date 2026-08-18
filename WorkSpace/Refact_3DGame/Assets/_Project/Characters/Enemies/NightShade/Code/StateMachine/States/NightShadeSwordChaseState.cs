namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 먼 대상을 달리기로 추적하고 가까워지면 걷기 상태로 넘긴다.
    internal sealed class NightShadeSwordChaseState : INightShadeSwordState
    {
        private readonly NightShadeSwordTargetReader targetReader;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;

        internal NightShadeSwordChaseState(
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
            animation.PlayChase();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            if (!targetReader.IsFound(settings.FindRangeSquared))
            {
                return NightShadeSwordStateId.Idle;
            }

            if (targetReader.DistanceSquared <= settings.WalkStartRangeSquared)
            {
                return NightShadeSwordStateId.Walk;
            }

            movement.MoveTo(
                targetReader.Position,
                settings.ChaseSpeed,
                settings.TurnSpeed,
                deltaTime);

            return null;
        }

        public void Exit()
        {
        }
    }
}
