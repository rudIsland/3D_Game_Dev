using rudIsland.RPG3D.Characters;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // EnemyUnit 생명주기에서 Nightshade 상태머신과 체력을 연결한다.
    public sealed class NightshadeSpearWorldUnit : EnemyUnit
    {
        private readonly NightshadeSpearStateMachine stateMachine; // 현재 행동 상태

        public float CurrentHealth => Health.CurrentHealth; // 현재 체력
        public int CurrentPhase => stateMachine.CurrentPhase;

        public NightshadeSpearWorldUnit(
            float maxHealth,
            NightshadeSpearStateMachine stateMachine)
            : base(maxHealth)
        {
            this.stateMachine = stateMachine;
        }

        public void TakeDamage(
            float damage,
            Vector3 hitPosition)
        {
            float healthBeforeDamage = Health.CurrentHealth;
            Health.TakeDamage(damage);

            if (Health.CurrentHealth >= healthBeforeDamage || IsDead) return;
            stateMachine.SetHealthRatio(
                Health.CurrentHealth / Health.MaxHealth);
            stateMachine.ChangeToHitState(hitPosition);
        }

        protected override void OnUnitCreate()
        {
            Health.Died += HandleHealthDied;
        }

        protected override void OnEnemyEnable()
        {
            stateMachine.Enable();
        }

        protected override void OnUnitTick(float deltaTime)
        {
            stateMachine.Update(deltaTime);
        }
        protected override void OnUnitDisable() => stateMachine.Disable();

        protected override void OnUnitDispose()
        {
            Health.Died -= HandleHealthDied;
        }

        private void HandleHealthDied() => stateMachine.ChangeToDeadState();
    }
}
