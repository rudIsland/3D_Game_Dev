namespace Characters.Player.Combat.Hit
{
    public interface IPlayerDamageReceiver
    {
        PlayerHitResult TryTakeHit(in PlayerHitRequest hitRequest);
    }
}
