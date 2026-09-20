namespace Characters
{
    public interface IEnemyDamageReceiver
    {
        // 적이 타격 결과를 받았을 때 호출된다.
        EnemyHitResult TakeHit(in EnemyHitRequest hitRequest);
    }
}
