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
        Idle = 0,       //서있기
        Chase = 1,      //추격
        Walk = 2,       //걷기접근
        Attack = 3,     //공격
        CombatMove = 4, //전투움직임
        Hit = 5,        //피격
        Dead = 6        //죽음
    }

    // 상태 객체를 보관하고 Exit -> Enter 순서의 전환만 관리한다.
    internal sealed class NightShadeSwordStateMachine
    {
        private readonly INightShadeSwordState[] states;
        private readonly NightShadeSwordTargetReader targetReader;
        private readonly NightShadeSwordFightMemory fightMemory;
        private readonly NightShadeSwordActions actions;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordAttackState attackState;
        private readonly NightShadeSwordHitState hitState;

        private INightShadeSwordState currentState;
        private NightShadeSwordStateId currentStateId;
        private bool hasCurrentState;
        private bool isEnabled;
        private bool isInCombat;
        internal bool IsInCombat => isInCombat;
        internal bool ProtectsSmallHit =>
            IsAttackStateActive &&
            attackState.ProtectsSmallHit;
        internal float StopDamageScale =>
            ProtectsSmallHit ? 0.5f : 1f;
        internal bool IsAttackStateActive =>
            isEnabled && hasCurrentState &&
            currentStateId == NightShadeSwordStateId.Attack;
        internal NightShadeSwordStateId CurrentStateId => currentStateId;
        internal NightShadeSwordFightMemory FightMemory => fightMemory;

        internal event Action CombatStateChanged;

        internal NightShadeSwordStateMachine(
            Transform target,
            IUnitDeathState targetDeathState,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordActions actions)
        {
            this.movement = movement;
            this.animation = animation;
            this.actions = actions;
            targetReader = new NightShadeSwordTargetReader(
                target,
                targetDeathState,
                movement);
            fightMemory = new NightShadeSwordFightMemory();
            var attackSelector = new NightShadeSwordAttackSelector(settings.AttackRangeSquared);

            states = new INightShadeSwordState[7];

            states[(int)NightShadeSwordStateId.Idle] =
                new NightShadeSwordIdleState(targetReader, movement, animation, settings);

            states[(int)NightShadeSwordStateId.Chase] =
                new NightShadeSwordChaseState(
                    targetReader,
                    movement,
                    animation,
                    settings);
            states[(int)NightShadeSwordStateId.Walk] =
                new NightShadeSwordWalkState(
                    targetReader,
                    movement,
                    animation,
                    settings,
                    fightMemory);
            attackState = new NightShadeSwordAttackState(
                targetReader,
                movement,
                animation,
                settings,
                fightMemory,
                attackSelector,
                actions);
            states[(int)NightShadeSwordStateId.Attack] = attackState;
            states[(int)NightShadeSwordStateId.CombatMove] =
                new NightShadeSwordCombatMoveState(
                    targetReader,
                    movement,
                    animation,
                    settings,
                    fightMemory);
            hitState = new NightShadeSwordHitState(
                targetReader,
                movement,
                animation,
                settings,
                fightMemory);
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
            movement.Reset();
            animation.ResetAttackPlaybackSpeed();
            fightMemory.Reset();
            targetReader.Refresh();
            SetCombatState(false);
            ChangeState(NightShadeSwordStateId.Idle, true);
        }

        internal void Disable()
        {
            isEnabled = false;
            fightMemory.CancelComboSecond();
            if (hasCurrentState)
            {
                currentState.Exit();
                currentState = null;
                hasCurrentState = false;
            }

            actions.CloseAttackHit();
            animation.ResetAttackPlaybackSpeed();
            SetCombatState(false);
        }

        internal void Update(float deltaTime)
        {
            if (!isEnabled || !hasCurrentState)
            {
                return;
            }

            targetReader.Refresh();
            fightMemory.UpdateAttackCooldown(deltaTime);
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
            if (hasCurrentState &&
                currentStateId == NightShadeSwordStateId.Dead)
            {
                return;
            }

            if (hasCurrentState &&
                currentStateId == NightShadeSwordStateId.Hit)
            {
                hitState.TryRestart(reaction, in hitRequest);
                return;
            }

            hitState.SetHitRequest(reaction, in hitRequest);
            ChangeState(NightShadeSwordStateId.Hit);
        }

        internal void ChangeToDeadState()
        {
            if (hasCurrentState &&
                currentStateId == NightShadeSwordStateId.Dead)
            {
                return;
            }

            ChangeState(NightShadeSwordStateId.Dead);
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
                attackState.QueueStopTurn();
            }
        }

        internal void PlayAttackSoundAnimationEvent(int hitIndex)
        {
            if (IsAttackStateActive)
            {
                attackState.QueuePlaySound(hitIndex);
            }
        }

        internal void OpenAttackHitAnimationEvent(int hitIndex)
        {
            if (IsAttackStateActive)
            {
                attackState.QueueOpenHit(hitIndex);
            }
        }

        internal void CloseAttackHitAnimationEvent()
        {
            if (IsAttackStateActive)
            {
                attackState.QueueCloseHit();
            }
        }

        private void ChangeState(NightShadeSwordStateId nextStateId, bool force = false)
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
            SetCombatState(nextStateId != NightShadeSwordStateId.Idle && nextStateId != NightShadeSwordStateId.Dead);
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
    }
}
