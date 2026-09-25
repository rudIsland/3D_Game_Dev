namespace Core
{
    public interface IPlayerDamageReceiver
    {
        PlayerHitResult TryTakeHit(in PlayerHitRequest hitRequest);
    }
}
