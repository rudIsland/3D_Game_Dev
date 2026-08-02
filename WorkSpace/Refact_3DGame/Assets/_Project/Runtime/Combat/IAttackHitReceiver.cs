namespace rudIsland.RPG3D.Combat
{
    // 공격자가 피격 대상의 구체 클래스를 몰라도 타격 정보를 전달하게 한다.
    public interface IAttackHitReceiver
    {
        AttackHitResult ReceiveHit(in AttackHitData hit);
    }
}
