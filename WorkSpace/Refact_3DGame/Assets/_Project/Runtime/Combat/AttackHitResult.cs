namespace rudIsland.RPG3D.Combat
{
    // 공격이 대상에게 전달된 뒤 결정된 최종 결과다.
    public enum AttackHitResult
    {
        Ignored,
        Dodged,
        Blocked,
        Damaged,
        Staggered,
        Killed
    }
}
