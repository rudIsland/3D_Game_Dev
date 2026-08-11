using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack09State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack09State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack09,
                NightshadeSpearAttackGroup.Sweep,
                "Attack09",
                2.6f,
                new AttackDamage(
                    20f,
                    1,
                    18f,
                    0.2f,
                    true),
                0.28f,
                0.62f,
                0.1f,
                1f,
                true)
        {
        }
    }
}
