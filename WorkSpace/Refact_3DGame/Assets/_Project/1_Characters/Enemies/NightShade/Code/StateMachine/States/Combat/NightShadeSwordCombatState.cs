// Combat 하위 단계와 공격, Recovery Action의 생명주기를 관리한다.
namespace Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordCombatState : INightShadeSwordState
    {
        private readonly NightShadeSwordBehaviorContext context;
        private readonly NightShadeSwordAttackSelectionRuntimeConfig attackSelection;
        private readonly NightShadeSwordRecoveryRuntimeConfig recovery;
        private readonly NightShadeSwordCombatDebug debug;
        private readonly NightShadeSwordActionRunner actionRunner;
        private readonly NightShadeSwordActionSelector actionSelector;
        private readonly INightShadeSwordCombatAction[] attackActions;
        private readonly INightShadeSwordCombatAction[] recoveryActions;

        private NightShadeSwordCombatPhase phase;
        private NightShadeSwordApproachMode approachMode;

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
            NightShadeSwordBehaviorContext context,
            NightShadeSwordSettings settings,
            NightShadeSwordCombatOutput combatOutput,
            INightShadeSwordRandomProvider randomProvider,
            NightShadeSwordCombatDebug debug)
        {
            this.context = context;
            attackSelection = settings.AttackSelection;
            recovery = settings.Recovery;
            this.debug = debug;
            actionRunner = new NightShadeSwordActionRunner(debug);
            actionSelector = new NightShadeSwordActionSelector(
                randomProvider,
                debug);

            attackActions = new INightShadeSwordCombatAction[4];
            attackActions[0] = new NightShadeSwordSingleAttackAction(
                NightShadeSwordActionId.Light,
                NightShadeSwordAttackType.Light,
                context,
                settings.GetAttackData(NightShadeSwordActionId.Light),
                attackSelection,
                combatOutput);
            attackActions[1] = new NightShadeSwordComboAction(
                context,
                settings.GetAttackData(NightShadeSwordActionId.Combo),
                attackSelection,
                combatOutput);
            attackActions[2] = new NightShadeSwordSingleAttackAction(
                NightShadeSwordActionId.Heavy,
                NightShadeSwordAttackType.Heavy,
                context,
                settings.GetAttackData(NightShadeSwordActionId.Heavy),
                attackSelection,
                combatOutput);
            attackActions[3] = new NightShadeSwordSingleAttackAction(
                NightShadeSwordActionId.WideSwing,
                NightShadeSwordAttackType.WideSwing,
                context,
                settings.GetAttackData(NightShadeSwordActionId.WideSwing),
                attackSelection,
                combatOutput);

            recoveryActions = new INightShadeSwordCombatAction[4];
            recoveryActions[0] = new NightShadeSwordIdleRecoveryAction(
                context,
                recovery);
            recoveryActions[1] = new NightShadeSwordMoveRecoveryAction(
                NightShadeSwordActionId.BackRecovery,
                NightShadeCombatMoveType.Backward,
                context,
                recovery);
            recoveryActions[2] = new NightShadeSwordMoveRecoveryAction(
                NightShadeSwordActionId.LeftRecovery,
                NightShadeCombatMoveType.Left,
                context,
                recovery);
            recoveryActions[3] = new NightShadeSwordMoveRecoveryAction(
                NightShadeSwordActionId.RightRecovery,
                NightShadeCombatMoveType.Right,
                context,
                recovery);
        }

        public void Enter()
        {
            EnterApproach();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            switch (phase)
            {
                case NightShadeSwordCombatPhase.Approach:
                    return UpdateApproach(deltaTime);
                case NightShadeSwordCombatPhase.PrepareAttack:
                    return UpdatePrepareAttack(deltaTime);
                case NightShadeSwordCombatPhase.Attack:
                    return UpdateAttack(deltaTime);
                case NightShadeSwordCombatPhase.Recovery:
                    return UpdateRecovery(deltaTime);
                default:
                    return NightShadeSwordStateId.Idle;
            }
        }

        public void Exit()
        {
            actionRunner.Stop(NightShadeSwordActionStopReason.Replaced);
            approachMode = NightShadeSwordApproachMode.None;
            ChangePhase(NightShadeSwordCombatPhase.None);
        }

        internal void InterruptCurrentAction(NightShadeSwordActionStopReason stopReason)
        {
            actionRunner.Stop(stopReason);
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

        private NightShadeSwordStateId? UpdateApproach(float deltaTime)
        {
            NightShadeSwordTargetStatus targetStatus = context.TargetStatus;
            if (!targetStatus.IsDetected)
            {
                return NightShadeSwordStateId.Idle;
            }

            if (targetStatus.IsInsideAttackRange)
            {
                EnterPrepareAttack();
                return null;
            }

            UpdateApproachMode();
            if (approachMode == NightShadeSwordApproachMode.Walk)
            {
                context.Movement.WalkToTarget(
                    targetStatus.TargetPosition,
                    deltaTime);
            }
            else
            {
                context.Movement.ChaseTarget(
                    targetStatus.TargetPosition,
                    deltaTime);
            }

            return null;
        }

        private NightShadeSwordStateId? UpdatePrepareAttack(float deltaTime)
        {
            NightShadeSwordTargetStatus targetStatus = context.TargetStatus;
            if (!targetStatus.IsDetected)
            {
                return NightShadeSwordStateId.Idle;
            }

            if (!targetStatus.IsInsideAttackRange)
            {
                EnterApproach();
                return null;
            }

            context.Movement.TurnToTarget(
                targetStatus.TargetPosition,
                deltaTime);
            if (!targetStatus.IsFacingAttackDirection ||
                context.CombatMemory.RemainingPostAttackDelay > 0f)
            {
                return null;
            }

            INightShadeSwordCombatAction selectedAction =
                actionSelector.Select(
                    NightShadeSwordCombatPhase.Attack,
                    attackActions,
                    attackSelection.RandomBonusMax);
            if (selectedAction == null)
            {
                return null;
            }

            ChangePhase(NightShadeSwordCombatPhase.Attack);
            actionRunner.Start(selectedAction);
            return null;
        }

        private NightShadeSwordStateId? UpdateAttack(float deltaTime)
        {
            if (!actionRunner.Update(deltaTime))
            {
                return null;
            }

            if (!context.TargetStatus.IsDetected)
            {
                return NightShadeSwordStateId.Idle;
            }

            INightShadeSwordCombatAction selectedAction =
                actionSelector.Select(
                    NightShadeSwordCombatPhase.Recovery,
                    recoveryActions,
                    recovery.RandomBonusMax);
            if (selectedAction == null)
            {
                return NightShadeSwordStateId.Idle;
            }

            ChangePhase(NightShadeSwordCombatPhase.Recovery);
            actionRunner.Start(selectedAction);
            return null;
        }

        private NightShadeSwordStateId? UpdateRecovery(float deltaTime)
        {
            if (!actionRunner.Update(deltaTime))
            {
                return null;
            }

            if (debug.PreviousActionStopReason ==
                NightShadeSwordActionStopReason.TargetLost)
            {
                return NightShadeSwordStateId.Idle;
            }

            EnterApproach();
            return null;
        }

        private void EnterApproach()
        {
            approachMode = NightShadeSwordApproachMode.None;
            ChangePhase(NightShadeSwordCombatPhase.Approach);
            if (context.TargetStatus.IsInsideAttackRange)
            {
                EnterPrepareAttack();
            }
        }

        private void EnterPrepareAttack()
        {
            approachMode = NightShadeSwordApproachMode.None;
            ChangePhase(NightShadeSwordCombatPhase.PrepareAttack);
            context.Animation.PlayIdle();
        }

        private void UpdateApproachMode()
        {
            NightShadeSwordApproachMode nextMode = approachMode;
            if (nextMode == NightShadeSwordApproachMode.None)
            {
                nextMode = context.TargetStatus.ShouldSwitchToWalk
                    ? NightShadeSwordApproachMode.Walk
                    : NightShadeSwordApproachMode.Chase;
            }
            else if (nextMode == NightShadeSwordApproachMode.Chase &&
                context.TargetStatus.ShouldSwitchToWalk)
            {
                nextMode = NightShadeSwordApproachMode.Walk;
            }
            else if (nextMode == NightShadeSwordApproachMode.Walk &&
                context.TargetStatus.ShouldSwitchToChase)
            {
                nextMode = NightShadeSwordApproachMode.Chase;
            }

            if (approachMode == nextMode)
            {
                return;
            }

            approachMode = nextMode;
            if (approachMode == NightShadeSwordApproachMode.Walk)
            {
                context.Animation.PlayWalk();
            }
            else
            {
                context.Animation.PlayChase();
            }
        }

        private void ChangePhase(NightShadeSwordCombatPhase nextPhase)
        {
            phase = nextPhase;
            debug.CombatPhase = nextPhase;
        }

        private INightShadeSwordAttackAction GetCurrentAttackAction()
        {
            return actionRunner.CurrentAction as INightShadeSwordAttackAction;
        }
    }
}
