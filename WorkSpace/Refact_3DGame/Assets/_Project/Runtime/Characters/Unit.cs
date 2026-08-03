using rudIsland.RPG3D.World;
using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Characters
{
    // 타겟을 사용하는 코드가 구체적인 유닛 종류를 몰라도 사망 여부를 확인하게 한다.
    public interface IUnitDeathState
    {
        bool IsDead { get; }
    }

    // 살아 있는 캐릭터가 공통으로 가지는 팀과 체력만 제공한다.
    public abstract class Unit : WorldObject, IUnitDeathState
    {
        // 이동, 공격, AI는 넣지 않고 팀과 체력만 공통으로 보관한다.
        public UnitTeam Team { get; } // 씬 또는 시스템 참조
        public UnitHealth Health { get; } // 씬 또는 시스템 참조
        public bool IsDead => Health.IsDead; // 기능 사용 여부

        protected Unit(UnitTeam team, float maxHealth)
        {
            Team = team;
            Health = new UnitHealth(maxHealth);
        }

        // 모든 Unit이 같은 순서로 팀, 사망과 체력 피해 결과를 판단한다.
        protected AttackHitResult ApplyHealthHit(in AttackHitData hit)
        {
            if (IsDead ||
                hit.AttackerTeam == Team ||
                !hit.Damage.IsValid)
            {
                return AttackHitResult.Ignored;
            }

            float healthBeforeDamage = Health.CurrentHealth;
            Health.TakeDamage(hit.Damage.HealthDamage);

            if (Health.CurrentHealth >= healthBeforeDamage)
            {
                return AttackHitResult.Ignored;
            }

            return IsDead
                ? AttackHitResult.Killed
                : AttackHitResult.Damaged;
        }

        // 체력 피해를 먼저 적용하고, 살아 있으면 경직 한계 도달 여부를 판단한다.
        protected AttackHitResult ApplyHealthAndStaggerHit(
            in AttackHitData hit,
            UnitStagger unitStagger)
        {
            AttackHitResult healthResult = ApplyHealthHit(in hit);
            if (healthResult != AttackHitResult.Damaged ||
                unitStagger == null)
            {
                return healthResult;
            }

            return unitStagger.AddStaggerDamage(hit.StaggerDamage)
                ? AttackHitResult.Staggered
                : AttackHitResult.Damaged;
        }

        // WorldObject의 호출 순서를 유지하면서 Unit 전용 확장 지점으로 전달한다.
        protected sealed override void OnCreate()
        {
            OnUnitCreate();
        }

        protected sealed override void OnEnable()
        {
            OnUnitEnable();
        }

        protected sealed override void OnTick(float deltaTime)
        {
            OnUnitTick(deltaTime);
        }

        protected sealed override void OnDisable()
        {
            OnUnitDisable();
        }

        protected sealed override void OnDispose()
        {
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

        protected virtual void OnUnitTick(float deltaTime)
        {
        }

        protected virtual void OnUnitDisable()
        {
        }

        protected virtual void OnUnitDispose()
        {
        }
    }
}
