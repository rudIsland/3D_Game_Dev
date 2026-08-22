using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal abstract class NightShadeSwordRecoveryActionBase :
        INightShadeSwordCombatAction
    {
        protected readonly NightShadeSwordBehaviorContext Context;
        protected readonly NightShadeSwordRecoveryRuntimeConfig Recovery;

        private float remainingTime;

        protected INightShadeSwordMovement Movement => Context.Movement;
        protected INightShadeSwordAnimation Animation => Context.Animation;
        protected NightShadeSwordTargetStatus TargetStatus => Context.TargetStatus;
        protected NightShadeSwordCombatMemory CombatMemory => Context.CombatMemory;

        public abstract NightShadeSwordActionId ActionId { get; }
        public bool IsFinished { get; protected set; }

        protected NightShadeSwordRecoveryActionBase(
            NightShadeSwordBehaviorContext context,
            NightShadeSwordRecoveryRuntimeConfig recovery)
        {
            Context = context;
            Recovery = recovery;
        }

        public bool CanStart(
            out NightShadeSwordActionRejectReason rejectReason)
        {
            if (!TargetStatus.IsDetected)
            {
                rejectReason = NightShadeSwordActionRejectReason.TargetNotDetected;
                return false;
            }

            rejectReason = NightShadeSwordActionRejectReason.None;
            return true;
        }

        public bool CanContinue(
            out NightShadeSwordActionStopReason stopReason)
        {
            if (!TargetStatus.IsDetected)
            {
                stopReason = NightShadeSwordActionStopReason.TargetLost;
                return false;
            }

            stopReason = NightShadeSwordActionStopReason.None;
            return true;
        }

        public void Enter()
        {
            IsFinished = false;
            remainingTime = Recovery.MoveDuration;
            CombatMemory.RecordRecovery(ActionId);
            PlayAnimation();
        }

        public void Update(float deltaTime)
        {
            Move(deltaTime);
            remainingTime = Mathf.Max(0f, remainingTime - deltaTime);
            IsFinished = remainingTime <= 0f;
        }

        public void Exit(NightShadeSwordActionStopReason stopReason)
        {
        }

        public abstract NightShadeSwordActionScore GetScore(float randomBonus);

        protected float GetRepeatPenalty()
        {
            return CombatMemory.HasPreviousRecovery &&
                CombatMemory.PreviousRecovery == ActionId
                    ? Recovery.RepeatPenalty
                    : 0f;
        }

        protected abstract void PlayAnimation();
        protected abstract void Move(float deltaTime);
    }
}
