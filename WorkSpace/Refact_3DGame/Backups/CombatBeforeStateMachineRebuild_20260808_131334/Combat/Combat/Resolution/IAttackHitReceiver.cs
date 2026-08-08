
using rudIsland.RPG3D.Combat.Attack;
using rudIsland.RPG3D.Combat.Result;

namespace rudIsland.RPG3D.Combat.Resolution
{
    // 공격자가 피격 대상의 구체 클래스를 몰라도 타격 정보를 전달하게 한다.
    public interface IAttackHitReceiver
    {
        bool CanTakeHit { get; }
        int ActivationSequence { get; }
        AttackHitResult ReceiveAttackHit(in AttackHitInput hit);
    }
}
