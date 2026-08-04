using System;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 거리, 페이즈, 쿨다운, 최근 공격을 보고 다음 공격을 고른다.
    internal sealed class NightshadeSpearAttackChooser
    {
        private const int RecentAttackCount = 4;

        private readonly NightshadeSpearAttackPattern[] attackPatterns;
        private readonly int[] recentAttackNumbers =
            new int[RecentAttackCount];
        private readonly Random random;

        private int recentAttackLength;
        private int recentAttackWriteIndex;
        private NightshadeSpearAttackGroup lastAttackGroup;
        private bool hasLastAttackGroup;
        private bool lastAttackCanChain;

        public NightshadeSpearAttackChooser(
            NightshadeSpearAttackPattern[] attackPatterns,
            int randomSeed = 0)
        {
            this.attackPatterns =
                attackPatterns ?? Array.Empty<NightshadeSpearAttackPattern>();
            random = new Random(
                randomSeed == 0 ? Environment.TickCount : randomSeed);
        }

        public NightshadeSpearAttackPattern Choose(
            float distanceSquared,
            float facingDot,
            float currentTime,
            int phase,
            out int attackNumber)
        {
            attackNumber = 0;

            bool hasFreshAttack = HasUsableAttack(
                distanceSquared,
                facingDot,
                currentTime,
                phase,
                true);

            float totalWeight = 0f;
            for (int index = 0; index < attackPatterns.Length; index++)
            {
                NightshadeSpearAttackPattern pattern = attackPatterns[index];
                if (!CanChoose(
                        pattern,
                        distanceSquared,
                        facingDot,
                        currentTime,
                        phase,
                        hasFreshAttack))
                {
                    continue;
                }

                totalWeight += GetWeight(pattern);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            double selectedWeight = random.NextDouble() * totalWeight;
            for (int index = 0; index < attackPatterns.Length; index++)
            {
                NightshadeSpearAttackPattern pattern = attackPatterns[index];
                if (!CanChoose(
                        pattern,
                        distanceSquared,
                        facingDot,
                        currentTime,
                        phase,
                        hasFreshAttack))
                {
                    continue;
                }

                selectedWeight -= GetWeight(pattern);
                if (selectedWeight > 0d)
                {
                    continue;
                }

                attackNumber = (int)pattern.AttackId;
                Remember(pattern, attackNumber);
                return pattern;
            }

            return null;
        }

        public void Reset()
        {
            recentAttackLength = 0;
            recentAttackWriteIndex = 0;
            hasLastAttackGroup = false;
            lastAttackCanChain = false;
            Array.Clear(recentAttackNumbers, 0, recentAttackNumbers.Length);
        }

        private bool HasUsableAttack(
            float distanceSquared,
            float facingDot,
            float currentTime,
            int phase,
            bool excludeRecent)
        {
            for (int index = 0; index < attackPatterns.Length; index++)
            {
                NightshadeSpearAttackPattern pattern = attackPatterns[index];
                if (pattern == null ||
                    !pattern.CanUse(distanceSquared, facingDot, currentTime, phase))
                {
                    continue;
                }

                if (!excludeRecent || !IsRecent((int)pattern.AttackId))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanChoose(
            NightshadeSpearAttackPattern pattern,
            float distanceSquared,
            float facingDot,
            float currentTime,
            int phase,
            bool excludeRecent)
        {
            if (pattern == null ||
                !pattern.CanUse(distanceSquared, facingDot, currentTime, phase))
            {
                return false;
            }

            return !excludeRecent || !IsRecent((int)pattern.AttackId);
        }

        private float GetWeight(NightshadeSpearAttackPattern pattern)
        {
            float weight = pattern.SelectionWeight;
            if (hasLastAttackGroup && pattern.AttackGroup == lastAttackGroup)
            {
                weight *= 0.35f;
            }

            if (lastAttackCanChain)
            {
                weight *= Math.Max(0.05f, pattern.ChainWeight);
            }

            return weight;
        }

        private void Remember(
            NightshadeSpearAttackPattern pattern,
            int attackNumber)
        {
            recentAttackNumbers[recentAttackWriteIndex] = attackNumber;
            recentAttackWriteIndex =
                (recentAttackWriteIndex + 1) % recentAttackNumbers.Length;
            recentAttackLength = Math.Min(
                recentAttackLength + 1,
                recentAttackNumbers.Length);
            lastAttackGroup = pattern.AttackGroup;
            hasLastAttackGroup = true;
            lastAttackCanChain = pattern.CanChain;
        }

        private bool IsRecent(int attackNumber)
        {
            for (int index = 0; index < recentAttackLength; index++)
            {
                if (recentAttackNumbers[index] == attackNumber)
                {
                    return true;
                }
            }

            return false;
        }
    }
}


