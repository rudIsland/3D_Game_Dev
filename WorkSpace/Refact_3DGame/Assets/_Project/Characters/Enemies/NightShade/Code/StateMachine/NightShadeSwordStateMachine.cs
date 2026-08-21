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
        private readonly INightShadeSwordState[] states;
        private readonly NightShadeSwordSituationReader situation;
        private readonly NightShadeSwordFightMemory fightMemory;
        private readonly NightShadeSwordActions actions;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordCombatState combatState;
        private readonly NightShadeSwordHitState hitState;
        private readonly NightShadeSwordCombatDebug debug;

        private INightShadeSwordState currentState;
        private NightShadeSwordStateId currentStateId;
        private EnemyHitRequest pendingHitRequest;
        private HitReaction pendingHitReaction;
        private bool hasCurrentState;
        private bool hasPendingHit;
        private bool hasPendingDeath;
        private bool isEnabled;
        private bool isInCombat;

        internal bool IsInCombat => isInCombat;
        internal bool ProtectsSmallHit => IsAttackStateActive && combatState.ProtectsSmallHit;
        internal float StopDamageScale => ProtectsSmallHit ? 0.5f : 1f;
        internal bool IsAttackStateActive =>
            isEnabled && hasCurrentState &&
            currentStateId == NightShadeSwordStateId.Combat &&
            combatState.IsAttackActionActive;
        internal NightShadeSwordStateId CurrentStateId => currentStateId;
        internal NightShadeSwordCombatPhase CurrentCombatPhase => combatState.Phase;
        internal NightShadeSwordActionId CurrentActionId => combatState.CurrentActionId;
        internal NightShadeSwordFightMemory FightMemory => fightMemory;
        internal NightShadeSwordCombatDebug Debug => debug;

        internal event Action CombatStateChanged;

        internal NightShadeSwordStateMachine(
            Transform target,
            IUnitDeathState targetDeathState,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordActions actions,
            INightShadeSwordRandomProvider randomProvider = null)
        {
            this.movement = movement;
            this.animation = animation;
            this.actions = actions;
            fightMemory = new NightShadeSwordFightMemory();
            debug = new NightShadeSwordCombatDebug();
            situation = new NightShadeSwordSituationReader(
                target,
                targetDeathState,
                movement,
                settings);
            combatState = new NightShadeSwordCombatState(
                situation,
                fightMemory,
                movement,
                animation,
                settings,
                actions,
                randomProvider ?? new UnityNightShadeSwordRandomProvider(),
                debug);
            hitState = new NightShadeSwordHitState(
                situation,
                movement,
                animation,
                settings,
                fightMemory);

            states = new INightShadeSwordState[4];
            states[(int)NightShadeSwordStateId.Idle] =
                new NightShadeSwordIdleState(
                    situation,
                    movement,
                    animation);
            states[(int)NightShadeSwordStateId.Combat] = combatState;
            states[(int)NightShadeSwordStateId.Hit] = hitState;
            states[(int)NightShadeSwordStateId.Dead] =
                new NightShadeSwordDeadState(
                    movement,
                    animation,
                    settings,
                    fightMemory,
                    actions);
        }

        internal void Enable()
        {
            isEnabled = true;
            hasPendingHit = false;
            hasPendingDeath = false;
            pendingHitReaction = HitReaction.None;
            movement.Reset();
            animation.ResetAttackPlaybackSpeed();
            fightMemory.Reset();
            situation.Refresh();
            debug.Reset();
            SetCombatState(false);
            ChangeState(NightShadeSwordStateId.Idle, true);
        }

        internal void Disable()
        {
            isEnabled = false;
            hasPendingHit = false;
            hasPendingDeath = false;
            fightMemory.ClearCombo();
            if (hasCurrentState)
            {
                if (currentStateId == NightShadeSwordStateId.Combat)
                {
                    combatState.Disable();
                }

                currentState.Exit();
                currentState = null;
                hasCurrentState = false;
            }

            actions.CloseAttackHit();
            animation.ResetAttackPlaybackSpeed();
            debug.CurrentAction = NightShadeSwordActionId.None;
            debug.CombatPhase = NightShadeSwordCombatPhase.None;
            SetCombatState(false);
        }

        internal void Update(float deltaTime, bool isHitStopActive = false)
        {
            if (!isEnabled || !hasCurrentState)
            {
                return;
            }

            // 1. 한 Tick에서 공유할 타겟 상황을 먼저 갱신한다.
            situation.Refresh();
            // 2. 일반 상태보다 우선하는 사망과 피격 요청을 처리한다.
            ProcessForcedReaction();
            if (isHitStopActive)
            {
                return;
            }

            // 3. Hit Stop이 아닐 때만 후딜과 현재 상태의 시간을 진행한다.
            fightMemory.UpdatePostAttackDelay(deltaTime);
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
                (hasCurrentState &&
                    currentStateId == NightShadeSwordStateId.Dead))
            {
                return;
            }

            if (!hasPendingHit ||
                GetReactionPriority(reaction) >=
                    GetReactionPriority(pendingHitReaction))
            {
                pendingHitReaction = reaction;
                pendingHitRequest = hitRequest;
                hasPendingHit = true;
            }
        }

        internal void ChangeToDeadState()
        {
            if (hasCurrentState &&
                currentStateId == NightShadeSwordStateId.Dead)
            {
                return;
            }

            hasPendingDeath = true;
            hasPendingHit = false;
            pendingHitReaction = HitReaction.None;
        }

        internal void NotifyDamaged()
        {
            if (!hasCurrentState ||
                currentStateId != NightShadeSwordStateId.Dead)
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
                hasPendingHit = false;
                pendingHitReaction = HitReaction.None;
                InterruptCombatAction();
                ChangeState(NightShadeSwordStateId.Dead);
                return;
            }

            if (!hasPendingHit ||
                currentStateId == NightShadeSwordStateId.Dead)
            {
                return;
            }

            HitReaction reaction = pendingHitReaction;
            EnemyHitRequest hitRequest = pendingHitRequest;
            hasPendingHit = false;
            pendingHitReaction = HitReaction.None;

            if (currentStateId == NightShadeSwordStateId.Hit)
            {
                hitState.TryRestart(reaction, in hitRequest);
                return;
            }

            InterruptCombatAction();
            hitState.SetHitRequest(reaction, in hitRequest);
            ChangeState(NightShadeSwordStateId.Hit);
        }

        private void InterruptCombatAction()
        {
            if (hasCurrentState &&
                currentStateId == NightShadeSwordStateId.Combat)
            {
                combatState.InterruptCurrentAction(
                    NightShadeSwordActionStopReason.Interrupted);
            }

            actions.CloseAttackHit();
            animation.ResetAttackPlaybackSpeed();
            fightMemory.ClearCombo();
        }

        private void ChangeState(
            NightShadeSwordStateId nextStateId,
            bool force = false)
        {
            if (!force && hasCurrentState &&
                currentStateId == nextStateId)
            {
                return;
            }

            if (hasCurrentState)
            {
                currentState.Exit();
            }

            currentStateId = nextStateId;
            currentState = states[(int)nextStateId];
            hasCurrentState = true;
            debug.TopState = nextStateId;
            SetCombatState(
                nextStateId != NightShadeSwordStateId.Idle &&
                nextStateId != NightShadeSwordStateId.Dead);
            currentState.Enter();
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
