using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack03State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack03State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack03,
                NightshadeSpearAttackGroup.Sweep,
                "Attack03",
                2.3f,
                new AttackDamage(
                    16f,
                    0,
                    9f,
                    0.1f,
                    true),
                0.22f,
                0.58f,
                0.08f,
                1f,
                true)
        {
        }
    }
}
