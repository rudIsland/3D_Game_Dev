namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordSingleAttackAction :
        NightShadeSwordAttackActionBase
    {
        private readonly NightShadeSwordActionId actionId;
        private readonly NightShadeSwordAttackType attackType;

        public override NightShadeSwordActionId ActionId => actionId;
        protected override NightShadeSwordAttackType FirstAttackType => attackType;
        public override bool ProtectsSmallHit
        {
            get
            {
                if (actionId != NightShadeSwordActionId.Heavy ||
                    Animation.IsTransitioning() ||
                    !Animation.TryGetRequestedAnimationTime(
                        out float normalizedTime))
                {
                    return false;
                }

                return NightShadeSwordAttackTiming.IsHeavyProtectionTime(
                    normalizedTime);
            }
        }

        internal NightShadeSwordSingleAttackAction(
            NightShadeSwordActionId actionId,
            NightShadeSwordAttackType attackType,
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordActions actions)
            : base(
                situation,
                fightMemory,
                movement,
                animation,
                settings,
                actions)
        {
            this.actionId = actionId;
            this.attackType = attackType;
        }
    }
}
