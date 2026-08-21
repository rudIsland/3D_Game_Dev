namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordIdleRecoveryAction :
        NightShadeSwordRecoveryActionBase
    {
        public override NightShadeSwordActionId ActionId => NightShadeSwordActionId.IdleRecovery;

        internal NightShadeSwordIdleRecoveryAction(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings)
            : base(situation, fightMemory, movement, animation, settings)
        {
        }

        public override NightShadeSwordActionScore GetScore(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            float randomBonus)
        {
            return new NightShadeSwordActionScore(
                Settings.IdleRecoveryBaseScore,
                situation.AttackDistanceRatio *
                    Settings.IdleRecoveryDistanceWeight,
                GetRepeatPenalty(fightMemory),
                randomBonus);
        }

        protected override void PlayAnimation()
        {
            Animation.PlayIdle();
        }

        protected override void Move(float deltaTime)
        {
            Movement.TurnTo(
                Situation.TargetPosition,
                Settings.TurnSpeed,
                deltaTime);
        }
    }
}
