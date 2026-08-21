namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 대기 중 중력을 적용하고 감지된 대상이 있으면 Combat 상위 상태로 넘긴다.
    internal sealed class NightShadeSwordIdleState : INightShadeSwordState
    {
        private readonly NightShadeSwordSituationReader situation;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;

        internal NightShadeSwordIdleState(
            NightShadeSwordSituationReader situation,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation)
        {
            this.situation = situation;
            this.movement = movement;
            this.animation = animation;
        }

        public void Enter()
        {
            animation.PlayIdle();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            movement.StayOnGround(deltaTime);
            if (!situation.IsTargetDetected)
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
