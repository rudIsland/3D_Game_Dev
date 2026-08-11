using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Player.Runtime.Hit
{
    public interface IPlayerDamageReceiver
    {
        bool TryTakeDamage(AttackDamage attackDamage);
    }
}
