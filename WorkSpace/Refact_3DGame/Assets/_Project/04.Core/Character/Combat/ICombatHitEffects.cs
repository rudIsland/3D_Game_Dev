using UnityEngine;

namespace Core
{
    // 전투 결과를 효과 재생 담당에게 전달한다.
    public interface ICombatHitEffects
    {
        void PlayBodyHit(Vector3 hitPosition, Vector3 incomingDirection);
        void PlayGuardHit(Vector3 hitPosition, Vector3 incomingDirection);
    }
}
