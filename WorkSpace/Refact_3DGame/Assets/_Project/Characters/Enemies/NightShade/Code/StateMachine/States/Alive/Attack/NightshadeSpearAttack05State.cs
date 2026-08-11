using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack05State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack05State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack05,
                NightshadeSpearAttackGroup.Sweep,
                "Attack05",
                2.8f,
                new AttackDamage(
                    22f,
                    1,
                    16f,
                    0.2f,
                    true),
                0.32f,
                0.68f,
                0.12f,
                0.95f,
                true)
        {
        }
    }
}
