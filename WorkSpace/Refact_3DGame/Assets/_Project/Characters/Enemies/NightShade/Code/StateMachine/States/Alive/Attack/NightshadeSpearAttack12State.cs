using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack12State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack12State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack12,
                NightshadeSpearAttackGroup.Thrust,
                "Attack12",
                5f,
                new AttackDamage(
                    29f,
                    1,
                    22f,
                    0.3f,
                    true),
                0.3f,
                0.67f,
                0.12f,
                1f,
                true)
        {
        }
    }
}
