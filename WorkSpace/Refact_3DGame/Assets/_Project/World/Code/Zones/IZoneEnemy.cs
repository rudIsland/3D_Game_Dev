using UnityEngine;

namespace World.Zones
{
    // 풀에서 꺼낸 적에게 활동 영역과 돌아갈 위치를 전달한다.
    public interface IZoneEnemy
    {
        EnemyZoneArea HomeZone { get; }

        void SetHomeZone(
            EnemyZoneArea homeZone,
            Vector3 homePosition);
    }
}
