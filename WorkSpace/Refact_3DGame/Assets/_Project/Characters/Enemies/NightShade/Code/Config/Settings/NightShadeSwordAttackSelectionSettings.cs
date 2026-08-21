using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 실행 가능한 공격 후보들의 Utility 점수를 계산할 때 사용하는 공통 값이다.
    [Serializable]
    internal sealed class NightShadeSwordAttackSelectionSettings
    {
        [SerializeField, Range(0f, 1f)] private float distanceScoreWeight = 0.55f;
        [SerializeField, Range(0f, 1f)] private float repeatPenalty = 0.25f;
        [SerializeField, Range(0f, 0.05f)] private float randomBonusMax = 0.05f;

        internal float DistanceScoreWeight => distanceScoreWeight;
        internal float RepeatPenalty => repeatPenalty;
        internal float RandomBonusMax => randomBonusMax;

        internal NightShadeSwordAttackSelectionSettings()
        {
        }

        internal NightShadeSwordAttackSelectionSettings(
            float distanceScoreWeight,
            float repeatPenalty,
            float randomBonusMax)
        {
            this.distanceScoreWeight = distanceScoreWeight;
            this.repeatPenalty = repeatPenalty;
            this.randomBonusMax = randomBonusMax;
        }

        internal void Validate()
        {
            distanceScoreWeight = Mathf.Clamp01(distanceScoreWeight);
            repeatPenalty = Mathf.Clamp01(repeatPenalty);
            randomBonusMax = Mathf.Clamp(randomBonusMax, 0f, 0.05f);
        }
    }
}
