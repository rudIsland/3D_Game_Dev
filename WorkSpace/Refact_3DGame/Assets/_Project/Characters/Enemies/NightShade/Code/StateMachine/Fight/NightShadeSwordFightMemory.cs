using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 한 번의 활성화 동안 이어지는 공격과 전투 이동 기록을 보관한다.
    internal sealed class NightShadeSwordFightMemory
    {
        internal bool HasPreviousAttack { get; private set; }
        internal NightShadeSwordAttackType PreviousAttackType { get; private set; }
        internal float RemainingAttackCooldown { get; private set; }
        internal int CompletedAttackCount { get; private set; }
        internal bool HasPendingComboSecond { get; private set; }

        private bool moveLeftNext;

        internal void Reset()
        {
            HasPreviousAttack = false;
            PreviousAttackType = NightShadeSwordAttackType.Light;
            RemainingAttackCooldown = 0f;
            CompletedAttackCount = 0;
            HasPendingComboSecond = false;
            moveLeftNext = true;
        }

        internal void UpdateAttackCooldown(float deltaTime)
        {
            if (RemainingAttackCooldown > 0f)
            {
                RemainingAttackCooldown = Mathf.Max(0f, RemainingAttackCooldown - deltaTime);
            }
        }

        internal void RecordAttack(NightShadeSwordAttackType attackType)
        {
            PreviousAttackType = attackType;
            HasPreviousAttack = true;
        }

        internal void CompleteAttack(float recoveryTime)
        {
            RemainingAttackCooldown = Mathf.Max(0f, recoveryTime);
            CompletedAttackCount++;
        }

        internal void ReserveComboSecond(float delay)
        {
            HasPendingComboSecond = true;
            RemainingAttackCooldown = Mathf.Max(0f, delay);
        }

        internal bool TakePendingComboSecond()
        {
            if (!HasPendingComboSecond)
            {
                return false;
            }

            HasPendingComboSecond = false;
            return true;
        }

        internal void CancelComboSecond()
        {
            HasPendingComboSecond = false;
        }

        internal void ResetCompletedAttackCount()
        {
            CompletedAttackCount = 0;
        }

        internal NightShadeCombatMoveType ChooseCombatMove(bool isTargetVeryClose)
        {
            if (isTargetVeryClose)
            {
                return NightShadeCombatMoveType.Backward;
            }

            NightShadeCombatMoveType moveType = moveLeftNext
                ? NightShadeCombatMoveType.Left
                : NightShadeCombatMoveType.Right;
            moveLeftNext = !moveLeftNext;
            return moveType;
        }
    }
}
