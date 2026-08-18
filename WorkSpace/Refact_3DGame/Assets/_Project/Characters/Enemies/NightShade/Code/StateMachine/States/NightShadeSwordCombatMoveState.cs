using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 공격 사이에 후퇴하거나 좌우로 거리를 조절한다.
    internal sealed class NightShadeSwordCombatMoveState : INightShadeSwordState
    {
        private readonly NightShadeSwordTargetReader targetReader;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;
        private readonly NightShadeSwordFightMemory fightMemory;

        private NightShadeCombatMoveType moveType;
        private float remainingMoveTime;

        internal NightShadeSwordCombatMoveState(
            NightShadeSwordTargetReader targetReader,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordFightMemory fightMemory)
        {
            this.targetReader = targetReader;
            this.movement = movement;
            this.animation = animation;
            this.settings = settings;
            this.fightMemory = fightMemory;
        }

        public void Enter()
        {
            moveType = fightMemory.ChooseCombatMove(settings.IsVeryClose(targetReader.DistanceSquared));
            remainingMoveTime = settings.CombatMoveDuration;
            animation.PlayCombatMove(moveType);
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            if (!targetReader.IsFound(settings.FindRangeSquared))
            {
                fightMemory.ResetCompletedAttackCount();
                return NightShadeSwordStateId.Idle;
            }

            movement.MoveForCombat(
                targetReader.Position,
                moveType,
                settings.CombatMoveSpeed,
                settings.TurnSpeed,
                deltaTime);
            remainingMoveTime = Mathf.Max(0f, remainingMoveTime - deltaTime);
            if (remainingMoveTime > 0f)
            {
                return null;
            }

            fightMemory.ResetCompletedAttackCount();
            return settings.GetApproachState(targetReader.DistanceSquared);
        }

        public void Exit()
        {
        }
    }
}
