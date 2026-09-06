namespace Characters.Enemies.NightShade
{
    // 여러 State와 Action이 공유하는 런타임 동작 경계만 보관한다.
    internal sealed class NightShadeSwordBehaviorContext
    {
        internal NightShadeSwordTargetStatus TargetStatus { get; }
        internal NightShadeSwordCombatMemory CombatMemory { get; }
        internal INightShadeSwordMovement Movement { get; }
        internal INightShadeSwordAnimation Animation { get; }

        internal NightShadeSwordBehaviorContext(
            NightShadeSwordTargetStatus targetStatus,
            NightShadeSwordCombatMemory combatMemory,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation)
        {
            TargetStatus = targetStatus;
            CombatMemory = combatMemory;
            Movement = movement;
            Animation = animation;
        }
    }
}
