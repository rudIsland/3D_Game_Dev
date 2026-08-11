using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack10State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack10State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack10,
                NightshadeSpearAttackGroup.Heavy,
                "Attack10",
                2.8f,
                new AttackDamage(
                    32f,
                    2,
                    30f,
                    0.45f,
                    true),
                0.38f,
                0.76f,
                0.14f,
                0.9f,
                false)
        {
        }
    }
}
