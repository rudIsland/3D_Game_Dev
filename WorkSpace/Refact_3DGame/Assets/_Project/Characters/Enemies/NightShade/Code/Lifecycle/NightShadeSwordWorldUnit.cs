using System;
using rudIsland.RPG3D.Characters.Combat;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // EnemyUnit 생명주기에서 NightShade 양손검 전투를 실행한다.
    public sealed class NightShadeSwordWorldUnit : EnemyUnit
    {
        private readonly NightShadeSwordStateMachine stateMachine;
        private readonly NightShadeSwordAttackRangeDetector attackRangeDetector;
        private readonly NightShadeSwordStagger stagger;
        private readonly CombatHitStop hitStop;

        public float CurrentHealth => Health.CurrentHealth;
        public float CurrentStagger => stagger.CurrentStagger;
        public float MaxStagger => stagger.StaggerLimit;
        public bool IsInCombat => stateMachine.IsInCombat;
        internal bool IsAttackStateActive => stateMachine.IsAttackStateActive;

        public event Action<NightShadeSwordWorldUnit> StaggerChanged;
        public event Action<NightShadeSwordWorldUnit> CombatStateChanged;

        internal NightShadeSwordWorldUnit(
            float maxHealth,
            NightShadeSwordStateMachine stateMachine,
            NightShadeSwordAttackRangeDetector attackRangeDetector,
            NightShadeSwordStagger stagger,
            CombatHitStop hitStop)
            : base(maxHealth)
        {
            this.stateMachine = stateMachine;
            this.attackRangeDetector = attackRangeDetector;
            this.stagger = stagger;
            this.hitStop = hitStop;
        }

        public EnemyHitResult TakeHit(in EnemyHitRequest hitRequest)
        {
            float healthBeforeDamage = Health.CurrentHealth;
            Health.TakeDamage(hitRequest.Damage);
            if (Health.CurrentHealth >= healthBeforeDamage)
            {
                return EnemyHitResult.Ignored;
            }

            hitStop.Request(hitRequest.HitStopDuration);
            if (IsDead)
            {
                return EnemyHitResult.Damaged;
            }

            stateMachine.NotifyDamaged();
            bool shouldEnterHitState =
                stagger.TryAccumulate(hitRequest.StaggerDamage);
            if (hitRequest.StaggerDamage > 0f)
            {
                StaggerChanged?.Invoke(this);
            }

            if (!shouldEnterHitState)
            {
                return EnemyHitResult.Damaged;
            }

            stateMachine.ChangeToHitState(in hitRequest);
            return EnemyHitResult.Staggered;
        }

        internal void StopAttackTurnAnimationEvent()
        {
            stateMachine.StopAttackTurnAnimationEvent();
        }

        internal void PlayAttackSoundAnimationEvent(int hitIndex)
        {
            stateMachine.PlayAttackSoundAnimationEvent(hitIndex);
        }

        internal void OpenAttackHitAnimationEvent(int hitIndex)
        {
            stateMachine.OpenAttackHitAnimationEvent(hitIndex);
        }

        internal void CloseAttackHitAnimationEvent()
        {
            stateMachine.CloseAttackHitAnimationEvent();
        }

        protected override void OnUnitCreate()
        {
            Health.Died += HandleHealthDied;
            stateMachine.CombatStateChanged += HandleCombatStateChanged;
        }

        protected override void OnEnemyEnable()
        {
            hitStop.Reset();
            attackRangeDetector.Close();
            stagger.Reset();
            stateMachine.Enable();
        }

        protected override void OnUnitTick(float deltaTime)
        {
            if (hitStop.Update(deltaTime))
            {
                return;
            }

            if (stagger.UpdateRecovery(deltaTime))
            {
                StaggerChanged?.Invoke(this);
            }

            stateMachine.Update(deltaTime);
            attackRangeDetector.Tick();
        }

        protected override void OnUnitDisable()
        {
            hitStop.Reset();
            stateMachine.Disable();
            attackRangeDetector.Close();
        }

        protected override void OnUnitDispose()
        {
            Health.Died -= HandleHealthDied;
            stateMachine.CombatStateChanged -= HandleCombatStateChanged;
        }

        private void HandleHealthDied()
        {
            stateMachine.ChangeToDeadState();
        }

        private void HandleCombatStateChanged()
        {
            CombatStateChanged?.Invoke(this);
        }
    }
}
