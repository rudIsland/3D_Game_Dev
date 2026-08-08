using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.MummyWarrior
{
    // Alive, Hit, Dead 우선순위와 살아 있을 때의 행동 순서를 관리한다.
    public sealed class MummyWarriorStateMachine
    {
        private readonly Transform target; // 대상 참조
        private readonly MummyWarriorMovement movement; // 이동 정보
        private readonly MummyWarriorAnimationController animation; // 씬 또는 시스템 참조
        private readonly MummyWarriorAttackPattern[] attackPatterns; // 행동 설정 참조
        private readonly MummyWarriorAttackChooser attackChooser;
        private readonly AliveState aliveState; // 현재 행동 상태
        private readonly HitState hitState; // 피격 또는 피해 관련 값
        private readonly DeadState deadState; // 현재 행동 상태
        private readonly Action<MummyWarriorAttackPattern, int> startAttackHit; // 공격 관련 설정 또는 상태
        private readonly Action endAttackHit; // 공격 관련 설정 또는 상태
        private readonly Action requestRelease; // 내부에서 사용하는 값
        private readonly bool canTrackTarget; // 기능 사용 여부
        private readonly float phaseTwoHealthRate;

        private IMummyWarriorState currentState; // 현재 행동 상태
        private float currentTime; // 시간 설정
        private bool isEnabled; // 기능 사용 여부
        private int currentPhase = 1;

        internal float FindRangeSquared { get; } // 거리 설정
        internal float RunStartRangeSquared { get; } // 거리 설정
        internal float MaximumAttackRangeSquared { get; } // 공격 관련 설정 또는 상태
        internal float WalkSpeed { get; } // 이동 속도
        internal float RunSpeed { get; } // 이동 속도
        internal float TurnSpeed { get; } // 이동 속도
        internal float DeadBodyKeepTime { get; } // 시간 설정
        internal int CurrentPhase => currentPhase;

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
            Action requestRelease,
            bool canTrackTarget = true,
            float phaseTwoHealthRate = 0.6f)
        {
            this.target = target;
            this.movement = movement;
            this.animation = animation;
            this.attackPatterns = attackPatterns ?? Array.Empty<MummyWarriorAttackPattern>();
            attackChooser = new MummyWarriorAttackChooser(this.attackPatterns);
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
            this.canTrackTarget = canTrackTarget;
            this.phaseTwoHealthRate = Mathf.Clamp01(phaseTwoHealthRate);

            aliveState = new AliveState(this);
            hitState = new HitState(this);
            deadState = new DeadState(this);
        }

        public void Enable()
        {
            if (isEnabled) return;

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
            hitState.SetDirection(MummyWarriorHitDirection.Forward);
            EndAttackHit();
            if (ReferenceEquals(currentState, hitState))
            {
                hitState.Restart();
                return;
            }

            ChangeState(hitState);
        }

        public void SetHealthRatio(float healthRatio)
        {
            if (currentPhase >= 2 ||
                healthRatio > phaseTwoHealthRate)
            {
                return;
            }

            currentPhase = 2;
        }

        public void ChangeToDeadState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState)) return;
            EndAttackHit();
            ChangeState(deadState);
        }

        internal bool IsTargetFound()
        {
            return canTrackTarget &&
                GetTargetDistanceSquared() <= FindRangeSquared;
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
            return attackChooser.Choose(
                GetTargetDistanceSquared(),
                GetTargetFacingDot(),
                currentTime,
                currentPhase,
                out attackNumber);
        }

        internal void PlayAttack(MummyWarriorAttackPattern pattern, int attackNumber)
        {
            pattern.StartCooldown(currentTime);
            animation.SetMovement(0f, 0f);
            animation.PlayAttack(
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

        private MummyWarriorHitDirection GetHitDirection(Vector3 hitDirection)
        {
            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude <= 0.0001f)
            {
                return MummyWarriorHitDirection.Forward;
            }

            Vector3 attackerDirection = -hitDirection.normalized;
            float forwardDot = Vector3.Dot(
                movement.Forward,
                attackerDirection);
            float rightDot = Vector3.Dot(
                movement.Right,
                attackerDirection);

            if (Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot))
            {
                return forwardDot >= 0f
                    ? MummyWarriorHitDirection.Forward
                    : MummyWarriorHitDirection.Backward;
            }

            return rightDot >= 0f
                ? MummyWarriorHitDirection.Right
                : MummyWarriorHitDirection.Left;
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

            private readonly MummyWarriorStateMachine stateMachine; // 현재 행동 상태
            private MummyWarriorAttackPattern currentAttack; // 공격 관련 설정 또는 상태
            private AliveAction currentAction; // 현재 행동 상태
            private int currentAttackNumber; // 공격 관련 설정 또는 상태
            private bool isHitOpen; // 기능 사용 여부
            private bool hasHitWindowFinished; // 기능 사용 여부
            private bool hasPlayedEnter; // 기능 사용 여부
            private bool hasEnteredEnter; // 기능 사용 여부
            private bool hasEnteredAttack; // 기능 사용 여부

            internal int PreviousAttackIndex { get; private set; } = -1; // 공격 관련 설정 또는 상태

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
            private readonly MummyWarriorStateMachine stateMachine; // 현재 행동 상태
            private bool hasEnteredHit; // 기능 사용 여부
            private MummyWarriorHitDirection direction;

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

            public void SetDirection(MummyWarriorHitDirection nextDirection)
            {
                direction = nextDirection;
            }

            public void Restart()
            {
                stateMachine.animation.SetMovement(0f, 0f);
                hasEnteredHit = false;
                stateMachine.animation.PlayHit(direction);
            }
        }

        private sealed class DeadState : IMummyWarriorState
        {
            private readonly MummyWarriorStateMachine stateMachine; // 현재 행동 상태
            private float remainingKeepTime; // 시간 설정
            private bool hasRequestedRelease; // 기능 사용 여부

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
