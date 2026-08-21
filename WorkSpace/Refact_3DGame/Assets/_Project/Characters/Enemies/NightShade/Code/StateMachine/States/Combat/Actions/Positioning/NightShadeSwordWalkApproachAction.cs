namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordWalkApproachAction : NightShadeSwordCombatActionBase
    {
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;

        public override NightShadeSwordActionId ActionId => NightShadeSwordActionId.WalkApproach;
        public override NightShadeSwordCombatPhase Phase => NightShadeSwordCombatPhase.Positioning;

        internal NightShadeSwordWalkApproachAction(
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

            if (situation.IsInsideAttackRange)
            {
                rejectReason = NightShadeSwordActionRejectReason.TargetInsideAttackRange;
                return false;
            }

            if (situation.DistanceSquared >= settings.RunStartRangeSquared)
            {
                rejectReason = NightShadeSwordActionRejectReason.TargetTooFarForWalk;
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

            if (situation.IsInsideAttackRange ||
                situation.DistanceSquared >= settings.RunStartRangeSquared)
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
            animation.PlayWalk();
        }

        public override void Update(float deltaTime)
        {
            movement.MoveTo(
                Situation.TargetPosition,
                settings.WalkSpeed,
                settings.TurnSpeed,
                deltaTime);
        }
    }
}
