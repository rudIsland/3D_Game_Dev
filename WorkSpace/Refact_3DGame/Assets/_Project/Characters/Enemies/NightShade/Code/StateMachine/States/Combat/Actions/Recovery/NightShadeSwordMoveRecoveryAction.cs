using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
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
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings)
            : base(situation, fightMemory, movement, animation, settings)
        {
            this.actionId = actionId;
            this.moveType = moveType;
        }

        public override NightShadeSwordActionScore GetScore(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            float randomBonus)
        {
            float distanceScore;
            float baseScore;
            if (actionId == NightShadeSwordActionId.BackRecovery)
            {
                baseScore = Settings.BackRecoveryBaseScore;
                distanceScore = (1f - situation.AttackDistanceRatio) *
                    Settings.BackRecoveryCloseWeight;
            }
            else
            {
                baseScore = Settings.SideRecoveryBaseScore;
                float sideDistanceFitness = 1f - Mathf.Clamp01(
                    Mathf.Abs(situation.AttackDistanceRatio - 0.5f) / 0.5f);
                distanceScore = sideDistanceFitness *
                    Settings.SideRecoveryDistanceWeight;
            }

            return new NightShadeSwordActionScore(
                baseScore,
                distanceScore,
                GetRepeatPenalty(fightMemory),
                randomBonus);
        }

        protected override void PlayAnimation()
        {
            Animation.PlayCombatMove(moveType);
        }

        protected override void Move(float deltaTime)
        {
            Movement.MoveForCombat(
                Situation.TargetPosition,
                moveType,
                Settings.RecoveryMoveSpeed,
                Settings.TurnSpeed,
                deltaTime);
        }
    }
}
