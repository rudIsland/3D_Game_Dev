using System;
using rudIsland.RPG3D.Characters;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 좀비의 탐지·경계·추적·공격 상태를 관리한다.
    public sealed class ZombieStateMachine
    {
        private readonly Transform target; // 대상 참조
        private readonly IUnitDeathState targetDeathState; // 목표 사망 여부
        private readonly ZombieMovement movement; // 이동 정보
        private readonly ZombieAnimationController animation; // 씬 또는 시스템 참조
        private readonly ZombieAliveState aliveState; // 현재 행동 상태
        private readonly ZombieHitState hitState; // 피격 또는 피해 관련 값
        private readonly ZombieDeadState deadState; // 현재 행동 상태
        private readonly Action requestRelease; // 내부에서 사용하는 값
        private readonly Action endAttackHit; // 공격 관련 설정 또는 상태
        private readonly float minimumAttackFacingDot; // 공격 관련 설정 또는 상태
        private readonly AnimationCurve hitPushCurve;
        private readonly float hitPushCurveStart;
        private readonly float hitPushCurveRange;

        private IZombieState currentState; // 현재 행동 상태
        private bool isEnabled; // 기능 사용 여부
        private Vector3 targetPosition; // 대상 참조
        private float targetDistanceSquared; // 대상 참조

        internal event Action CombatStateChanged;

        internal float FindRangeSquared { get; } // 거리 설정
        internal float IdleTargetCheckInterval { get; } // 대상 참조
        internal float AttackRangeSquared { get; } // 공격 관련 설정 또는 상태
        internal float ChaseSpeed { get; } // 이동 속도
        internal float TurnSpeed { get; } // 이동 속도
        internal float HitPushDuration { get; }
        internal float DeadBodyKeepTime { get; } // 시간 설정
        internal bool IsInCombat { get; private set; }

        public ZombieStateMachine(
            Transform target,
            IUnitDeathState targetDeathState,
            ZombieMovement movement,
            ZombieAnimationController animation,
            float findRange,
            float idleTargetCheckInterval,
            float attackRange,
            float attackFacingAngle,
            float chaseSpeed,
            float turnSpeed,
            float hitPushDuration,
            AnimationCurve hitPushCurve,
            float deadBodyKeepTime,
            Action requestRelease,
            Action endAttackHit)
        {
            this.target = target;
            this.targetDeathState = targetDeathState ??
                throw new ArgumentNullException(nameof(targetDeathState));
            this.movement = movement;
            this.animation = animation;
            FindRangeSquared = findRange * findRange;
            IdleTargetCheckInterval = Mathf.Max(
                0.01f,
                idleTargetCheckInterval);
            AttackRangeSquared = attackRange * attackRange;
            minimumAttackFacingDot = Mathf.Cos(
                Mathf.Clamp(attackFacingAngle, 0f, 180f) *
                Mathf.Deg2Rad);
            ChaseSpeed = chaseSpeed;
            TurnSpeed = turnSpeed;
            HitPushDuration = Mathf.Max(0.01f, hitPushDuration);
            this.hitPushCurve = hitPushCurve ??
                AnimationCurve.Linear(0f, 0f, 1f, 1f);
            hitPushCurveStart = this.hitPushCurve.Evaluate(0f);
            hitPushCurveRange =
                this.hitPushCurve.Evaluate(1f) - hitPushCurveStart;
            DeadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
            this.requestRelease = requestRelease;
            this.endAttackHit = endAttackHit;

            aliveState = new ZombieAliveState(this);
            hitState = new ZombieHitState(this);
            deadState = new ZombieDeadState(this);
        }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            SetCombatState(false);
            aliveState.ResetTargetAwareness();
            movement.Reset();
            animation.ResetAnimation();
            ChangeState(aliveState);
        }

        public void Update(float deltaTime)
        {
            if (!isEnabled || currentState == null)
            {
                return;
            }

            if (ReferenceEquals(currentState, aliveState) &&
                IsInCombat &&
                !CanTrackTarget())
            {
                aliveState.ChangeToIdleAfterLostTarget();
            }

            if (ReferenceEquals(currentState, aliveState) &&
                aliveState.NeedsTargetUpdateEveryFrame)
            {
                UpdateTargetSnapshot();
            }

            currentState.Update(deltaTime);
        }

        public void Disable()
        {
            if (!isEnabled)
            {
                return;
            }

            currentState?.Exit();
            EndAttackHit();
            currentState = null;
            isEnabled = false;
            SetCombatState(false);
            animation.ResetAnimation();
        }

        internal bool IsTargetFound()
        {
            return CanTrackTarget() &&
                targetDistanceSquared <= FindRangeSquared;
        }

        // 목표가 활성 상태이고 살아 있을 때만 추적을 허용한다.
        internal bool CanTrackTarget()
        {
            return target != null &&
                target.gameObject.activeInHierarchy &&
                !targetDeathState.IsDead;
        }

        internal bool IsTargetInAttackRange()
        {
            return targetDistanceSquared <= AttackRangeSquared;
        }

        internal bool IsReadyToAttack()
        {
            return IsTargetInAttackRange() && IsFacingTarget();
        }

        internal bool IsFacingTarget()
        {
            return movement.IsFacing(
                targetPosition,
                minimumAttackFacingDot);
        }

        internal void MoveToTarget(float deltaTime)
        {
            movement.MoveTo(
                targetPosition,
                ChaseSpeed,
                TurnSpeed,
                deltaTime);
        }

        internal void TurnToTarget(float deltaTime)
        {
            movement.TurnTo(targetPosition, TurnSpeed, deltaTime);
        }

        internal void StayOnGround(float deltaTime)
        {
            movement.StayOnGround(deltaTime);
        }

        internal void ApplyHitMovement(
            Vector3 horizontalMovement,
            float deltaTime)
        {
            movement.ApplyHitMovement(horizontalMovement, deltaTime);
        }

        internal float EvaluateHitPushProgress(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            if (Mathf.Abs(hitPushCurveRange) <= 0.000001f)
            {
                return normalizedTime;
            }

            return Mathf.Clamp01(
                (hitPushCurve.Evaluate(normalizedTime) -
                    hitPushCurveStart) /
                hitPushCurveRange);
        }

        internal void ChangeToHitState(
            in EnemyHitRequest hitRequest)
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            EndAttackHit();
            hitState.SetHitRequest(in hitRequest);
            if (ReferenceEquals(currentState, hitState))
            {
                hitState.Restart();
                return;
            }

            ChangeState(hitState);
        }

        internal void ChangeToAliveState()
        {
            if (ReferenceEquals(currentState, deadState))
            {
                return;
            }

            ChangeState(aliveState);
        }

        internal void ChangeToDeadState()
        {
            if (!isEnabled ||
                ReferenceEquals(currentState, deadState))
            {
                return;
            }

            EndAttackHit();
            SetCombatState(false);
            ChangeState(deadState);
        }

        internal void NotifyDamaged()
        {
            SetCombatState(true);
        }

        internal void EnterCombat()
        {
            SetCombatState(true);
        }

        internal void ExitCombat()
        {
            SetCombatState(false);
        }

        internal void EndAttackHit()
        {
            endAttackHit?.Invoke();
        }

        internal bool BeginAttackHit()
        {
            return isEnabled &&
                ReferenceEquals(currentState, aliveState) &&
                aliveState.BeginAttackHit();
        }

        internal bool BeginAttackRecovery()
        {
            return isEnabled &&
                ReferenceEquals(currentState, aliveState) &&
                aliveState.BeginAttackRecovery();
        }

        internal void PlayIdle()
        {
            animation.PlayIdle();
        }

        internal void PlayAlert()
        {
            animation.PlayAlert();
        }

        internal void PlayChase()
        {
            animation.PlayChase();
        }

        internal void PlayAttack(ZombieAttackType attackType)
        {
            animation.PlayAttack(attackType);
        }

        internal void PlayHitFromStart()
        {
            animation.PlayHitFromStart();
        }

        internal void PlayDead()
        {
            animation.PlayDead();
        }

        internal void RequestRelease()
        {
            requestRelease?.Invoke();
        }

        internal bool TryGetCurrentAnimationTime(out float normalizedTime)
        {
            return animation.TryGetCurrentAnimationTime(out normalizedTime);
        }

        internal bool IsAnimationTransitioning()
        {
            return animation.IsAnimationTransitioning();
        }

        internal void NotifyAttackAnimationEnded()
        {
            aliveState.NotifyAttackAnimationEnded();
        }

        internal void NotifyAlertAnimationEnded()
        {
            aliveState.NotifyAlertAnimationEnded();
        }

        internal float GetTargetDistanceSquared()
        {
            return targetDistanceSquared;
        }

        internal void UpdateTargetSnapshot()
        {
            if (!CanTrackTarget())
            {
                targetDistanceSquared = float.PositiveInfinity;
                return;
            }

            targetPosition = target.position;
            Vector3 distance = targetPosition - movement.Position;
            distance.y = 0f;
            targetDistanceSquared = distance.sqrMagnitude;
        }

        private void ChangeState(IZombieState nextState)
        {
            if (ReferenceEquals(currentState, nextState))
            {
                return;
            }

            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }

        private void SetCombatState(bool isInCombat)
        {
            if (IsInCombat == isInCombat)
            {
                return;
            }

            IsInCombat = isInCombat;
            CombatStateChanged?.Invoke();
        }
    }
}
