namespace rudIsland.RPG3D.Player.Runtime.Hit
{
    public interface IPlayerDamageReceiver
    {
        PlayerHitResult TryTakeHit(in PlayerHitRequest hitRequest);
    }
}
