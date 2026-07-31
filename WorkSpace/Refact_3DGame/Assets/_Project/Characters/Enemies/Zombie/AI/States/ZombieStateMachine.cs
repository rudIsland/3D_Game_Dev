using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // Alive와 Dead 최상위 상태를 관리하고 공통 기능을 제공한다.
    public sealed class ZombieStateMachine
    {
        private readonly Transform target;
        private readonly ZombieMovement movement;
        private readonly ZombieAnimationController animation;

        private readonly ZombieAliveState aliveState;
        private readonly ZombieDeadState deadState;

        private IZombieState currentState;
        private UnitHealth health;
        private bool isEnabled;
        private int nextAttackNumber;

        internal float FindRangeSquared { get; }
        internal float AttackRangeSquared { get; }
        internal float ChaseSpeed { get; }
        internal float TurnSpeed { get; }
        internal float AlertTime { get; }
        internal float AttackInterval { get; }
        internal float HitTime { get; }

        public string CurrentStateName =>
            currentState?.Name ?? "Disabled";

        public ZombieStateMachine(
            Transform target,
            ZombieMovement movement,
            ZombieAnimationController animation,
            float findRange,
            float attackRange,
            float chaseSpeed,
            float turnSpeed,
            float alertTime,
            float attackInterval,
            float hitTime)
        {
            this.target = target;
            this.movement = movement;
            this.animation = animation;

            FindRangeSquared = findRange * findRange;
            AttackRangeSquared = attackRange * attackRange;
            ChaseSpeed = chaseSpeed;
            TurnSpeed = turnSpeed;
            AlertTime = alertTime;
            AttackInterval = attackInterval;
            HitTime = hitTime;

            aliveState = new ZombieAliveState(this);
            deadState = new ZombieDeadState(this);
        }

        internal void SetHealth(UnitHealth unitHealth)
        {
            health = unitHealth;
        }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            nextAttackNumber = 0;
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

            if (health.IsDead &&
                !ReferenceEquals(currentState, deadState))
            {
                ChangeState(deadState);
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
            currentState = null;
            isEnabled = false;
            animation.ResetAnimation();
        }

        public void TakeDamage(float damage)
        {
            if (!isEnabled || health == null || health.IsDead)
            {
                return;
            }

            float healthBeforeDamage = health.CurrentHealth;
            health.TakeDamage(damage);

            if (Mathf.Approximately(
                    healthBeforeDamage,
                    health.CurrentHealth))
            {
                return;
            }

            if (health.IsDead)
            {
                ChangeState(deadState);
                return;
            }

            aliveState.PlayHit();
        }

        internal bool IsTargetFound()
        {
            return GetTargetDistanceSquared() <= FindRangeSquared;
        }

        internal bool IsTargetInAttackRange()
        {
            return GetTargetDistanceSquared() <= AttackRangeSquared;
        }

        internal void MoveToTarget(float deltaTime)
        {
            movement.MoveTo(
                target.position,
                ChaseSpeed,
                TurnSpeed,
                deltaTime);
        }

        internal void TurnToTarget(float deltaTime)
        {
            movement.TurnTo(
                target.position,
                TurnSpeed,
                deltaTime);
        }

        internal void StayOnGround(float deltaTime)
        {
            movement.StayOnGround(deltaTime);
        }

        internal void PlayAttack()
        {
            switch (nextAttackNumber)
            {
                case 1:
                    animation.PlayKickAttack();
                    break;
                case 2:
                    animation.PlayUpDownAttack();
                    break;
                default:
                    animation.PlaySwingAttack();
                    break;
            }

            nextAttackNumber = (nextAttackNumber + 1) % 3;
        }

        internal void PlayScream()
        {
            animation.PlayScream();
        }

        internal void PlayHit()
        {
            animation.PlayHit();
        }

        internal void PlayDeath()
        {
            animation.PlayDeath();
        }

        internal void SetMoveSpeed(float moveSpeed)
        {
            animation.SetMoveSpeed(moveSpeed);
        }

        private float GetTargetDistanceSquared()
        {
            Vector3 distance = target.position - movement.Position;
            distance.y = 0f;
            return distance.sqrMagnitude;
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
