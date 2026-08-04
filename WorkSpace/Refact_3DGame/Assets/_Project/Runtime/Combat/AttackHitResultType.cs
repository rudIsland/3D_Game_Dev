namespace rudIsland.RPG3D.Combat
{
    // 공격 판정이 확정된 뒤의 행동 결과 종류다.
    public enum AttackHitResultType
    {
        Ignored,
        Dodged,
        Parried,
        Guarded,
        GuardBroken,
        Damaged,
        Staggered,
        KnockedDown,
        Killed
    }
}
