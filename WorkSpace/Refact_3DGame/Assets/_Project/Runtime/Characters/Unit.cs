using rudIsland.RPG3D.Combat;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Characters
{
    // 타겟을 사용하는 코드가 구체적인 유닛 클래스를 몰라도 사망 여부를 확인하게 한다.
    public interface IUnitDeathState
    {
        bool IsDead { get; }
    }

    // 공통 자원을 소유하고 공격 결과의 계산·반영 순서를 고정한다.
    public abstract class Unit : WorldObject, IUnitDeathState
    {
        public UnitTeam Team { get; } // 씬 또는 시스템 참조
        public UnitHealth Health { get; } // 씬 또는 시스템 참조
        public UnitStagger Stagger { get; } // 현재 경직 누적값과 회복 규칙
        public UnitStamina Stamina { get; } // 현재 Stamina와 회복 규칙
        public UnitDefenseStatus DefenseStatus { get; } // 현재 방어 상태
        public int ActivationSequence { get; private set; } // 풀 활성화 순번
        public bool IsDead => Health.IsDead; // 기능 사용 여부
        public bool CanTakeHit => IsEnabled && !IsDead; // 기능 사용 여부

        private readonly AttackHitResultCalculator hitResultCalculator;

        protected Unit(UnitTeam team, float maxHealth)
            : this(
                team,
                maxHealth,
                1f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f)
        {
        }

        protected Unit(
            UnitTeam team,
            float maxHealth,
            float staggerLimit,
            float staggerRecoverDelay,
            float staggerRecoverSpeed,
            float maxStamina,
            float staminaRecoverDelay,
            float staminaRecoverSpeed,
            float guardAngle)
        {
            Team = team;
            Health = new UnitHealth(maxHealth);
            Stagger = new UnitStagger(
                staggerLimit,
                staggerRecoverDelay,
                staggerRecoverSpeed);
            Stamina = new UnitStamina(
                maxStamina,
                staminaRecoverDelay,
                staminaRecoverSpeed);
            DefenseStatus = new UnitDefenseStatus(guardAngle);
            hitResultCalculator = new AttackHitResultCalculator();
        }

        // 계산 결과를 한 번 반영하고 파생 상태머신에 전달한다.
        public AttackHitResult ReceiveAttackHit(
            in AttackHitInput hit,
            Vector3 targetForward)
        {
            AttackHitResult result = hitResultCalculator.CalculateResult(
                in hit,
                this,
                targetForward);
            ApplyAttackHitResult(in result);
            HandleAttackHitResult(in result);
            return result;
        }

        protected void ApplyAttackHitResult(in AttackHitResult result)
        {
            if (result.HealthDamage > 0f)
            {
                Health.TakeDamage(result.HealthDamage);
            }

            if (result.StaminaDamage > 0f)
            {
                Stamina.Spend(result.StaminaDamage);
            }

            if (result.StaggerDamage > 0f)
            {
                Stagger.ApplyConfirmedDamage(
                    result.StaggerDamage,
                    result.Type == AttackHitResultType.Staggered ||
                    result.Type == AttackHitResultType.KnockedDown);
            }
        }

        // WorldObject의 호출 순서를 유지하면서 Unit 전용 확장 지점으로 전달한다.
        protected sealed override void OnCreate()
        {
            OnUnitCreate();
        }

        protected sealed override void OnEnable()
        {
            IncreaseActivationSequence();
            DefenseStatus.Reset();
            Stagger.Reset();
            OnUnitResourceEnable();
            OnUnitEnable();
        }

        protected sealed override void OnTick(float deltaTime)
        {
            if (!IsDead)
            {
                Stagger.Update(deltaTime);
                Stamina.Update(deltaTime, CanRecoverStamina());
            }

            OnUnitTick(deltaTime);
        }

        protected sealed override void OnDisable()
        {
            DefenseStatus.Reset();
            Stagger.Reset();
            OnUnitDisable();
        }

        protected sealed override void OnDispose()
        {
            DefenseStatus.Reset();
            OnUnitDispose();
            Health.ClearListeners();
        }

        // 플레이어와 적은 필요한 단계만 아래 메서드에서 구현한다.
        protected virtual void OnUnitCreate()
        {
        }

        protected virtual void OnUnitEnable()
        {
        }

        protected virtual void OnUnitResourceEnable()
        {
        }

        protected virtual void OnUnitTick(float deltaTime)
        {
        }

        protected virtual void OnUnitDisable()
        {
        }

        protected virtual void OnUnitDispose()
        {
        }

        protected virtual void HandleAttackHitResult(
            in AttackHitResult result)
        {
        }

        protected virtual bool CanRecoverStamina()
        {
            return true;
        }

        private void IncreaseActivationSequence()
        {
            ActivationSequence = ActivationSequence == int.MaxValue
                ? 1
                : ActivationSequence + 1;
        }
    }
}
