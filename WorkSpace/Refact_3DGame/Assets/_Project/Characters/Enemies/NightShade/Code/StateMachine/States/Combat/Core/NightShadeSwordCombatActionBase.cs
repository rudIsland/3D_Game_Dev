namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal abstract class NightShadeSwordCombatActionBase : INightShadeSwordCombatAction
    {
        protected readonly NightShadeSwordSituationReader Situation;
        protected readonly NightShadeSwordFightMemory FightMemory;

        public abstract NightShadeSwordActionId ActionId { get; }
        public abstract NightShadeSwordCombatPhase Phase { get; }
        public bool IsFinished { get; protected set; }

        protected NightShadeSwordCombatActionBase(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory)
        {
            Situation = situation;
            FightMemory = fightMemory;
        }

        public abstract bool CanStart(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            out NightShadeSwordActionRejectReason rejectReason);

        public abstract bool CanContinue(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            out NightShadeSwordActionStopReason stopReason);

        public virtual NightShadeSwordActionScore GetScore(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            float randomBonus)
        {
            return default;
        }

        public virtual void Enter()
        {
            IsFinished = false;
        }

        public abstract void Update(float deltaTime);

        public virtual void Exit(NightShadeSwordActionStopReason stopReason)
        {
        }
    }
}
