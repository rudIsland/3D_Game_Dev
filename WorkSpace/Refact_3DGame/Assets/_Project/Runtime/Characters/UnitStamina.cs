using System;

namespace rudIsland.RPG3D.Characters
{
    // Unit의 현재 Stamina와 회복 대기 시간을 관리한다.
    public sealed class UnitStamina
    {
        private readonly float recoverDelay;
        private readonly float recoverSpeed;
        private float remainingRecoverDelay;

        public float MaxStamina { get; }
        public float CurrentStamina { get; private set; }

        public UnitStamina(
            float maxStamina,
            float recoverDelay,
            float recoverSpeed)
        {
            ThrowIfNegativeFinite(maxStamina, nameof(maxStamina));
            ThrowIfNegativeFinite(recoverDelay, nameof(recoverDelay));
            ThrowIfNegativeFinite(recoverSpeed, nameof(recoverSpeed));

            MaxStamina = maxStamina;
            this.recoverDelay = recoverDelay;
            this.recoverSpeed = recoverSpeed;
            Reset();
        }

        public bool CanSpend(float amount)
        {
            return IsPositiveFinite(amount) &&
                CurrentStamina >= amount;
        }

        public void Spend(float amount)
        {
            if (!CanSpend(amount))
            {
                return;
            }

            CurrentStamina -= amount;
            remainingRecoverDelay = recoverDelay;
        }

        public void Update(float deltaTime, bool canRecover)
        {
            if (!canRecover ||
                CurrentStamina >= MaxStamina ||
                !IsPositiveFinite(deltaTime) ||
                !IsPositiveFinite(recoverSpeed))
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

            CurrentStamina = Math.Min(
                MaxStamina,
                CurrentStamina + recoverSpeed * recoverTime);
        }

        public void Reset()
        {
            CurrentStamina = MaxStamina;
            remainingRecoverDelay = 0f;
        }

        private static void ThrowIfNegativeFinite(
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
