using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack13State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack13State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack13,
                NightshadeSpearAttackGroup.Finisher,
                "Attack13",
                3.5f,
                new AttackDamage(
                    40f,
                    2,
                    40f,
                    0.55f,
                    true),
                0.4f,
                0.8f,
                0.16f,
                0.88f,
                false)
        {
        }
    }
}
