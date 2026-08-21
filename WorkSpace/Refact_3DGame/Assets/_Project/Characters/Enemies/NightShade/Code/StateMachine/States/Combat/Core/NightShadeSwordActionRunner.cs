// 현재 전투 Action의 생명주기를 관리한다.
namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 현재 Action 하나의 Enter -> Update -> Exit 호출 순서를 보장한다.
    internal sealed class NightShadeSwordActionRunner
    {
        private readonly NightShadeSwordSituationReader situation;
        private readonly NightShadeSwordFightMemory fightMemory;
        private readonly NightShadeSwordCombatDebug debug;

        private INightShadeSwordCombatAction currentAction;

        internal bool HasAction => currentAction != null;
        internal INightShadeSwordCombatAction CurrentAction => currentAction;
        internal NightShadeSwordActionId CurrentActionId => currentAction != null
            ? currentAction.ActionId
            : NightShadeSwordActionId.None;

        internal NightShadeSwordActionRunner(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            NightShadeSwordCombatDebug debug)
        {
            this.situation = situation;
            this.fightMemory = fightMemory;
            this.debug = debug;
        }

        internal bool Start(INightShadeSwordCombatAction action)
        {
            if (action == null || currentAction != null)
            {
                return false;
            }

            currentAction = action;
            debug.CurrentAction = action.ActionId;
            debug.CurrentActionStopReason = NightShadeSwordActionStopReason.None;
            action.Enter();
            return true;
        }

        internal bool Update(float deltaTime)
        {
            if (currentAction == null)
            {
                return false;
            }

            if (!currentAction.CanContinue(
                    situation,
                    fightMemory,
                    out NightShadeSwordActionStopReason stopReason))
            {
                Stop(stopReason);
                return true;
            }

            currentAction.Update(deltaTime);
            if (!currentAction.IsFinished)
            {
                return false;
            }

            Stop(NightShadeSwordActionStopReason.Completed);
            return true;
        }

        internal void Stop(NightShadeSwordActionStopReason stopReason)
        {
            if (currentAction == null)
            {
                return;
            }

            INightShadeSwordCombatAction stoppedAction = currentAction;
            currentAction = null;
            stoppedAction.Exit(stopReason);
            debug.PreviousActionStopReason = stopReason;
            debug.CurrentActionStopReason = stopReason;
            debug.CurrentAction = NightShadeSwordActionId.None;
        }
    }
}
