namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordWatchTargetAction : NightShadeSwordCombatActionBase
    {
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;

        public override NightShadeSwordActionId ActionId => NightShadeSwordActionId.WatchTarget;
        public override NightShadeSwordCombatPhase Phase => NightShadeSwordCombatPhase.Positioning;

        internal NightShadeSwordWatchTargetAction(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings)
            : base(situation, fightMemory)
        {
            this.movement = movement;
            this.animation = animation;
            this.settings = settings;
        }

        public override bool CanStart(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            out NightShadeSwordActionRejectReason rejectReason)
        {
            if (!situation.IsTargetDetected)
            {
                rejectReason = NightShadeSwordActionRejectReason.TargetNotDetected;
                return false;
            }

            if (!situation.IsInsideAttackRange)
            {
                rejectReason = NightShadeSwordActionRejectReason.TargetOutsideAttackRange;
                return false;
            }

            rejectReason = NightShadeSwordActionRejectReason.None;
            return true;
        }

        public override bool CanContinue(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            out NightShadeSwordActionStopReason stopReason)
        {
            if (!situation.IsTargetDetected)
            {
                stopReason = NightShadeSwordActionStopReason.TargetLost;
                return false;
            }

            if (!situation.IsInsideAttackRange)
            {
                stopReason = NightShadeSwordActionStopReason.RangeChanged;
                return false;
            }

            stopReason = NightShadeSwordActionStopReason.None;
            return true;
        }

        public override void Enter()
        {
            base.Enter();
            animation.PlayIdle();
        }

        public override void Update(float deltaTime)
        {
            movement.TurnTo(
                Situation.TargetPosition,
                settings.TurnSpeed,
                deltaTime);
            IsFinished = Situation.IsFacingAttackDirection &&
                FightMemory.RemainingPostAttackDelay <= 0f;
        }
    }
}
