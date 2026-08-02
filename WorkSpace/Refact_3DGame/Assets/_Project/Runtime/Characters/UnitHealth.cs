using System;

namespace rudIsland.RPG3D.Characters
{
    // 캐릭터의 현재 체력과 사망 알림만 관리한다.
    public sealed class UnitHealth
    {
        public event Action<UnitHealth> HealthChanged; // 씬 또는 시스템 참조
        public event Action Died; // 내부에서 사용하는 값

        public float MaxHealth { get; } // 최대 체력
        public float CurrentHealth { get; private set; } // 현재 체력
        public bool IsDead => CurrentHealth <= 0f; // 기능 사용 여부

        public UnitHealth(float maxHealth)
        {
            if (maxHealth <= 0f ||
                float.IsNaN(maxHealth) ||
                float.IsInfinity(maxHealth))
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0f ||
                float.IsNaN(damage) ||
                float.IsInfinity(damage) ||
                IsDead)
            {
                return;
            }

            float nextHealth = Math.Max(0f, CurrentHealth - damage);
            if (nextHealth >= CurrentHealth)
            {
                return;
            }

            CurrentHealth = nextHealth;
            HealthChanged?.Invoke(this);

            if (IsDead)
            {
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f ||
                float.IsNaN(amount) ||
                float.IsInfinity(amount) ||
                IsDead)
            {
                return;
            }

            float nextHealth = Math.Min(MaxHealth, CurrentHealth + amount);
            if (nextHealth <= CurrentHealth)
            {
                return;
            }

            CurrentHealth = nextHealth;
            HealthChanged?.Invoke(this);
        }

        public void Reset()
        {
            if (CurrentHealth >= MaxHealth)
            {
                return;
            }

            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(this);
        }

        internal void ClearListeners()
        {
            HealthChanged = null;
            Died = null;
        }
    }
}
