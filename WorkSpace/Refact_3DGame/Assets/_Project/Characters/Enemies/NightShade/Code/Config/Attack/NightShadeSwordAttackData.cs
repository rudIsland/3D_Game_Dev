using rudIsland.RPG3D.Characters.Enemies.AttackData;
namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    public abstract class NightShadeSwordAttackData : EnemyAttackData
    {
        internal abstract NightShadeSwordActionId ActionId { get; }
        internal virtual float ComboFirstExitNormalizedTime => 1f;
        internal virtual float ComboSecondDelay => 0f;

        internal void Validate()
        {
            ValidateAttackData(
                ActionId == NightShadeSwordActionId.Combo ? 2 : 1);
            ValidateNightShadeAttack();
        }

        protected abstract void ValidateNightShadeAttack();
    }
}
