using System;

namespace rudIsland.RPG3D.Characters
{
    // Unit의 현재 경직 누적값과 시간에 따른 회복을 계산한다.
    public sealed class UnitStagger
    {
        private readonly float staggerRecoverDelay;
        private readonly float staggerRecoverSpeed;
        private float remainingRecoverDelay;

        public float CurrentStagger { get; private set; }
        public float StaggerLimit { get; }

        public UnitStagger(
            float staggerLimit,
            float staggerRecoverDelay,
            float staggerRecoverSpeed)
        {
            ThrowIfNotPositive(
                staggerLimit,
                nameof(staggerLimit));
            ThrowIfNegative(
                staggerRecoverDelay,
                nameof(staggerRecoverDelay));
            ThrowIfNegative(
                staggerRecoverSpeed,
                nameof(staggerRecoverSpeed));

            StaggerLimit = staggerLimit;
            this.staggerRecoverDelay = staggerRecoverDelay;
            this.staggerRecoverSpeed = staggerRecoverSpeed;
        }

        // 유효한 경직 피해를 누적하고 한계에 도달했는지를 반환한다.
        public bool AddStaggerDamage(float staggerDamage)
        {
            bool reachedLimit = WillReachLimit(staggerDamage);
            ApplyConfirmedDamage(staggerDamage, reachedLimit);
            return reachedLimit;
        }

        // 계산기가 현재 값만 읽고 경직 한계 도달 여부를 판단하게 한다.
        public bool WillReachLimit(float staggerDamage)
        {
            return IsPositiveFinite(staggerDamage) &&
                CurrentStagger + staggerDamage >= StaggerLimit;
        }

        // 계산이 끝난 경직 결과를 한 번만 반영한다.
        public void ApplyConfirmedDamage(
            float staggerDamage,
            bool reachedLimit)
        {
            if (!IsPositiveFinite(staggerDamage))
            {
                return;
            }

            if (reachedLimit)
            {
                CurrentStagger = 0f;
                remainingRecoverDelay = 0f;
                return;
            }

            CurrentStagger = Math.Min(
                StaggerLimit,
                CurrentStagger + staggerDamage);
            remainingRecoverDelay = staggerRecoverDelay;
        }

        // 마지막 경직 피해 이후 대기 시간이 끝나면 현재 경직을 줄인다.
        public void Update(float deltaTime)
        {
            if (CurrentStagger <= 0f ||
                !IsPositiveFinite(deltaTime))
            {
                return;
            }

            float recoverTime = deltaTime;
            if (remainingRecoverDelay > 0f)
            {
                if (recoverTime <= remainingRecoverDelay)
                {
                    remainingRecoverDelay -= recoverTime;
                    return;
                }

                recoverTime -= remainingRecoverDelay;
                remainingRecoverDelay = 0f;
            }

            CurrentStagger = Math.Max(
                0f,
                CurrentStagger - staggerRecoverSpeed * recoverTime);
        }

        public void Reset()
        {
            CurrentStagger = 0f;
            remainingRecoverDelay = 0f;
        }

        private static void ThrowIfNotPositive(
            float value,
            string parameterName)
        {
            if (!IsPositiveFinite(value))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void ThrowIfNegative(
            float value,
            string parameterName)
        {
            if (value < 0f ||
                float.IsNaN(value) ||
                float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f &&
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}
