using System;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    internal sealed class DemonSwordsmanAttackChooser
    {
        private readonly DemonSwordsmanAttackPattern[] attacks; // 공격 관련 설정 또는 상태
        private readonly float[] nextUseTimes; // 시간 설정
        private int lastAttackIndex = -1; // 공격 관련 설정 또는 상태
        private bool lastAttackWasJump; // 기능 사용 여부

        public DemonSwordsmanAttackChooser(
            DemonSwordsmanAttackPattern[] attackPatterns)
        {
            attacks = attackPatterns ?? Array.Empty<DemonSwordsmanAttackPattern>();
            nextUseTimes = new float[attacks.Length];
        }

        public void Reset()
        {
            Array.Clear(nextUseTimes, 0, nextUseTimes.Length);
            lastAttackIndex = -1;
            lastAttackWasJump = false;
        }

        public DemonSwordsmanAttackPattern Choose(
            DemonSwordsmanPhase phase,
            DemonSwordsmanStyle style,
            float distance,
            float absoluteAngle,
            float currentTime,
            float randomValue)
        {
            DemonSwordsmanPhaseMask currentPhaseMask =
                phase == DemonSwordsmanPhase.PhaseOne
                    ? DemonSwordsmanPhaseMask.PhaseOne
                    : DemonSwordsmanPhaseMask.PhaseTwo;

            float totalWeight = 0f;

            for (int index = 0; index < attacks.Length; index++)
            {
                if (CanChoose(
                        index,
                        currentPhaseMask,
                        style,
                        distance,
                        absoluteAngle,
                        currentTime))
                {
                    totalWeight += attacks[index].SelectionWeight;
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float clampedRandomValue = randomValue < 0f
                ? 0f
                : randomValue > 0.999999f
                    ? 0.999999f
                    : randomValue;
            float targetWeight = clampedRandomValue * totalWeight;

            for (int index = 0; index < attacks.Length; index++)
            {
                if (!CanChoose(
                        index,
                        currentPhaseMask,
                        style,
                        distance,
                        absoluteAngle,
                        currentTime))
                {
                    continue;
                }

                targetWeight -= attacks[index].SelectionWeight;

                if (targetWeight <= 0f)
                {
                    return attacks[index];
                }
            }

            return null;
        }

        public void MarkUsed(
            DemonSwordsmanAttackPattern attack,
            float currentTime)
        {
            int attackIndex = FindAttackIndex(attack);

            if (attackIndex < 0)
            {
                return;
            }

            nextUseTimes[attackIndex] =
                currentTime + attacks[attackIndex].Cooldown;
            lastAttackIndex = attackIndex;
            lastAttackWasJump = attacks[attackIndex].IsJumpAttack;
        }

        private bool CanChoose(
            int attackIndex,
            DemonSwordsmanPhaseMask currentPhase,
            DemonSwordsmanStyle style,
            float distance,
            float absoluteAngle,
            float currentTime)
        {
            DemonSwordsmanAttackPattern attack = attacks[attackIndex];

            if ((attack.PhaseMask & currentPhase) == 0 ||
                attack.Style != style ||
                attackIndex == lastAttackIndex ||
                currentTime < nextUseTimes[attackIndex] ||
                distance < attack.MinimumDistance ||
                distance > attack.MaximumDistance ||
                absoluteAngle > attack.MaximumAngle)
            {
                return false;
            }

            return !attack.IsJumpAttack || !lastAttackWasJump;
        }

        private int FindAttackIndex(DemonSwordsmanAttackPattern attack)
        {
            for (int index = 0; index < attacks.Length; index++)
            {
                if (ReferenceEquals(attacks[index], attack))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
