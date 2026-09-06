namespace Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordSingleAttackAction :
        NightShadeSwordAttackActionBase
    {
        private readonly NightShadeSwordActionId actionId;
        private readonly NightShadeSwordAttackType attackType;

        public override NightShadeSwordActionId ActionId => actionId;
        protected override NightShadeSwordAttackType FirstAttackType => attackType;
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
