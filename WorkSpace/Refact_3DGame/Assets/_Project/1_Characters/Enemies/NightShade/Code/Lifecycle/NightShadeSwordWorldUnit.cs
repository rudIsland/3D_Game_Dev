using System;
using Characters.Combat;

namespace Characters.Enemies.NightShade
{
    // EnemyUnit 생명주기에서 NightShade 양손검 전투를 실행한다.
    public sealed class NightShadeSwordWorldUnit : EnemyUnit
    {
        private readonly NightShadeSwordStateMachine stateMachine;
        private readonly NightShadeSwordAttackRangeDetector attackRangeDetector;
        private readonly StopPoint stopPoint;
        private readonly CombatHitStop hitStop;

        public float CurrentHealth => Health.CurrentHealth;
        public float CurrentStagger => stopPoint.CurrentPoint;
        public float MaxStagger => stopPoint.MaxPoint;
        public bool IsInCombat => stateMachine.IsInCombat;
        internal bool IsAttackStateActive => stateMachine.IsAttackStateActive;
        internal NightShadeSwordCombatDebug CombatDebug => stateMachine.Debug;

        public event Action<NightShadeSwordWorldUnit> StaggerChanged;
        public event Action<NightShadeSwordWorldUnit> CombatStateChanged;

        internal NightShadeSwordWorldUnit(
            float maxHealth,
            NightShadeSwordStateMachine stateMachine,
            NightShadeSwordAttackRangeDetector attackRangeDetector,
            StopPoint stopPoint,
            CombatHitStop hitStop)
            : base(maxHealth)
        {
            this.stateMachine = stateMachine;
            this.attackRangeDetector = attackRangeDetector;
            this.stopPoint = stopPoint;
            this.hitStop = hitStop;
        }

        public EnemyHitResult TakeHit(in EnemyHitRequest hitRequest)
        {
            HitDamageResult damageResult =
                HitDamageCalculator.Apply(Health, hitRequest.Damage);
            if (damageResult == HitDamageResult.Ignored)
            {
                return EnemyHitResult.Ignored;
            }

            hitStop.Request(hitRequest.HitStopDuration);
            if (damageResult == HitDamageResult.Killed)
            {
                return EnemyHitResult.Killed;
            }

            float appliedStopDamage = hitRequest.StaggerDamage * stateMachine.StopDamageScale;
            bool reachedStopLimit = stopPoint.TryAccumulate(appliedStopDamage);
            HitReaction reaction = NightShadeSwordHitReactionSelector.Select(
                hitRequest.Strength,
                reachedStopLimit,
                stateMachine.ProtectsSmallHit);
            var hitResult = new EnemyHitResult(
                HitDamageResult.Damaged,
                reaction);

            stateMachine.NotifyDamaged();
            if (appliedStopDamage > 0f)
            {
                StaggerChanged?.Invoke(this);
            }

            if (reaction != HitReaction.None)
            {
                stateMachine.ChangeToHitState(
                    reaction,
                    in hitRequest);
            }

            return hitResult;
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
            stopPoint.Reset();
            stateMachine.Enable();
        }

        protected override void OnUnitTick(float deltaTime)
        {
            bool isHitStopActive = hitStop.Update(deltaTime);
            stateMachine.Update(
                isHitStopActive ? 0f : deltaTime,
                isHitStopActive);
            if (isHitStopActive)
            {
                return;
            }

            if (stopPoint.UpdateRecovery(deltaTime))
            {
                StaggerChanged?.Invoke(this);
            }

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
