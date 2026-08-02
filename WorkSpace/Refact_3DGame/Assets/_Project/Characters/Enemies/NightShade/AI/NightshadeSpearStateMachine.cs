using System;
using rudIsland.RPG3D.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 상태 생성과 전환, Nightshade 공용 전투 정보를 조정한다.
    public sealed class NightshadeSpearStateMachine
    {
        private readonly Transform target;
        private readonly NightshadeSpearMovement movement;
        private readonly NightshadeSpearAnimationController animation;
        private readonly NightshadeSpearAttackPattern[] attackPatterns;
        private readonly NightshadeSpearAttackChooser attackChooser;
        private readonly NightshadeSpearEnterState enterState;
        private readonly NightshadeSpearIdleState idleState;
        private readonly NightshadeSpearChaseState chaseState;
        private readonly NightshadeSpearAttackState attackState;
        private readonly NightshadeSpearHitState hitState;
        private readonly NightshadeSpearDeadState deadState;
        private readonly Action<NightshadeSpearAttackPattern, int> startAttackHit;
        private readonly Action endAttackHit;
        private readonly Action requestRelease;
        private readonly bool canTrackTarget;
        private readonly float phaseTwoHealthRate;

        private INightshadeSpearState currentState;
        private float currentTime;
        private bool isEnabled;
        private int currentPhase = 1;

        internal Transform Target => target;
        internal NightshadeSpearMovement Movement => movement;
        internal NightshadeSpearAnimationController Animation => animation;
        internal float FindRangeSquared { get; }
        internal float RunStartRangeSquared { get; }
        internal float MaximumAttackRangeSquared { get; }
        internal float WalkSpeed { get; }
        internal float RunSpeed { get; }
        internal float TurnSpeed { get; }
        internal float DeadBodyKeepTime { get; }
        internal float CurrentTime => currentTime;
        public int CurrentPhase => currentPhase;
        public bool IsEnabled => isEnabled;
        public string CurrentStateName =>
            currentState != null ? currentState.Name : "Disabled";
        public string CurrentAttackName =>
            ReferenceEquals(currentState, attackState)
                ? attackState.CurrentAttackName
                : string.Empty;

        public NightshadeSpearStateMachine(
            Transform target,
            NightshadeSpearMovement movement,
            NightshadeSpearAnimationController animation,
            NightshadeSpearAttackPattern[] attackPatterns,
            float findRange,
            float runStartRange,
            float walkSpeed,
            float runSpeed,
            float turnSpeed,
            float deadBodyKeepTime,
            Action<NightshadeSpearAttackPattern, int> startAttackHit,
            Action endAttackHit,
            Action requestRelease,
            bool canTrackTarget = true,
            float phaseTwoHealthRate = 0.6f)
        {
            this.target = target;
            this.movement = movement;
            this.animation = animation;
            this.attackPatterns = attackPatterns ??
                Array.Empty<NightshadeSpearAttackPattern>();
            attackChooser = new NightshadeSpearAttackChooser(
                this.attackPatterns);

            FindRangeSquared = findRange * findRange;
            RunStartRangeSquared = runStartRange * runStartRange;

            float maximumAttackRangeSquared = 0f;
            for (int index = 0; index < this.attackPatterns.Length; index++)
            {
                NightshadeSpearAttackPattern pattern =
                    this.attackPatterns[index];
                if (pattern != null)
                {
                    maximumAttackRangeSquared = Mathf.Max(
                        maximumAttackRangeSquared,
                        pattern.MaximumDistanceSquared);
                }
            }

            MaximumAttackRangeSquared = maximumAttackRangeSquared;
            WalkSpeed = walkSpeed;
            RunSpeed = runSpeed;
            TurnSpeed = turnSpeed;
            DeadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
            this.startAttackHit = startAttackHit;
            this.endAttackHit = endAttackHit;
            this.requestRelease = requestRelease;
            this.canTrackTarget = canTrackTarget;
            this.phaseTwoHealthRate = Mathf.Clamp01(phaseTwoHealthRate);

            enterState = new NightshadeSpearEnterState(this);
            idleState = new NightshadeSpearIdleState(this);
            chaseState = new NightshadeSpearChaseState(this);
            attackState = new NightshadeSpearAttackState(this);
            hitState = new NightshadeSpearHitState(this);
            deadState = new NightshadeSpearDeadState(this);
        }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            currentTime = 0f;
            currentPhase = 1;
            attackChooser.Reset();

            for (int index = 0; index < attackPatterns.Length; index++)
            {
                attackPatterns[index]?.Prepare();
            }

            movement.Reset();
            animation.ResetAnimation();
            ChangeState(enterState);
        }

        public void Update(float deltaTime)
        {
            if (!isEnabled || currentState == null)
            {
                return;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            currentTime += safeDeltaTime;
            currentState.Update(safeDeltaTime);
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

        public void ChangeToHitState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            HitReaction reaction = default;
            hitState.SetHitReaction(in reaction);
            ChangeToHitStateInternal();
        }

        public void ChangeToHitState(in AttackHitData hit)
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            HitReaction reaction = HitReaction.Create(
                in hit,
                movement.Forward,
                movement.Right);
            hitState.SetHitReaction(in reaction);
            ChangeToHitStateInternal();
        }

        public void SetHealthRatio(float healthRatio)
        {
            if (currentPhase >= 2 || healthRatio > phaseTwoHealthRate)
            {
                return;
            }

            currentPhase = 2;
        }

        public void ChangeToDeadState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            ChangeState(deadState);
        }

        internal bool IsTargetFound()
        {
            return canTrackTarget &&
                GetTargetDistanceSquared() <= FindRangeSquared;
        }

        internal float GetTargetDistanceSquared()
        {
            Vector3 distance = target.position - movement.Position;
            distance.y = 0f;
            return distance.sqrMagnitude;
        }

        internal float GetTargetFacingDot()
        {
            Vector3 direction = target.position - movement.Position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return 1f;
            }

            direction.Normalize();
            return Vector3.Dot(movement.Forward, direction);
        }

        internal void MoveToTarget(float deltaTime)
        {
            float distanceSquared = GetTargetDistanceSquared();
            bool shouldRun = distanceSquared >= RunStartRangeSquared;
            float moveSpeed = shouldRun ? RunSpeed : WalkSpeed;
            Vector3 direction = movement.MoveTo(
                target.position,
                moveSpeed,
                TurnSpeed,
                deltaTime);
            float moveSide = Vector3.Dot(movement.Right, direction);
            animation.SetMovement(moveSide, shouldRun ? 1f : 0.5f);
        }

        internal void MoveAwayFromTarget(float deltaTime)
        {
            Vector3 direction = movement.MoveAwayFrom(
                target.position,
                WalkSpeed,
                TurnSpeed,
                deltaTime);
            float moveSide = Vector3.Dot(movement.Right, direction);
            animation.SetMovement(moveSide, 0.5f);
        }

        internal void TurnToTarget(float deltaTime)
        {
            animation.SetMovement(0f, 0f);
            movement.TurnTo(target.position, TurnSpeed, deltaTime);
        }

        internal void StayOnGround(float deltaTime)
        {
            movement.StayOnGround(deltaTime);
        }

        internal void StartHitPush(
            Vector3 hitDirection,
            float pushDistance)
        {
            movement.StartHitPush(hitDirection, pushDistance);
        }

        internal void UpdateHitPush(float deltaTime)
        {
            movement.UpdateHitPush(deltaTime);
        }

        internal void StopHitPush()
        {
            movement.StopHitPush();
        }

        internal NightshadeSpearAttackPattern ChooseAttack(out int attackNumber)
        {
            return attackChooser.Choose(
                GetTargetDistanceSquared(),
                GetTargetFacingDot(),
                currentTime,
                currentPhase,
                out attackNumber);
        }

        internal void ChangeToIdleState()
        {
            if (!ReferenceEquals(currentState, deadState))
            {
                ChangeState(idleState);
            }
        }

        internal void ChangeToChaseState()
        {
            if (!ReferenceEquals(currentState, deadState))
            {
                ChangeState(chaseState);
            }
        }

        internal void ChangeToAttackState(
            NightshadeSpearAttackPattern pattern,
            int attackNumber)
        {
            attackState.Prepare(pattern, attackNumber);
            ChangeState(attackState);
        }

        internal void ChangeToAliveState()
        {
            if (!ReferenceEquals(currentState, deadState))
            {
                ChangeToChaseState();
            }
        }

        internal void PlayAttack(
            NightshadeSpearAttackPattern pattern,
            int attackNumber)
        {
            pattern.StartCooldown(currentTime);
            animation.SetMovement(0f, 0f);
            animation.PlayAttack(
                pattern.AnimatorStateId,
                pattern.TransitionTime,
                pattern.AnimationSpeed);
        }

        internal void UpdateAttackHit(
            NightshadeSpearAttackPattern pattern,
            int attackNumber,
            ref bool isHitOpen,
            ref bool hasHitWindowFinished)
        {
            if (hasHitWindowFinished ||
                !animation.TryGetCurrentActionTime(
                    out float normalizedTime))
            {
                return;
            }

            if (!isHitOpen && normalizedTime >= pattern.HitStartTime)
            {
                isHitOpen = true;
                startAttackHit?.Invoke(pattern, attackNumber);
            }

            if (isHitOpen && normalizedTime > pattern.HitEndTime)
            {
                isHitOpen = false;
                hasHitWindowFinished = true;
                EndAttackHit();
            }
        }

        internal void EndAttackHit()
        {
            endAttackHit?.Invoke();
        }

        internal void RequestRelease()
        {
            requestRelease?.Invoke();
        }

        internal bool TryGetCurrentActionTime(out float normalizedTime)
        {
            return animation.TryGetCurrentActionTime(out normalizedTime);
        }

        internal bool IsActionTransitioning()
        {
            return animation.IsActionTransitioning();
        }

        private void ChangeToHitStateInternal()
        {
            EndAttackHit();
            if (ReferenceEquals(currentState, hitState))
            {
                hitState.Restart();
                return;
            }

            ChangeState(hitState);
        }

        private void ChangeState(INightshadeSpearState nextState)
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
