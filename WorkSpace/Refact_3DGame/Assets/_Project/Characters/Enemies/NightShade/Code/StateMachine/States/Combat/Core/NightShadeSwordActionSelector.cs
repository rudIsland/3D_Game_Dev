// 현재 단계에서 실행할 Action을 점수로 선택한다.
namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 고정 후보 배열을 순서대로 평가하고 가장 높은 Utility 점수를 고른다.
    internal sealed class NightShadeSwordActionSelector
    {
        private readonly INightShadeSwordRandomProvider randomProvider;
        private readonly NightShadeSwordSettings settings;
        private readonly NightShadeSwordCombatDebug debug;

        internal NightShadeSwordActionSelector(
            INightShadeSwordRandomProvider randomProvider,
            NightShadeSwordSettings settings,
            NightShadeSwordCombatDebug debug)
        {
            this.randomProvider = randomProvider;
            this.settings = settings;
            this.debug = debug;
        }

        internal INightShadeSwordCombatAction Select(
            INightShadeSwordCombatAction[] candidates,
            int candidateCount,
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory)
        {
            candidateCount = candidateCount < 0
                ? 0
                : candidateCount > NightShadeSwordCombatDebug.CandidateCapacity
                    ? NightShadeSwordCombatDebug.CandidateCapacity
                    : candidateCount;
            NightShadeSwordCombatPhase phase = candidateCount > 0
                ? candidates[0].Phase
                : NightShadeSwordCombatPhase.None;
            debug.BeginEvaluation(phase, candidateCount);

            int selectedIndex = -1;
            float highestScore = float.NegativeInfinity;
            float randomBonusMax = phase == NightShadeSwordCombatPhase.Attack
                ? settings.AttackRandomBonusMax
                : settings.RecoveryRandomBonusMax;

            for (int index = 0; index < candidateCount; index++)
            {
                INightShadeSwordCombatAction candidate = candidates[index];
                bool canStart = candidate.CanStart(
                    situation,
                    fightMemory,
                    out NightShadeSwordActionRejectReason rejectReason);
                NightShadeSwordActionScore score = default;
                if (canStart)
                {
                    float randomBonus = randomProvider.Next01() * randomBonusMax;
                    score = candidate.GetScore(
                        situation,
                        fightMemory,
                        randomBonus);
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
