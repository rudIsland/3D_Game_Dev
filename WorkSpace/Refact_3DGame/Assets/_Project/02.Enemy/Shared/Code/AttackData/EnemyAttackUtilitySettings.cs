using System;
using UnityEngine;

namespace Characters.Enemies.AttackData
{
    [Serializable]
    public sealed class EnemyAttackUtilitySettings
    {
        [SerializeField, Range(0f, 1f)] private float baseScore = 0.35f; // 거리 보정 전 기본 점수
        [SerializeField, Range(0f, 1f)] private float preferredDistance = 0.5f; // 0~1로 정규화한 선호 거리
        [SerializeField, Range(0.01f, 1f)] private float distanceTolerance = 0.5f; // 선호 지점의 허용폭

        public float BaseScore => baseScore;
        public float PreferredDistance => preferredDistance;
        public float DistanceTolerance => distanceTolerance;

        internal void Validate()
        {
            baseScore = Mathf.Clamp01(baseScore);
            preferredDistance = Mathf.Clamp01(preferredDistance);
            distanceTolerance = Mathf.Clamp(distanceTolerance, 0.01f, 1f);
        }
    }
}
