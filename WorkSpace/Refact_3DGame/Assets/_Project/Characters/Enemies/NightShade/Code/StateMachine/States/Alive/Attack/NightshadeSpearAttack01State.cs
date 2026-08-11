using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightshadeSpearAttack01State : NightshadeSpearAttackState
    {
        internal NightshadeSpearAttack01State(
            NightshadeSpearStateMachine stateMachine)
            : base(
                stateMachine,
                NightshadeSpearAttackId.Attack01,
                NightshadeSpearAttackGroup.Thrust,
                "Attack01",
                2.2f,
                new AttackDamage(15f, 0, 8f, 0.1f,true),
                0.25f,
                0.55f,
                0.08f,
                1f,
                true)
        {
        }
    }
}
