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
            if (!IsPositiveFinite(staggerDamage))
            {
                return false;
            }

            float nextStagger = CurrentStagger + staggerDamage;
            if (nextStagger >= StaggerLimit)
            {
                CurrentStagger = 0f;
                remainingRecoverDelay = 0f;
                return true;
            }

            CurrentStagger = nextStagger;
            remainingRecoverDelay = staggerRecoverDelay;
            return false;
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
