using System;
using UnityEngine;

namespace Characters.Enemies.NightShade
{
    [Serializable]
    internal sealed class NightShadeSwordRecoverySettings
    {
        [Header("이동")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 2f;
        [SerializeField, Min(0.1f)] private float moveDuration = 0.6f;

        [Header("선택 점수")]
        [SerializeField, Range(0f, 1f)] private float idleBaseScore = 0.35f;
        [SerializeField, Range(0f, 1f)] private float idleDistanceWeight = 0.35f;
        [SerializeField, Range(0f, 1f)] private float backBaseScore = 0.25f;
        [SerializeField, Range(0f, 1f)] private float backCloseWeight = 0.65f;
        [SerializeField, Range(0f, 1f)] private float sideBaseScore = 0.35f;
        [SerializeField, Range(0f, 1f)] private float sideDistanceWeight = 0.35f;
        [SerializeField, Range(0f, 1f)] private float repeatPenalty = 0.20f;
        [SerializeField, Range(0f, 0.05f)] private float randomBonusMax = 0.05f;

        internal float MoveSpeed => moveSpeed;
        internal float MoveDuration => moveDuration;
        internal float IdleBaseScore => idleBaseScore;
        internal float IdleDistanceWeight => idleDistanceWeight;
        internal float BackBaseScore => backBaseScore;
        internal float BackCloseWeight => backCloseWeight;
        internal float SideBaseScore => sideBaseScore;
        internal float SideDistanceWeight => sideDistanceWeight;
        internal float RepeatPenalty => repeatPenalty;
        internal float RandomBonusMax => randomBonusMax;

        internal NightShadeSwordRecoverySettings()
        {
        }

        internal NightShadeSwordRecoverySettings(
            float moveSpeed,
            float moveDuration,
            float idleBaseScore,
            float idleDistanceWeight,
            float backBaseScore,
            float backCloseWeight,
            float sideBaseScore,
            float sideDistanceWeight,
            float repeatPenalty,
            float randomBonusMax)
        {
            this.moveSpeed = moveSpeed;
            this.moveDuration = moveDuration;
            this.idleBaseScore = idleBaseScore;
            this.idleDistanceWeight = idleDistanceWeight;
            this.backBaseScore = backBaseScore;
            this.backCloseWeight = backCloseWeight;
            this.sideBaseScore = sideBaseScore;
            this.sideDistanceWeight = sideDistanceWeight;
            this.repeatPenalty = repeatPenalty;
            this.randomBonusMax = randomBonusMax;
        }

        internal void Validate()
        {
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            moveDuration = Mathf.Max(0.1f, moveDuration);
            idleBaseScore = Mathf.Clamp01(idleBaseScore);
            idleDistanceWeight = Mathf.Clamp01(idleDistanceWeight);
            backBaseScore = Mathf.Clamp01(backBaseScore);
            backCloseWeight = Mathf.Clamp01(backCloseWeight);
            sideBaseScore = Mathf.Clamp01(sideBaseScore);
            sideDistanceWeight = Mathf.Clamp01(sideDistanceWeight);
            repeatPenalty = Mathf.Clamp01(repeatPenalty);
            randomBonusMax = Mathf.Clamp(randomBonusMax, 0f, 0.05f);
        }
    }
}
