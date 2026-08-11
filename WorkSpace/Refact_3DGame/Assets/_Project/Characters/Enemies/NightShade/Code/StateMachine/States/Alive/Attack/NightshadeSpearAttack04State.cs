using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack04State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack04State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack04,
                NightshadeSpearAttackGroup.Approach,
                "Attack04",
                3f,
                new AttackDamage(
                    18f,
                    1,
                    14f,
                    0.18f,
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
