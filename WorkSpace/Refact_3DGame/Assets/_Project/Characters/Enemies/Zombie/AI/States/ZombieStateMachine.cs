using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 좀비의 탐지·경계·추적·공격 상태를 관리한다.
    public sealed class ZombieStateMachine
    {
        private readonly Transform target;
        private readonly ZombieMovement movement;
        private readonly ZombieAnimationController animation;
        private readonly ZombieAliveState aliveState;
        private readonly ZombieHitState hitState;
        private readonly ZombieDeadState deadState;
        private readonly Action requestRelease;
        private readonly Action endAttackHit;
        private readonly float minimumAttackFacingDot;

        private IZombieState currentState;
        private bool isEnabled;
        private Vector3 targetPosition;
        private float targetDistanceSquared;

        internal float FindRangeSquared { get; }
        internal float IdleTargetCheckInterval { get; }
        internal float AttackRangeSquared { get; }
        internal float ChaseSpeed { get; }
        internal float TurnSpeed { get; }
        internal float DeadBodyKeepTime { get; }
        public ZombieStateMachine(
            Transform target,
            ZombieMovement movement,
            ZombieAnimationController animation,
            float findRange,
            float idleTargetCheckInterval,
            float attackRange,
            float attackFacingAngle,
            float chaseSpeed,
            float turnSpeed,
            float deadBodyKeepTime,
            Action requestRelease,
            Action endAttackHit)
        {
            this.target = target;
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
            aliveState.ResetTargetAwareness();
            movement.Reset();
            animation.ResetAnimation();
            ChangeState(aliveState);
        }

        public void Update(float deltaTime)
        {
            if (isEnabled && currentState != null)
            {
                if (ReferenceEquals(currentState, aliveState) &&
                    aliveState.NeedsTargetUpdateEveryFrame)
                {
                    UpdateTargetSnapshot();
                }

                currentState.Update(deltaTime);
            }
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
            animation.ResetAnimation();
        }

        internal bool IsTargetFound()
        {
            return targetDistanceSquared <= FindRangeSquared;
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

        internal void ChangeToHitState()
        {
            if (!isEnabled ||
                ReferenceEquals(currentState, deadState))
            {
                return;
            }

            EndAttackHit();
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
            ChangeState(deadState);
        }

        internal void EndAttackHit()
        {
            endAttackHit?.Invoke();
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
    }
}
