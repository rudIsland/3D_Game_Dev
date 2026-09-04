namespace Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordIdleRecoveryAction :
        NightShadeSwordRecoveryActionBase
    {
        public override NightShadeSwordActionId ActionId => NightShadeSwordActionId.IdleRecovery;

        internal NightShadeSwordIdleRecoveryAction(
            NightShadeSwordBehaviorContext context,
            NightShadeSwordRecoveryRuntimeConfig recovery)
            : base(context, recovery)
        {
        }

        public override NightShadeSwordActionScore GetScore(float randomBonus)
        {
            return new NightShadeSwordActionScore(
                Recovery.IdleBaseScore,
                TargetStatus.AttackDistanceRatio *
                    Recovery.IdleDistanceWeight,
                GetRepeatPenalty(),
                randomBonus);
        }

        protected override void PlayAnimation()
        {
            Animation.PlayIdle();
        }

        protected override void Move(float deltaTime)
        {
            Movement.TurnToTarget(TargetStatus.TargetPosition, deltaTime);
        }
    }
}
