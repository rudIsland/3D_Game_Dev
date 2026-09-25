using UnityEngine;

namespace Player
{
    // 공격 상태에는 현재 락온 대상 조회만 허용하고 시점 전환 책임은 노출하지 않는다.
    internal interface IPlayerAttackTarget
    {
        // 락온 상태의 살아 있는 대상을 반환한다.
        bool TryGetCurrentAttackTarget(out Transform target);
        // 공격 시작에 선택한 대상이 여전히 현재 락온 대상인지 확인한다.
        bool IsAttackTargetAvailable(Transform target);
    }
}
