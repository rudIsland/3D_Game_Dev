using System;
using Characters.Combat;
using UnityEngine;

namespace Characters.Enemies.Zombie
{
    // EnemyUnit 생명주기에서 좀비의 탐지·추적 상태머신을 실행한다.
    public sealed class ZombieWorldUnit : EnemyUnit
    {
        private readonly ZombieStateMachine stateMachine; // 현재 행동 상태
        private readonly ZombieAttackRangeDetector attackRangeDetector;
        private readonly StopPoint stopPoint;
        private readonly CombatHitStop hitStop;
        private readonly float homeRecoveryDelay;
        private readonly float healthRecoverySpeed;
        private readonly float staggerRecoverySpeed;

        private float homeRecoveryElapsedTime;

        public float CurrentHealth => Health.CurrentHealth; // 현재 체력
        public float CurrentStagger => stopPoint.CurrentPoint;
        public float MaxStagger => stopPoint.MaxPoint;
        public bool IsInCombat => stateMachine.IsInCombat;

        public event Action<ZombieWorldUnit> StaggerChanged;
        public event Action<ZombieWorldUnit> CombatStateChanged;

        internal ZombieWorldUnit(
            float maxHealth,
            ZombieStateMachine stateMachine,
            ZombieAttackRangeDetector attackRangeDetector,
            StopPoint stopPoint,
            CombatHitStop hitStop,
            float homeRecoveryDelay,
            float healthRecoverySpeed,
            float staggerRecoverySpeed)
            : base(maxHealth)
        {
            this.stateMachine = stateMachine;
            this.attackRangeDetector = attackRangeDetector;
            this.stopPoint = stopPoint;
            this.hitStop = hitStop;
            this.homeRecoveryDelay = Mathf.Max(0f, homeRecoveryDelay);
            this.healthRecoverySpeed = Mathf.Max(0f, healthRecoverySpeed);
            this.staggerRecoverySpeed = Mathf.Max(0f, staggerRecoverySpeed);
        }

        public EnemyHitResult TakeHit(in EnemyHitRequest hitRequest)
        {
            HitDamageResult damageResult =
                HitDamageCalculator.Apply(Health, hitRequest.Damage);
            if (damageResult == HitDamageResult.Ignored)
            {
                return EnemyHitResult.Ignored;
            }

            homeRecoveryElapsedTime = 0f;
            hitStop.Request(hitRequest.HitStopDuration);
            if (damageResult == HitDamageResult.Killed)
            {
                return EnemyHitResult.Killed;
            }

            bool reachedStopLimit =
                stopPoint.TryAccumulate(hitRequest.StaggerDamage);
            HitReaction reaction = HitReactionSelector.Select(
                hitRequest.Strength,
                reachedStopLimit,
                false,
                false,
                false);
            var hitResult = new EnemyHitResult(
                HitDamageResult.Damaged,
                reaction);

            stateMachine.NotifyDamaged();
            if (hitRequest.StaggerDamage > 0f)
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

        internal void NotifyAttackAnimationEnded()
        {
            stateMachine.NotifyAttackAnimationEnded();
        }

        internal bool BeginAttackHit()
        {
            return stateMachine.BeginAttackHit();
        }

        internal bool BeginAttackRecovery()
        {
            return stateMachine.BeginAttackRecovery();
        }

        internal void NotifyAlertAnimationEnded()
        {
            stateMachine.NotifyAlertAnimationEnded();
        }

        protected override void OnUnitCreate()
        {
            Health.Died += HandleHealthDied;
            stateMachine.CombatStateChanged +=
                HandleCombatStateChanged;
        }

        protected override void OnEnemyEnable()
        {
            hitStop.Reset();
            attackRangeDetector.Close();
            stopPoint.Reset();
            homeRecoveryElapsedTime = 0f;
            stateMachine.Enable();
        }

        protected override void OnUnitTick(float deltaTime)
        {
            if (hitStop.Update(deltaTime))
            {
                return;
            }

            stateMachine.Update(deltaTime);
            attackRangeDetector.Tick();
            UpdateHomeRecovery(deltaTime);
        }

        protected override void OnUnitDisable()
        {
            hitStop.Reset();
            homeRecoveryElapsedTime = 0f;
            stateMachine.Disable();
            attackRangeDetector.Close();
        }

        protected override void OnUnitDispose()
        {
            Health.Died -= HandleHealthDied;
            stateMachine.CombatStateChanged -=
                HandleCombatStateChanged;
        }

        private void HandleHealthDied()
        {
            stateMachine.ChangeToDeadState();
        }

        private void HandleCombatStateChanged()
        {
            if (stateMachine.IsInCombat)
            {
                homeRecoveryElapsedTime = 0f;
            }

            CombatStateChanged?.Invoke(this);
        }

        private void UpdateHomeRecovery(float deltaTime)
        {
            if (!stateMachine.CanRecoverAtHome || deltaTime <= 0f)
            {
                homeRecoveryElapsedTime = 0f;
                return;
            }

            float previousElapsedTime = homeRecoveryElapsedTime;
            homeRecoveryElapsedTime += deltaTime;
            if (homeRecoveryElapsedTime <= homeRecoveryDelay)
            {
                return;
            }

            float recoveryStartTime = Mathf.Max(
                previousElapsedTime,
                homeRecoveryDelay);
            float recoveryDuration =
                homeRecoveryElapsedTime - recoveryStartTime;

            Health.Heal(healthRecoverySpeed * recoveryDuration);
            if (stopPoint.Recover(staggerRecoverySpeed * recoveryDuration))
            {
                StaggerChanged?.Invoke(this);
            }
        }
    }
}
