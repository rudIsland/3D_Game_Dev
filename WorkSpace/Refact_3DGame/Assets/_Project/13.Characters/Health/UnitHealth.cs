using System;

namespace Characters
{
    // 캐릭터의 체력과 체력 변화 알림만 관리한다.
    public sealed class UnitHealth
    {
        // 체력이 바뀌었을 때 UI나 다른 시스템에 알린다.
        public event Action<UnitHealth> HealthChanged;

        // 체력이 0이 되었을 때 한 번 알린다.
        public event Action Died;

        // 캐릭터가 가질 수 있는 최대 체력이다.
        public float MaxHealth { get; private set; }

        // 현재 남아 있는 체력이다.
        public float CurrentHealth { get; private set; }

        // 현재 체력이 0인지 알려준다.
        public bool IsDead => CurrentHealth <= 0f;

        // 최대 체력을 기준으로 체력 값을 준비한다.
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

        // 받은 피해만큼 체력을 줄이고 체력 변화와 사망을 알린다.
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

        // 회복량만큼 체력을 올리되 최대 체력을 넘기지 않는다.
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

        // 체력을 최대 체력으로 되돌린다.
        public void Reset()
        {
            if (CurrentHealth >= MaxHealth)
            {
                return;
            }

            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(this);
        }

        public void MultiplyMaximum(float multiplier)
        {
            if (multiplier <= 1f ||
                float.IsNaN(multiplier) ||
                float.IsInfinity(multiplier) ||
                IsDead)
            {
                return;
            }

            float previousMaximum = MaxHealth;
            MaxHealth *= multiplier;
            CurrentHealth = Math.Min(
                MaxHealth,
                CurrentHealth + MaxHealth - previousMaximum);
            HealthChanged?.Invoke(this);
        }

        // 객체가 제거될 때 이벤트 구독을 끊는다.
        internal void ClearListeners()
        {
            HealthChanged = null;
            Died = null;
        }
    }
}
