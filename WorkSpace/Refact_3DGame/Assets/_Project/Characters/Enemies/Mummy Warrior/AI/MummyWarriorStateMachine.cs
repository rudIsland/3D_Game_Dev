using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.MummyWarrior
{
    // Alive, Hit, Dead 우선순위와 살아 있을 때의 행동 순서를 관리한다.
    public sealed class MummyWarriorStateMachine
    {
        private readonly Transform target;
        private readonly MummyWarriorMovement movement;
        private readonly MummyWarriorAnimationController animation;
        private readonly MummyWarriorAttackPattern[] attackPatterns;
        private readonly AliveState aliveState;
        private readonly HitState hitState;
        private readonly DeadState deadState;
        private readonly Action<MummyWarriorAttackPattern, int> startAttackHit;
        private readonly Action endAttackHit;
        private readonly Action requestRelease;

        private IMummyWarriorState currentState;
        private float currentTime;
        private bool isEnabled;

        internal float FindRangeSquared { get; }
        internal float RunStartRangeSquared { get; }
        internal float MaximumAttackRangeSquared { get; }
        internal float WalkSpeed { get; }
        internal float RunSpeed { get; }
        internal float TurnSpeed { get; }
        internal float DeadBodyKeepTime { get; }

        public MummyWarriorStateMachine(
            Transform target,
            MummyWarriorMovement movement,
            MummyWarriorAnimationController animation,
            MummyWarriorAttackPattern[] attackPatterns,
            float findRange,
            float runStartRange,
            float walkSpeed,
            float runSpeed,
            float turnSpeed,
            float deadBodyKeepTime,
            Action<MummyWarriorAttackPattern, int> startAttackHit,
            Action endAttackHit,
            Action requestRelease)
        {
            this.target = target;
            this.movement = movement;
            this.animation = animation;
            this.attackPatterns = attackPatterns ?? Array.Empty<MummyWarriorAttackPattern>();
            FindRangeSquared = findRange * findRange;
            RunStartRangeSquared = runStartRange * runStartRange;
            float maximumAttackRangeSquared = 0f;
            for (int index = 0; index < this.attackPatterns.Length; index++)
            {
                MummyWarriorAttackPattern pattern = this.attackPatterns[index];
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

            aliveState = new AliveState(this);
            hitState = new HitState(this);
            deadState = new DeadState(this);
        }

        public void Enable()
        {
            if (isEnabled) return;

            isEnabled = true;
            currentTime = 0f;
            for (int index = 0; index < attackPatterns.Length; index++)
            {
                attackPatterns[index]?.Prepare();
            }

            movement.Reset();
            animation.ResetAnimation();
            aliveState.Reset();
            ChangeState(aliveState);
        }

        public void Update(float deltaTime)
        {
            if (!isEnabled || currentState == null) return;
            currentTime += Mathf.Max(0f, deltaTime);
            currentState.Update(deltaTime);
        }

        public void Disable()
        {
            if (!isEnabled) return;
            currentState?.Exit();
            EndAttackHit();
            currentState = null;
            isEnabled = false;
            animation.ResetAnimation();
        }

        public void ChangeToHitState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState)) return;
            EndAttackHit();
            if (ReferenceEquals(currentState, hitState))
            {
                hitState.Restart();
                return;
            }

            ChangeState(hitState);
        }

        public void ChangeToDeadState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState)) return;
            EndAttackHit();
            ChangeState(deadState);
        }

        internal bool IsTargetFound() => GetTargetDistanceSquared() <= FindRangeSquared;

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

        internal void TurnToTarget(float deltaTime)
        {
            animation.SetMovement(0f, 0f);
            movement.TurnTo(target.position, TurnSpeed, deltaTime);
        }

        internal void StayOnGround(float deltaTime)
        {
            movement.StayOnGround(deltaTime);
        }

        internal MummyWarriorAttackPattern ChooseAttack(out int attackNumber)
        {
            attackNumber = 0;
            float distanceSquared = GetTargetDistanceSquared();
            float facingDot = GetTargetFacingDot();
            int previousIndex = aliveState.PreviousAttackIndex;
            bool hasOtherAttack = false;

            for (int index = 0; index < attackPatterns.Length; index++)
            {
                MummyWarriorAttackPattern pattern = attackPatterns[index];
                if (index != previousIndex && pattern != null &&
                    pattern.CanUse(distanceSquared, facingDot, currentTime))
                {
                    hasOtherAttack = true;
                    break;
                }
            }

            float totalWeight = 0f;
            for (int index = 0; index < attackPatterns.Length; index++)
            {
                MummyWarriorAttackPattern pattern = attackPatterns[index];
                if (pattern == null || (hasOtherAttack && index == previousIndex) ||
                    !pattern.CanUse(distanceSquared, facingDot, currentTime))
                {
                    continue;
                }

                totalWeight += pattern.SelectionWeight;
            }

            if (totalWeight <= 0f) return null;

            float selectedWeight = UnityEngine.Random.Range(0f, totalWeight);
            for (int index = 0; index < attackPatterns.Length; index++)
            {
                MummyWarriorAttackPattern pattern = attackPatterns[index];
                if (pattern == null || (hasOtherAttack && index == previousIndex) ||
                    !pattern.CanUse(distanceSquared, facingDot, currentTime))
                {
                    continue;
                }

                selectedWeight -= pattern.SelectionWeight;
                if (selectedWeight > 0f) continue;

                attackNumber = index + 1;
                return pattern;
            }

            return null;
        }

        internal void PlayAttack(MummyWarriorAttackPattern pattern, int attackNumber)
        {
            pattern.StartCooldown(currentTime);
            animation.SetMovement(0f, 0f);
            animation.PlayAttack(
                attackNumber,
                pattern.AnimatorStateId,
                pattern.TransitionTime,
                pattern.AnimationSpeed);
        }

        internal void UpdateAttackHit(
            MummyWarriorAttackPattern pattern,
            int attackNumber,
            ref bool isHitOpen,
            ref bool hasHitWindowFinished)
        {
            if (hasHitWindowFinished) return;

            if (!animation.TryGetCurrentActionTime(
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

        internal void EndAttackHit() => endAttackHit?.Invoke();
        internal void RequestRelease() => requestRelease?.Invoke();
        internal bool TryGetCurrentActionTime(out float normalizedTime)
        {
            return animation.TryGetCurrentActionTime(out normalizedTime);
        }

        internal bool IsActionTransitioning()
        {
            return animation.IsActionTransitioning();
        }

        internal void ChangeToAliveState()
        {
            if (!ReferenceEquals(currentState, deadState)) ChangeState(aliveState);
        }

        private float GetTargetDistanceSquared()
        {
            Vector3 distance = target.position - movement.Position;
            distance.y = 0f;
            return distance.sqrMagnitude;
        }

        private float GetTargetFacingDot()
        {
            Vector3 direction = target.position - movement.Position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f) return 1f;
            direction.Normalize();
            return Vector3.Dot(movement.Forward, direction);
        }

        private void ChangeState(IMummyWarriorState nextState)
        {
            if (ReferenceEquals(currentState, nextState)) return;
            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }

        private sealed class AliveState : IMummyWarriorState
        {
            private enum AliveAction
            {
                Enter,
                Idle,
                Chase,
                Attack
            }

            private readonly MummyWarriorStateMachine stateMachine;
            private MummyWarriorAttackPattern currentAttack;
            private AliveAction currentAction;
            private int currentAttackNumber;
            private bool isHitOpen;
            private bool hasHitWindowFinished;
            private bool hasPlayedEnter;
            private bool hasEnteredEnter;
            private bool hasEnteredAttack;

            internal int PreviousAttackIndex { get; private set; } = -1;

            public AliveState(MummyWarriorStateMachine stateMachine)
            {
                this.stateMachine = stateMachine;
            }

            public void Reset()
            {
                currentAttack = null;
                currentAttackNumber = 0;
                PreviousAttackIndex = -1;
                isHitOpen = false;
                hasHitWindowFinished = false;
                hasPlayedEnter = false;
                hasEnteredEnter = false;
                hasEnteredAttack = false;
            }

            public void Enter()
            {
                if (!hasPlayedEnter)
                {
                    hasPlayedEnter = true;
                    ChangeAction(AliveAction.Enter);
                    return;
                }

                ChooseNextAction();
            }

            public void Update(float deltaTime)
            {
                switch (currentAction)
                {
                    case AliveAction.Enter:
                        UpdateEnter(deltaTime);
                        break;
                    case AliveAction.Chase:
                        UpdateChase(deltaTime);
                        break;
                    case AliveAction.Attack:
                        UpdateAttack(deltaTime);
                        break;
                    default:
                        UpdateIdle(deltaTime);
                        break;
                }
            }

            public void Exit()
            {
                if (isHitOpen) stateMachine.EndAttackHit();
                currentAttack = null;
                isHitOpen = false;
                hasHitWindowFinished = false;
            }

            private void UpdateEnter(float deltaTime)
            {
                stateMachine.StayOnGround(deltaTime);
                bool hasActionTime = stateMachine.TryGetCurrentActionTime(
                    out float normalizedTime);
                if (hasActionTime)
                {
                    hasEnteredEnter = true;
                }

                if (!hasEnteredEnter ||
                    stateMachine.IsActionTransitioning() ||
                    !hasActionTime ||
                    normalizedTime < 1f)
                {
                    return;
                }

                stateMachine.animation.ResetActionSpeed();
                ChooseNextAction();
            }

            private void UpdateIdle(float deltaTime)
            {
                stateMachine.animation.SetMovement(0f, 0f);
                stateMachine.StayOnGround(deltaTime);
                if (stateMachine.IsTargetFound()) ChooseNextAction();
            }

            private void UpdateChase(float deltaTime)
            {
                if (!stateMachine.IsTargetFound())
                {
                    ChangeAction(AliveAction.Idle);
                    return;
                }

                MummyWarriorAttackPattern attack =
                    stateMachine.ChooseAttack(out int attackNumber);
                if (attack != null)
                {
                    StartAttack(attack, attackNumber);
                    return;
                }

                if (stateMachine.GetTargetDistanceSquared() <=
                    stateMachine.MaximumAttackRangeSquared)
                {
                    stateMachine.TurnToTarget(deltaTime);
                    return;
                }

                stateMachine.MoveToTarget(deltaTime);
            }

            private void UpdateAttack(float deltaTime)
            {
                stateMachine.StayOnGround(deltaTime);
                stateMachine.UpdateAttackHit(
                    currentAttack,
                    currentAttackNumber,
                    ref isHitOpen,
                    ref hasHitWindowFinished);

                bool hasActionTime = stateMachine.TryGetCurrentActionTime(
                    out float normalizedTime);
                if (hasActionTime)
                {
                    hasEnteredAttack = true;
                }

                if (!hasEnteredAttack ||
                    stateMachine.IsActionTransitioning() ||
                    !hasActionTime ||
                    normalizedTime < 1f)
                {
                    return;
                }

                if (isHitOpen)
                {
                    isHitOpen = false;
                    hasHitWindowFinished = true;
                    stateMachine.EndAttackHit();
                }

                stateMachine.animation.ResetActionSpeed();
                ChooseNextAction();
            }

            private void ChooseNextAction()
            {
                if (!stateMachine.IsTargetFound())
                {
                    ChangeAction(AliveAction.Idle);
                    return;
                }

                MummyWarriorAttackPattern attack =
                    stateMachine.ChooseAttack(out int attackNumber);
                if (attack != null)
                {
                    StartAttack(attack, attackNumber);
                    return;
                }

                ChangeAction(AliveAction.Chase);
            }

            private void StartAttack(
                MummyWarriorAttackPattern attack,
                int attackNumber)
            {
                currentAttack = attack;
                currentAttackNumber = attackNumber;
                PreviousAttackIndex = attackNumber - 1;
                isHitOpen = false;
                hasHitWindowFinished = false;
                hasEnteredAttack = false;
                ChangeAction(AliveAction.Attack);
                stateMachine.PlayAttack(attack, attackNumber);
            }

            private void ChangeAction(AliveAction nextAction)
            {
                if (currentAction == AliveAction.Attack && isHitOpen)
                {
                    isHitOpen = false;
                    stateMachine.EndAttackHit();
                }

                currentAction = nextAction;
                if (nextAction == AliveAction.Enter)
                {
                    stateMachine.animation.SetMovement(0f, 0f);
                    stateMachine.animation.PlayEnter();
                }
                else if (nextAction == AliveAction.Idle)
                {
                    stateMachine.animation.SetMovement(0f, 0f);
                }
            }
        }

        private sealed class HitState : IMummyWarriorState
        {
            private readonly MummyWarriorStateMachine stateMachine;
            private bool hasEnteredHit;

            public HitState(MummyWarriorStateMachine stateMachine)
            {
                this.stateMachine = stateMachine;
            }

            public void Enter() => Restart();

            public void Update(float deltaTime)
            {
                stateMachine.StayOnGround(deltaTime);
                bool hasActionTime = stateMachine.TryGetCurrentActionTime(
                    out float normalizedTime);
                if (hasActionTime)
                {
                    hasEnteredHit = true;
                }

                if (hasEnteredHit &&
                    !stateMachine.IsActionTransitioning() &&
                    hasActionTime &&
                    normalizedTime >= 1f)
                {
                    stateMachine.ChangeToAliveState();
                }
            }

            public void Exit()
            {
                stateMachine.animation.ResetActionSpeed();
            }

            public void Restart()
            {
                stateMachine.animation.SetMovement(0f, 0f);
                hasEnteredHit = false;
                stateMachine.animation.PlayHit();
            }
        }

        private sealed class DeadState : IMummyWarriorState
        {
            private readonly MummyWarriorStateMachine stateMachine;
            private float remainingKeepTime;
            private bool hasRequestedRelease;

            public DeadState(MummyWarriorStateMachine stateMachine)
            {
                this.stateMachine = stateMachine;
            }

            public void Enter()
            {
                remainingKeepTime = stateMachine.DeadBodyKeepTime;
                hasRequestedRelease = false;
                stateMachine.animation.PlayDeath();
            }

            public void Update(float deltaTime)
            {
                stateMachine.StayOnGround(deltaTime);
                if (hasRequestedRelease) return;

                remainingKeepTime -= deltaTime;
                if (remainingKeepTime > 0f) return;

                hasRequestedRelease = true;
                stateMachine.RequestRelease();
            }

            public void Exit()
            {
            }
        }
    }

    internal interface IMummyWarriorState
    {
        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}
