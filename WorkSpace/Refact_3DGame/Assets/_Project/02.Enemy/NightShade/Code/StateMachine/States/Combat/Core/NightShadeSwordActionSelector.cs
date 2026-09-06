// 현재 단계에서 실행할 Action을 점수로 선택한다.
namespace Characters.Enemies.NightShade
{
    // 고정 후보 배열을 순서대로 평가하고 가장 높은 Utility 점수를 고른다.
    internal sealed class NightShadeSwordActionSelector
    {
        private readonly INightShadeSwordRandomProvider randomProvider;
        private readonly NightShadeSwordCombatDebug debug;

        internal NightShadeSwordActionSelector(
            INightShadeSwordRandomProvider randomProvider,
            NightShadeSwordCombatDebug debug)
        {
            this.randomProvider = randomProvider;
            this.debug = debug;
        }

        internal INightShadeSwordCombatAction Select(
            NightShadeSwordCombatPhase combatPhase,
            INightShadeSwordCombatAction[] candidates,
            float randomBonusMax)
        {
            int candidateCount = candidates != null ? candidates.Length : 0;
            candidateCount = candidateCount < 0
                ? 0
                : candidateCount > NightShadeSwordCombatDebug.CandidateCapacity
                    ? NightShadeSwordCombatDebug.CandidateCapacity
                    : candidateCount;
            debug.BeginEvaluation(combatPhase, candidateCount);

            int selectedIndex = -1;
            float highestScore = float.NegativeInfinity;
            for (int index = 0; index < candidateCount; index++)
            {
                INightShadeSwordCombatAction candidate = candidates[index];
                bool canStart = candidate.CanStart(
                    out NightShadeSwordActionRejectReason rejectReason);
                NightShadeSwordActionScore score = default;
                if (canStart)
                {
                    float randomBonus = randomProvider.Next01() * randomBonusMax;
                    score = candidate.GetScore(randomBonus);
                    if (score.FinalScore > highestScore)
                    {
                        highestScore = score.FinalScore;
                        selectedIndex = index;
                    }
                }

                debug.SetCandidate(
                    index,
                    candidate.ActionId,
                    canStart,
                    rejectReason,
                    in score);
            }

            if (selectedIndex < 0)
            {
                return null;
            }

            debug.SelectCandidate(selectedIndex);
            return candidates[selectedIndex];
        }
    }
}
