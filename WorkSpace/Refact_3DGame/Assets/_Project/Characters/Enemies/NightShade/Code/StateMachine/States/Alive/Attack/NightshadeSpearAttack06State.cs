using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack06State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack06State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack06,
                NightshadeSpearAttackGroup.Thrust,
                "Attack06",
                2.4f,
                new AttackDamage(
                    17f,
                    0,
                    10f,
                    0.1f,
                    true),
                0.24f,
                0.56f,
                0.08f,
                1.05f,
                true)
        {
        }
    }
}
