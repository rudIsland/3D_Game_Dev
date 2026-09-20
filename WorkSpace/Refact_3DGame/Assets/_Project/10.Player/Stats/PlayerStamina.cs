using System;
using UnityEngine;

namespace Characters.Player.Stats
{
    // 플레이어 Stamina의 소비, 회복 대기와 회복량을 관리한다.
    public sealed class PlayerStamina
    {
        private float maxStamina;
        private readonly float recoverDelay;
        private readonly float recoverSpeed;
        private float recoverElapsedTime;

        public event Action<PlayerStamina> StaminaChanged;

        public float CurrentStamina { get; private set; }
        public float MaxStamina => maxStamina;

        public PlayerStamina(
            float maxStamina,
            float recoverDelay,
            float recoverSpeed)
        {
            this.maxStamina = Mathf.Max(1f, maxStamina);
            this.recoverDelay = Mathf.Max(0f, recoverDelay);
            this.recoverSpeed = Mathf.Max(0f, recoverSpeed);
            CurrentStamina = this.maxStamina;
        }

        public bool TryConsume(float staminaCost)
        {
            staminaCost = Mathf.Max(0f, staminaCost);
            if (staminaCost <= 0f)
            {
                return true;
            }

            if (CurrentStamina < staminaCost)
            {
                return false;
            }

            recoverElapsedTime = 0f;
            SetCurrentStamina(CurrentStamina - staminaCost);
            return true;
        }

        public bool TryConsumeGuard(float staminaDamage)
        {
            staminaDamage = Mathf.Max(0f, staminaDamage);
            if (staminaDamage <= 0f)
            {
                return true;
            }

            recoverElapsedTime = 0f;
            if (CurrentStamina <= staminaDamage)
            {
                SetCurrentStamina(0f);
                return false;
            }

            SetCurrentStamina(CurrentStamina - staminaDamage);
            return true;
        }

        public bool CanConsume(float staminaCost)
        {
            return staminaCost <= 0f ||
                CurrentStamina >= staminaCost;
        }

        public void UpdateRecovery(float deltaTime, float recoveryRate)
        {
            if (deltaTime <= 0f || CurrentStamina >= maxStamina)
            {
                return;
            }

            recoverElapsedTime += deltaTime;
            recoveryRate = Mathf.Clamp01(recoveryRate);
            if (recoveryRate <= 0f || recoverElapsedTime < recoverDelay)
            {
                return;
            }

            SetCurrentStamina(Mathf.Min(maxStamina, CurrentStamina + recoverSpeed * recoveryRate * deltaTime));
        }

        public void MultiplyMaximum(float multiplier)
        {
            if (multiplier <= 1f ||
                float.IsNaN(multiplier) ||
                float.IsInfinity(multiplier))
            {
                return;
            }

            float previousMaximum = maxStamina;
            maxStamina *= multiplier;
            SetCurrentStamina(
                Mathf.Min(
                    maxStamina,
                    CurrentStamina + maxStamina - previousMaximum));
        }

        private void SetCurrentStamina(float nextStamina)
        {
            if (CurrentStamina == nextStamina)
            {
                return;
            }

            CurrentStamina = nextStamina;
            StaminaChanged?.Invoke(this);
        }
    }
}
