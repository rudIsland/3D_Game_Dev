using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack11State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack11State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack11,
                NightshadeSpearAttackGroup.Retreat,
                "Attack11",
                4f,
                new AttackDamage(
                    27f,
                    1,
                    24f,
                    0.3f,
                    true),
                0.3f,
                0.68f,
                0.12f,
                1f,
                true)
        {
        }
    }
}
