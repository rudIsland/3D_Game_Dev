using System;
using Characters.Combat;

namespace Characters.Enemies.NightShade
{
    // EnemyUnit 생명주기에서 NightShade 양손검 전투를 실행한다.
    public sealed class NightShadeSwordWorldUnit : EnemyUnit, IEnemyCombatStatus
    {
        private readonly NightShadeSwordStateMachine stateMachine;
        private readonly NightShadeSwordAttackRangeDetector attackRangeDetector;
        private readonly StopPoint stopPoint;
        private readonly CombatHitStop hitStop;
        private readonly Action beginBattle;
        private readonly Action restoreBattlePosition;
        private bool isHitStopActive;

        public float CurrentHealth => Health.CurrentHealth;
        public string DisplayName => "NIGHTSHADE";
        public bool ShowScreenHealthBar => true;
        public float CurrentStagger => stopPoint.CurrentPoint;
        public float MaxStagger => stopPoint.MaxPoint;
        public bool IsInCombat => stateMachine.IsInCombat;
        internal bool IsAttackStateActive => stateMachine.IsAttackStateActive;
        internal NightShadeSwordCombatDebug CombatDebug => stateMachine.Debug;

        public event Action<IEnemyCombatStatus> StaggerChanged;
        public event Action<IEnemyCombatStatus> CombatStateChanged;

        internal NightShadeSwordWorldUnit(
            float maxHealth,
            NightShadeSwordStateMachine stateMachine,
            NightShadeSwordAttackRangeDetector attackRangeDetector,
            StopPoint stopPoint,
            CombatHitStop hitStop,
            Action beginBattle = null,
            Action restoreBattlePosition = null)
            : base(maxHealth)
        {
            this.stateMachine = stateMachine;
            this.attackRangeDetector = attackRangeDetector;
            this.stopPoint = stopPoint;
            this.hitStop = hitStop;
            this.beginBattle = beginBattle;
            this.restoreBattlePosition = restoreBattlePosition;
        }

        public EnemyHitResult TakeHit(in EnemyHitRequest hitRequest)
        {
            if (!IsEnabled)
                return EnemyHitResult.Ignored;
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
            beginBattle?.Invoke();
            isHitStopActive = false;
            hitStop.Reset();
            attackRangeDetector.Close();
            stopPoint.Reset();
            stateMachine.Enable();
        }

        protected override void OnUnitTick(float deltaTime)
        {
            isHitStopActive = hitStop.Update(deltaTime);
            stateMachine.Update(
                isHitStopActive ? 0f : deltaTime,
                isHitStopActive);
            if (stateMachine.NeedsBattleReset)
            {
                ResetBattle();
                return;
            }
            if (isHitStopActive)
            {
                return;
            }

            if (stopPoint.UpdateRecovery(deltaTime))
            {
                StaggerChanged?.Invoke(this);
            }

        }

        internal void LateUpdate()
        {
            if (!IsEnabled || IsDead || isHitStopActive || stateMachine.HasPendingReaction)
                return;
            stateMachine.ProcessAnimationEvents();
            attackRangeDetector.Tick();
        }

        public void ResetBattle()
        {
            // 처치한 보스를 재도전 초기화로 부활시키지 않는다.
            if (!IsEnabled || IsDead)
                return;
            stateMachine.Disable();
            attackRangeDetector.Close();
            hitStop.Reset();
            isHitStopActive = false;
            restoreBattlePosition?.Invoke();
            stopPoint.Reset();
            Health.Reset();
            StaggerChanged?.Invoke(this);
            stateMachine.Enable();
        }

        protected override void OnUnitDisable()
        {
            hitStop.Reset();
            stateMachine.Disable();
            attackRangeDetector.Close();
        }

        protected override void OnUnitRelease()
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
