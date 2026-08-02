using rudIsland.RPG3D.Characters;

namespace rudIsland.RPG3D.Combat
{
    // 한 번의 타격에 필요한 피해량과 공격자 정보를 함께 전달한다.
    public readonly struct AttackHitData
    {
        public AttackDamage Damage { get; } // 실제 피해 구조
        public UnitTeam AttackerTeam { get; } //아군 or 적
        public int AttackNumber { get; } // 1부터 시작하는 공격 순서

        public AttackHitData(
            AttackDamage damage,
            UnitTeam attackerTeam,
            int attackNumber)
        {
            Damage = damage;
            AttackerTeam = attackerTeam;
            AttackNumber = attackNumber;
        }
    }
}
