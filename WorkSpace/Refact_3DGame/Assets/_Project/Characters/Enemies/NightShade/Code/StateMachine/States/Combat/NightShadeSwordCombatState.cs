// Combat 하위 단계의 전환과 Action 선택을 관리한다.
namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // Combat 하위 단계와 현재 Action 하나만 관리한다.
    internal sealed class NightShadeSwordCombatState : INightShadeSwordState
    {
        private readonly NightShadeSwordSituationReader situation;
        private readonly NightShadeSwordFightMemory fightMemory;
        private readonly NightShadeSwordCombatDebug debug;
        private readonly NightShadeSwordActionRunner actionRunner;
        private readonly NightShadeSwordActionSelector actionSelector;
        private readonly INightShadeSwordCombatAction[] positioningActions;
        private readonly INightShadeSwordCombatAction[] attackActions;
        private readonly INightShadeSwordCombatAction[] recoveryActions;

        private NightShadeSwordCombatPhase phase;
        private NightShadeSwordActionId lastPositioningAction;

        internal NightShadeSwordCombatPhase Phase => phase;
        internal NightShadeSwordActionId CurrentActionId =>
            actionRunner.CurrentActionId;
        internal bool IsAttackActionActive =>
            actionRunner.CurrentAction is INightShadeSwordAttackAction;
        internal bool ProtectsSmallHit
        {
            get
            {
                INightShadeSwordAttackAction attackAction =
                    actionRunner.CurrentAction as INightShadeSwordAttackAction;
                return attackAction != null && attackAction.ProtectsSmallHit;
            }
        }

        internal NightShadeSwordCombatState(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordActions actions,
            INightShadeSwordRandomProvider randomProvider,
            NightShadeSwordCombatDebug debug)
        {
            this.situation = situation;
            this.fightMemory = fightMemory;
            this.debug = debug;
            actionRunner = new NightShadeSwordActionRunner(
                situation,
                fightMemory,
                debug);
            actionSelector = new NightShadeSwordActionSelector(
                randomProvider,
                settings,
                debug);

            positioningActions = new INightShadeSwordCombatAction[3];
            positioningActions[0] = new NightShadeSwordChaseAction(
                situation,
                fightMemory,
                movement,
                animation,
                settings);
            positioningActions[1] = new NightShadeSwordWalkApproachAction(
                situation,
                fightMemory,
                movement,
                animation,
                settings);
            positioningActions[2] = new NightShadeSwordWatchTargetAction(
                situation,
                fightMemory,
                movement,
                animation,
                settings);

            attackActions = new INightShadeSwordCombatAction[4];
            attackActions[0] = new NightShadeSwordSingleAttackAction(
                NightShadeSwordActionId.Light,
                NightShadeSwordAttackType.Light,
                situation,
                fightMemory,
                movement,
                animation,
                settings,
                actions);
            attackActions[1] = new NightShadeSwordComboAction(
                situation,
                fightMemory,
                movement,
                animation,
                settings,
                actions);
            attackActions[2] = new NightShadeSwordSingleAttackAction(
                NightShadeSwordActionId.Heavy,
                NightShadeSwordAttackType.Heavy,
                situation,
                fightMemory,
                movement,
                animation,
                settings,
                actions);
            attackActions[3] = new NightShadeSwordSingleAttackAction(
                NightShadeSwordActionId.WideSwing,
                NightShadeSwordAttackType.WideSwing,
                situation,
                fightMemory,
                movement,
                animation,
                settings,
                actions);

            recoveryActions = new INightShadeSwordCombatAction[4];
            recoveryActions[0] = new NightShadeSwordIdleRecoveryAction(
                situation,
                fightMemory,
                movement,
                animation,
                settings);
            recoveryActions[1] = new NightShadeSwordMoveRecoveryAction(
                NightShadeSwordActionId.BackRecovery,
                NightShadeCombatMoveType.Backward,
                situation,
                fightMemory,
                movement,
                animation,
                settings);
            recoveryActions[2] = new NightShadeSwordMoveRecoveryAction(
                NightShadeSwordActionId.LeftRecovery,
                NightShadeCombatMoveType.Left,
                situation,
                fightMemory,
                movement,
                animation,
                settings);
            recoveryActions[3] = new NightShadeSwordMoveRecoveryAction(
                NightShadeSwordActionId.RightRecovery,
                NightShadeCombatMoveType.Right,
                situation,
                fightMemory,
                movement,
                animation,
                settings);
        }

        public void Enter()
        {
            phase = NightShadeSwordCombatPhase.Positioning;
            lastPositioningAction = NightShadeSwordActionId.None;
            debug.CombatPhase = phase;
            StartPositioningAction();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            if (!actionRunner.HasAction)
            {
                return StartCurrentPhaseAction();
            }

            NightShadeSwordCombatPhase finishedPhase = phase;
            NightShadeSwordActionId finishedAction =
                actionRunner.CurrentActionId;
            if (!actionRunner.Update(deltaTime))
            {
                return null;
            }

            if (debug.PreviousActionStopReason ==
                    NightShadeSwordActionStopReason.TargetLost &&
                finishedPhase != NightShadeSwordCombatPhase.Attack)
            {
                return NightShadeSwordStateId.Idle;
            }

            switch (finishedPhase)
            {
                case NightShadeSwordCombatPhase.Positioning:
                    if (finishedAction == NightShadeSwordActionId.WatchTarget &&
                        debug.PreviousActionStopReason ==
                            NightShadeSwordActionStopReason.Completed)
                    {
                        return StartAttackDecision();
                    }

                    phase = NightShadeSwordCombatPhase.Positioning;
                    debug.CombatPhase = phase;
                    return StartPositioningAction()
                        ? null
                        : NightShadeSwordStateId.Idle;

                case NightShadeSwordCombatPhase.Attack:
                    if (!situation.IsTargetDetected)
                    {
                        return NightShadeSwordStateId.Idle;
                    }

                    phase = NightShadeSwordCombatPhase.Recovery;
                    debug.CombatPhase = phase;
                    return StartRecoveryAction()
                        ? null
                        : NightShadeSwordStateId.Idle;

                case NightShadeSwordCombatPhase.Recovery:
                    phase = NightShadeSwordCombatPhase.Positioning;
                    debug.CombatPhase = phase;
                    return StartPositioningAction()
                        ? null
                        : NightShadeSwordStateId.Idle;
                default:
                    return NightShadeSwordStateId.Idle;
            }
        }

        public void Exit()
        {
            actionRunner.Stop(NightShadeSwordActionStopReason.Replaced);
            phase = NightShadeSwordCombatPhase.None;
            debug.CombatPhase = phase;
        }

        internal void InterruptCurrentAction(
            NightShadeSwordActionStopReason stopReason)
        {
            actionRunner.Stop(stopReason);
        }

        internal void Disable()
        {
            actionRunner.Stop(NightShadeSwordActionStopReason.Disabled);
            phase = NightShadeSwordCombatPhase.None;
            debug.CombatPhase = phase;
        }

        internal void QueueStopTurn()
        {
            GetCurrentAttackAction()?.QueueStopTurn();
        }

        internal void QueuePlaySound(int hitIndex)
        {
            GetCurrentAttackAction()?.QueuePlaySound(hitIndex);
        }

        internal void QueueOpenHit(int hitIndex)
        {
            GetCurrentAttackAction()?.QueueOpenHit(hitIndex);
        }

        internal void QueueCloseHit()
        {
            GetCurrentAttackAction()?.QueueCloseHit();
        }

        private NightShadeSwordStateId? StartCurrentPhaseAction()
        {
            switch (phase)
            {
                case NightShadeSwordCombatPhase.Positioning:
                    return StartPositioningAction()
                        ? null
                        : NightShadeSwordStateId.Idle;
                case NightShadeSwordCombatPhase.Decision:
                    return StartAttackDecision();
                case NightShadeSwordCombatPhase.Recovery:
                    return StartRecoveryAction()
                        ? null
                        : NightShadeSwordStateId.Idle;
                default:
                    return NightShadeSwordStateId.Idle;
            }
        }

        private bool StartPositioningAction()
        {
            debug.BeginEvaluation(
                NightShadeSwordCombatPhase.Positioning,
                positioningActions.Length);
            for (int index = 0; index < positioningActions.Length; index++)
            {
                INightShadeSwordCombatAction candidate =
                    positioningActions[index];
                bool canStart = candidate.CanStart(
                    situation,
                    fightMemory,
                    out NightShadeSwordActionRejectReason rejectReason);
                NightShadeSwordActionScore score = default;
                debug.SetCandidate(
                    index,
                    candidate.ActionId,
                    canStart,
                    rejectReason,
                    in score);
            }

            int selectedIndex = GetPositioningActionIndex();
            if (selectedIndex < 0)
            {
                return false;
            }

            debug.SelectCandidate(selectedIndex);
            INightShadeSwordCombatAction selectedAction =
                positioningActions[selectedIndex];
            lastPositioningAction = selectedAction.ActionId;
            return actionRunner.Start(selectedAction);
        }

        private int GetPositioningActionIndex()
        {
            if (!situation.IsTargetDetected)
            {
                return -1;
            }

            if (situation.IsInsideAttackRange)
            {
                return 2;
            }

            if (lastPositioningAction == NightShadeSwordActionId.Chase)
            {
                return positioningActions[1].CanStart(
                    situation,
                    fightMemory,
                    out _)
                        ? 1
                        : 0;
            }

            if (lastPositioningAction == NightShadeSwordActionId.WalkApproach)
            {
                return positioningActions[1].CanStart(
                    situation,
                    fightMemory,
                    out _)
                        ? 1
                        : 0;
            }

            return positioningActions[0].CanStart(
                situation,
                fightMemory,
                out _)
                    ? 0
                    : 1;
        }

        private NightShadeSwordStateId? StartAttackDecision()
        {
            // Decision은 시간을 소모하는 Action이 아니라 이 Tick에서 점수만 계산하는 순간 단계다.
            phase = NightShadeSwordCombatPhase.Decision;
            debug.CombatPhase = phase;
            INightShadeSwordCombatAction selectedAction =
                actionSelector.Select(
                    attackActions,
                    attackActions.Length,
                    situation,
                    fightMemory);
            if (selectedAction == null)
            {
                phase = NightShadeSwordCombatPhase.Positioning;
                debug.CombatPhase = phase;
                return StartPositioningAction()
                    ? null
                    : NightShadeSwordStateId.Idle;
            }

            phase = NightShadeSwordCombatPhase.Attack;
            debug.CombatPhase = phase;
            actionRunner.Start(selectedAction);
            return null;
        }

        private bool StartRecoveryAction()
        {
            INightShadeSwordCombatAction selectedAction =
                actionSelector.Select(
                    recoveryActions,
                    recoveryActions.Length,
                    situation,
                    fightMemory);
            return actionRunner.Start(selectedAction);
        }

        private INightShadeSwordAttackAction GetCurrentAttackAction()
        {
            return actionRunner.CurrentAction as INightShadeSwordAttackAction;
        }
    }
}
