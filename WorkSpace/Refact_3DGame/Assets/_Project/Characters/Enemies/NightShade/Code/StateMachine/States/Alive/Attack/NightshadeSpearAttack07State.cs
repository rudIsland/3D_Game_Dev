using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack07State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack07State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack07,
                NightshadeSpearAttackGroup.Approach,
                "Attack07",
                5f,
                new AttackDamage(
                    24f,
                    1,
                    20f,
                    0.25f,
                    true),
                0.3f,
                0.64f,
                0.12f,
                1f,
                true)
        {
        }
    }
}
