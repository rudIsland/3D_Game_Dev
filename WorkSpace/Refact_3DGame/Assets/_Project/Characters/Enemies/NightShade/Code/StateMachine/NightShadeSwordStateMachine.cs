using System;
using rudIsland.RPG3D.Characters.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal enum NightShadeSwordAttackType
    {
        Light = 0,
        ComboFirst = 1,
        Heavy = 2,
        WideSwing = 3,
        ComboSecond = 4
    }

    internal enum NightShadeCombatMoveType
    {
        Backward = 0,
        Left = 1,
        Right = 2
    }

    internal enum NightShadeSwordStateId
    {
        Idle = 0,
        Combat = 1,
        Hit = 2,
        Dead = 3
    }

    // 상위 Idle / Combat / Hit / Dead와 강제 반응 우선순위만 관리한다.
    internal sealed class NightShadeSwordStateMachine
    {
        private readonly NightShadeSwordTargetStatus targetStatus;
        private readonly NightShadeSwordCombatMemory combatMemory;
        private readonly NightShadeSwordCombatOutput combatOutput;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordIdleState idleState;
        private readonly NightShadeSwordCombatState combatState;
        private readonly NightShadeSwordHitState hitState;
        private readonly NightShadeSwordDeadState deadState;
        private readonly NightShadeSwordCombatDebug debug;

        private INightShadeSwordState currentState;
        private NightShadeSwordStateId currentStateId;
        private EnemyHitRequest pendingHitRequest;
        private HitReaction pendingHitReaction;
        private bool hasPendingDeath;
        private bool isEnabled;
        private bool isInCombat;

        internal bool IsInCombat => isInCombat;
        internal bool ProtectsSmallHit => IsAttackStateActive && combatState.ProtectsSmallHit;
        internal float StopDamageScale => ProtectsSmallHit ? 0.5f : 1f;
        internal bool IsAttackStateActive =>
            isEnabled && currentState != null &&
            currentStateId == NightShadeSwordStateId.Combat &&
            combatState.IsAttackActionActive;
        internal NightShadeSwordStateId CurrentStateId => currentStateId;
        internal NightShadeSwordCombatPhase CurrentCombatPhase => combatState.Phase;
        internal NightShadeSwordActionId CurrentActionId => combatState.CurrentActionId;
        internal NightShadeSwordCombatMemory CombatMemory => combatMemory;
        internal NightShadeSwordCombatDebug Debug => debug;

        internal event Action CombatStateChanged;

        internal NightShadeSwordStateMachine(
            Transform target,
            IUnitDeathState targetDeathState,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordCombatOutput combatOutput,
            INightShadeSwordRandomProvider randomProvider = null)
        {
            this.movement = movement;
            this.animation = animation;
            this.combatOutput = combatOutput;
            combatMemory = new NightShadeSwordCombatMemory();
            debug = new NightShadeSwordCombatDebug();
            targetStatus = new NightShadeSwordTargetStatus(
                target,
                targetDeathState,
                movement,
                settings.CombatRange);
            var context = new NightShadeSwordBehaviorContext(
                targetStatus,
                combatMemory,
                movement,
                animation);
            combatState = new NightShadeSwordCombatState(
                context,
                settings,
                combatOutput,
                randomProvider ?? new UnityNightShadeSwordRandomProvider(),
                debug);
            hitState = new NightShadeSwordHitState(
                context,
                settings.HitReaction);
            idleState = new NightShadeSwordIdleState(context);
            deadState = new NightShadeSwordDeadState(
                context,
                settings.Life,
                combatOutput);
        }

        internal void Enable()
        {
            isEnabled = true;
            hasPendingDeath = false;
            pendingHitReaction = HitReaction.None;
            movement.Reset();
            animation.ResetAttackPlaybackSpeed();
            combatMemory.Reset();
            targetStatus.Refresh();
            debug.Reset();
            SetCombatState(false);
            ChangeState(NightShadeSwordStateId.Idle, true);
        }

        internal void Disable()
        {
            isEnabled = false;
            hasPendingDeath = false;
            pendingHitReaction = HitReaction.None;
            if (currentState != null)
            {
                if (currentStateId == NightShadeSwordStateId.Combat)
                {
                    combatState.InterruptCurrentAction(
                        NightShadeSwordActionStopReason.Disabled);
                }

                currentState.Exit();
                currentState = null;
            }

            debug.CurrentAction = NightShadeSwordActionId.None;
            debug.CombatPhase = NightShadeSwordCombatPhase.None;
            SetCombatState(false);
        }

        internal void Update(float deltaTime, bool isHitStopActive = false)
        {
            if (!isEnabled || currentState == null)
            {
                return;
            }

            // 1. 한 Tick에서 공유할 타겟 상황을 먼저 갱신한다.
            targetStatus.Refresh();
            // 2. 일반 상태보다 우선하는 사망과 피격 요청을 처리한다.
            ProcessForcedReaction();
            if (isHitStopActive)
            {
                return;
            }

            // 3. Hit Stop이 아닐 때만 후딜과 현재 상태의 시간을 진행한다.
            combatMemory.UpdatePostAttackDelay(deltaTime);
            NightShadeSwordStateId? nextState = currentState.Update(deltaTime);
            if (nextState.HasValue)
            {
                ChangeState(nextState.Value);
            }
        }

        internal void ChangeToHitState(
            HitReaction reaction,
            in EnemyHitRequest hitRequest)
        {
            if (reaction == HitReaction.None ||
                hasPendingDeath ||
                (currentState != null &&
                    currentStateId == NightShadeSwordStateId.Dead))
            {
                return;
            }

            if (pendingHitReaction == HitReaction.None ||
                GetReactionPriority(reaction) >=
                    GetReactionPriority(pendingHitReaction))
            {
                pendingHitReaction = reaction;
                pendingHitRequest = hitRequest;
            }
        }

        internal void ChangeToDeadState()
        {
            if (currentState != null &&
                currentStateId == NightShadeSwordStateId.Dead)
            {
                return;
            }

            hasPendingDeath = true;
            pendingHitReaction = HitReaction.None;
        }

        internal void NotifyDamaged()
        {
            if (currentState == null || currentStateId != NightShadeSwordStateId.Dead)
            {
                SetCombatState(true);
            }
        }

        internal void StopAttackTurnAnimationEvent()
        {
            if (IsAttackStateActive)
            {
                combatState.QueueStopTurn();
            }
        }

        internal void PlayAttackSoundAnimationEvent(int hitIndex)
        {
            if (IsAttackStateActive)
            {
                combatState.QueuePlaySound(hitIndex);
            }
        }

        internal void OpenAttackHitAnimationEvent(int hitIndex)
        {
            if (IsAttackStateActive)
            {
                combatState.QueueOpenHit(hitIndex);
            }
        }

        internal void CloseAttackHitAnimationEvent()
        {
            if (IsAttackStateActive)
            {
                combatState.QueueCloseHit();
            }
        }

        private void ProcessForcedReaction()
        {
            if (hasPendingDeath)
            {
                hasPendingDeath = false;
                pendingHitReaction = HitReaction.None;
                InterruptCombatAction();
                ChangeState(NightShadeSwordStateId.Dead);
                return;
            }

            if (pendingHitReaction == HitReaction.None ||
                currentStateId == NightShadeSwordStateId.Dead)
            {
                return;
            }

            HitReaction reaction = pendingHitReaction;
            EnemyHitRequest hitRequest = pendingHitRequest;
            pendingHitReaction = HitReaction.None;

            if (currentStateId == NightShadeSwordStateId.Hit)
            {
                hitState.TryRestart(reaction, in hitRequest);
                return;
            }

            hitState.SetHitRequest(reaction, in hitRequest);
            InterruptCombatAction();
            ChangeState(NightShadeSwordStateId.Hit);
        }

        private void InterruptCombatAction()
        {
            if (currentStateId == NightShadeSwordStateId.Combat)
            {
                combatState.InterruptCurrentAction(
                    NightShadeSwordActionStopReason.Interrupted);
            }
        }

        private void ChangeState(
            NightShadeSwordStateId nextStateId,
            bool force = false)
        {
            if (!force && currentState != null &&
                currentStateId == nextStateId)
            {
                return;
            }

            if (currentState != null)
            {
                currentState.Exit();
            }

            currentStateId = nextStateId;
            currentState = GetState(nextStateId);
            debug.TopState = nextStateId;
            SetCombatState(
                nextStateId != NightShadeSwordStateId.Idle &&
                nextStateId != NightShadeSwordStateId.Dead);
            currentState.Enter();
        }

        private INightShadeSwordState GetState(
            NightShadeSwordStateId stateId)
        {
            switch (stateId)
            {
                case NightShadeSwordStateId.Combat:
                    return combatState;
                case NightShadeSwordStateId.Hit:
                    return hitState;
                case NightShadeSwordStateId.Dead:
                    return deadState;
                default:
                    return idleState;
            }
        }

        private void SetCombatState(bool nextState)
        {
            if (isInCombat == nextState)
            {
                return;
            }

            isInCombat = nextState;
            CombatStateChanged?.Invoke();
        }

        private static int GetReactionPriority(HitReaction reaction)
        {
            switch (reaction)
            {
                case HitReaction.StaggerBreak:
                    return 5;
                case HitReaction.Knockdown:
                    return 4;
                case HitReaction.Knockback:
                    return 3;
                case HitReaction.BigHit:
                    return 2;
                case HitReaction.SmallHit:
                    return 1;
                default:
                    return 0;
            }
        }
    }
}
