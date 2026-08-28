namespace Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordSingleAttackAction :
        NightShadeSwordAttackActionBase
    {
        private const float HeavyProtectionStartNormalizedTime = 0.16f;
        private const float HeavyProtectionEndNormalizedTime = 0.39f;

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

                return normalizedTime >= HeavyProtectionStartNormalizedTime &&
                    normalizedTime < HeavyProtectionEndNormalizedTime;
            }
        }

        internal NightShadeSwordSingleAttackAction(
            NightShadeSwordActionId actionId,
            NightShadeSwordAttackType attackType,
            NightShadeSwordBehaviorContext context,
            NightShadeSwordRuntimeAttackData attackData,
            NightShadeSwordAttackSelectionRuntimeConfig attackSelection,
            NightShadeSwordCombatOutput combatOutput)
            : base(
                context,
                attackData,
                attackSelection,
                combatOutput)
        {
            this.actionId = actionId;
            this.attackType = attackType;
        }
    }
}
