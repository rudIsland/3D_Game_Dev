using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack08State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack08State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack08,
                NightshadeSpearAttackGroup.Sweep,
                "Attack08",
                3.2f,
                new AttackDamage(
                    25f,
                    1,
                    22f,
                    0.25f,
                    true),
                0.3f,
                0.68f,
                0.12f,
                0.95f,
                false)
        {
        }
    }
}
