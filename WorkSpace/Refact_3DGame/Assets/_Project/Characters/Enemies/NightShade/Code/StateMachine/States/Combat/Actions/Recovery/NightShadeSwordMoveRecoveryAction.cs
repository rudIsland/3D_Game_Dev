using UnityEngine;

namespace Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordMoveRecoveryAction :
        NightShadeSwordRecoveryActionBase
    {
        private readonly NightShadeSwordActionId actionId;
        private readonly NightShadeCombatMoveType moveType;

        public override NightShadeSwordActionId ActionId => actionId;

        internal NightShadeSwordMoveRecoveryAction(
            NightShadeSwordActionId actionId,
            NightShadeCombatMoveType moveType,
            NightShadeSwordBehaviorContext context,
            NightShadeSwordRecoveryRuntimeConfig recovery)
            : base(context, recovery)
        {
            this.actionId = actionId;
            this.moveType = moveType;
        }

        public override NightShadeSwordActionScore GetScore(float randomBonus)
        {
            float distanceScore;
            float baseScore;
            if (actionId == NightShadeSwordActionId.BackRecovery)
            {
                baseScore = Recovery.BackBaseScore;
                distanceScore = (1f - TargetStatus.AttackDistanceRatio) *
                    Recovery.BackCloseWeight;
            }
            else
            {
                baseScore = Recovery.SideBaseScore;
                float sideDistanceFitness = 1f - Mathf.Clamp01(
                    Mathf.Abs(TargetStatus.AttackDistanceRatio - 0.5f) / 0.5f);
                distanceScore = sideDistanceFitness *
                    Recovery.SideDistanceWeight;
            }

            return new NightShadeSwordActionScore(
                baseScore,
                distanceScore,
                GetRepeatPenalty(),
                randomBonus);
        }

        protected override void PlayAnimation()
        {
            Animation.PlayCombatMove(moveType);
        }

        protected override void Move(float deltaTime)
        {
            Movement.MoveForRecovery(
                TargetStatus.TargetPosition,
                moveType,
                deltaTime);
        }
    }
}
