using System;

namespace rudIsland.RPG3D.Characters
{
    // 캐릭터의 체력 값과 사망 알림만 관리한다.
    public sealed class UnitHealth
    {
        public event Action Died;

        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        // 생성할 때 최대 체력만 받아 현재 체력을 가득 채운다.
        public UnitHealth(float maxHealth)
        {
            if (maxHealth <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth),
                    "최대 체력은 0보다 커야 합니다.");
            }

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        // 살아 있을 때만 피해를 적용하고 0이 되면 사망을 한 번 알린다.
        public void TakeDamage(float damage)
        {
            if (damage <= 0f || IsDead)
            {
                return;
            }

            CurrentHealth = Math.Max(0f, CurrentHealth - damage);

            if (IsDead)
            {
                Died?.Invoke();
            }
        }

        // 사망하지 않은 캐릭터만 최대 체력 범위 안에서 회복한다.
        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        // 풀에서 다시 사용하는 캐릭터의 체력을 최대치로 되돌린다.
        public void Reset()
        {
            CurrentHealth = MaxHealth;
        }

        // Unit이 해제될 때 외부 이벤트 연결도 함께 끊는다.
        internal void ClearDiedListeners()
        {
            Died = null;
        }
    }
}
