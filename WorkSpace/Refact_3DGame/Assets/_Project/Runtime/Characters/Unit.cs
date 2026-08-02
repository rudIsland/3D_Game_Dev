using rudIsland.RPG3D.World;

namespace rudIsland.RPG3D.Characters
{
    // 살아 있는 캐릭터가 공통으로 가지는 팀과 체력만 제공한다.
    public abstract class Unit : WorldObject
    {
        // 이동, 공격, AI는 넣지 않고 팀과 체력만 공통으로 보관한다.
        public UnitTeam Team { get; }
        public UnitHealth Health { get; }
        public bool IsDead => Health.IsDead;

        protected Unit(UnitTeam team, float maxHealth)
        {
            Team = team;
            Health = new UnitHealth(maxHealth);
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
