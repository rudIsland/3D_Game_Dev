using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal abstract class NightShadeSwordRecoveryActionBase :
        NightShadeSwordCombatActionBase
    {
        protected readonly INightShadeSwordMovement Movement;
        protected readonly INightShadeSwordAnimation Animation;
        protected readonly NightShadeSwordSettings Settings;

        private float remainingTime;

        public override NightShadeSwordCombatPhase Phase => NightShadeSwordCombatPhase.Recovery;

        protected NightShadeSwordRecoveryActionBase(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings)
            : base(situation, fightMemory)
        {
            Movement = movement;
            Animation = animation;
            Settings = settings;
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

            stopReason = NightShadeSwordActionStopReason.None;
            return true;
        }

        public override void Enter()
        {
            base.Enter();
            remainingTime = Settings.RecoveryMoveDuration;
            FightMemory.RecordRecovery(ActionId);
            PlayAnimation();
        }

        public override void Update(float deltaTime)
        {
            Move(deltaTime);
            remainingTime = Mathf.Max(0f, remainingTime - deltaTime);
            IsFinished = remainingTime <= 0f;
        }

        protected float GetRepeatPenalty(
            NightShadeSwordFightMemory fightMemory)
        {
            return fightMemory.HasPreviousRecovery &&
                fightMemory.PreviousRecovery == ActionId
                    ? Settings.RecoveryRepeatPenalty
                    : 0f;
        }

        protected abstract void PlayAnimation();
        protected abstract void Move(float deltaTime);
    }
}
