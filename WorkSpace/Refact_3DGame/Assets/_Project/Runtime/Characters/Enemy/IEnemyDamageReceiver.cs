using UnityEngine;

namespace rudIsland.RPG3D.Characters
{
    public interface IEnemyDamageReceiver
    {
        // 적이 피해를 받았을 때 호출된다.
        void TakeDamage(float damage, Vector3 hitPosition);
    }
}