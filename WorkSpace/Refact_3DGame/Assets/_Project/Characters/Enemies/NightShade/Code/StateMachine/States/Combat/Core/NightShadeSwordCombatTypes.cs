// Combat HFSM과 Action이 공유하는 값 형식을 정의한다.
using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal enum NightShadeSwordCombatPhase
    {
        None = 0,
        Approach = 1,
        PrepareAttack = 2,
        Attack = 3,
        Recovery = 4
    }

    internal enum NightShadeSwordApproachMode
    {
        None = 0,
        Chase = 1,
        Walk = 2
    }

    internal enum NightShadeSwordActionId
    {
        None = 0,
        // 1~3은 기존 직렬화 값과 디버그 기록 호환을 위해 비워 둔다.
        Light = 4,
        Combo = 5,
        Heavy = 6,
        WideSwing = 7,
        IdleRecovery = 8,
        BackRecovery = 9,
        LeftRecovery = 10,
        RightRecovery = 11
    }

    internal enum NightShadeSwordActionRejectReason
    {
        None = 0,
        TargetNotDetected = 1,
        TargetOutsideAttackRange = 2,
        DirectionNotMatched = 3,
        PostAttackDelayRemaining = 4
    }

    // Action Exit에서 정리 방법을 결정할 때 사용하는 종료 이유다.
    internal enum NightShadeSwordActionStopReason
    {
        None = 0,           // 종료 이유가 아직 없음
        Completed = 1,      // Action이 정상적으로 종료됨
        Interrupted = 2,    // 피격이나 사망이 현재 Action을 중단함
        TargetLost = 3,     // 타겟이 사라지거나 감지 범위를 벗어남
        RangeChanged = 4,   // 거리 구간이 바뀌어 다른 위치 Action이 필요함
        Disabled = 5,       // 풀 반환이나 Disable로 전투 객체가 비활성화됨
        Replaced = 6        // 상위 상태 전환으로 현재 Action이 교체됨
    }

    internal readonly struct NightShadeSwordActionScore
    {
        internal float BaseScore { get; }
        internal float DistanceScore { get; }
        internal float RepeatPenalty { get; }
        internal float RandomBonus { get; }
        internal float FinalScore { get; }

        internal NightShadeSwordActionScore(
            float baseScore,
            float distanceScore,
            float repeatPenalty,
            float randomBonus)
        {
            BaseScore = baseScore;
            DistanceScore = distanceScore;
            RepeatPenalty = repeatPenalty;
            RandomBonus = randomBonus;
            FinalScore = Mathf.Clamp01(
                baseScore + distanceScore - repeatPenalty + randomBonus);
        }
    }

    [Serializable]
    internal struct NightShadeSwordAttackScoreSettings
    {
        [SerializeField, Range(0f, 1f)] private float baseScore;
        [SerializeField, Range(0f, 1f)] private float preferredDistance;
        [SerializeField, Range(0.01f, 1f)] private float distanceTolerance;

        internal float BaseScore => baseScore;
        internal float PreferredDistance => preferredDistance;
        internal float DistanceTolerance => Mathf.Max(0.01f, distanceTolerance);

        internal NightShadeSwordAttackScoreSettings(
            float baseScore,
            float preferredDistance,
            float distanceTolerance)
        {
            this.baseScore = baseScore;
            this.preferredDistance = preferredDistance;
            this.distanceTolerance = distanceTolerance;
        }

        internal void Validate()
        {
            baseScore = Mathf.Clamp01(baseScore);
            preferredDistance = Mathf.Clamp01(preferredDistance);
            distanceTolerance = Mathf.Clamp(distanceTolerance, 0.01f, 1f);
        }
    }

    internal struct NightShadeSwordActionDebugEntry
    {
        internal NightShadeSwordActionId ActionId;
        internal bool CanStart;
        internal NightShadeSwordActionRejectReason RejectReason;
        internal NightShadeSwordActionScore Score;
        internal bool IsSelected;
    }

    internal sealed class NightShadeSwordCombatDebug
    {
        internal const int CandidateCapacity = 4;

        private readonly NightShadeSwordActionDebugEntry[] candidates =
            new NightShadeSwordActionDebugEntry[CandidateCapacity];

        internal NightShadeSwordStateId TopState { get; set; }
        internal NightShadeSwordCombatPhase CombatPhase { get; set; }
        internal NightShadeSwordActionId CurrentAction { get; set; }
        internal NightShadeSwordActionStopReason CurrentActionStopReason { get; set; }
        internal NightShadeSwordCombatPhase LastEvaluatedPhase { get; set; }
        internal NightShadeSwordActionId SelectedAction { get; set; }
        internal NightShadeSwordActionStopReason PreviousActionStopReason { get; set; }
        internal int CandidateCount { get; private set; }
        internal NightShadeSwordActionDebugEntry[] Candidates => candidates;

        internal void BeginEvaluation(
            NightShadeSwordCombatPhase phase,
            int candidateCount)
        {
            LastEvaluatedPhase = phase;
            SelectedAction = NightShadeSwordActionId.None;
            CandidateCount = Mathf.Clamp(
                candidateCount,
                0,
                CandidateCapacity);
            for (int index = 0; index < CandidateCount; index++)
            {
                candidates[index] = default;
            }
        }

        internal void SetCandidate(
            int index,
            NightShadeSwordActionId actionId,
            bool canStart,
            NightShadeSwordActionRejectReason rejectReason,
            in NightShadeSwordActionScore score)
        {
            if ((uint)index >= (uint)CandidateCount)
            {
                return;
            }

            candidates[index].ActionId = actionId;
            candidates[index].CanStart = canStart;
            candidates[index].RejectReason = rejectReason;
            candidates[index].Score = score;
            candidates[index].IsSelected = false;
        }

        internal void SelectCandidate(int index)
        {
            if ((uint)index >= (uint)CandidateCount)
            {
                return;
            }

            candidates[index].IsSelected = true;
            SelectedAction = candidates[index].ActionId;
        }

        internal void Reset()
        {
            TopState = NightShadeSwordStateId.Idle;
            CombatPhase = NightShadeSwordCombatPhase.None;
            CurrentAction = NightShadeSwordActionId.None;
            CurrentActionStopReason = NightShadeSwordActionStopReason.None;
            LastEvaluatedPhase = NightShadeSwordCombatPhase.None;
            SelectedAction = NightShadeSwordActionId.None;
            PreviousActionStopReason = NightShadeSwordActionStopReason.None;
            CandidateCount = 0;
        }
    }

    internal sealed class UnityNightShadeSwordRandomProvider : INightShadeSwordRandomProvider
    {
        public float Next01()
        {
            return UnityEngine.Random.value;
        }
    }
}
