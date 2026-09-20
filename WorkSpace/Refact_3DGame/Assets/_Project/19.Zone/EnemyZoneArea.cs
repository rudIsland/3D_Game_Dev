using System;
using UnityEngine;

namespace World.Zones
{
    // BoxCollider의 XZ 범위를 적이 활동할 수 있는 영역으로 제공한다.
    public sealed class EnemyZoneArea
    {
        private readonly BoxCollider zoneCollider;

        public EnemyZoneArea(BoxCollider zoneCollider)
        {
            this.zoneCollider = zoneCollider != null
                ? zoneCollider
                : throw new ArgumentNullException(nameof(zoneCollider));
        }

        // 높이는 무시하고 회전과 크기가 반영된 BoxCollider 안에 있는지 확인한다.
        public bool Contains(Vector3 worldPosition, float outsideMargin = 0f)
        {
            Vector3 localPosition =
                zoneCollider.transform.InverseTransformPoint(worldPosition) -
                zoneCollider.center;
            Vector3 halfSize = zoneCollider.size * 0.5f;
            Vector3 worldScale = zoneCollider.transform.lossyScale;
            float safeMargin = Mathf.Max(0f, outsideMargin);
            float localMarginX = safeMargin / Mathf.Max(Mathf.Abs(worldScale.x), 0.0001f);
            float localMarginZ = safeMargin / Mathf.Max(Mathf.Abs(worldScale.z), 0.0001f);

            return Mathf.Abs(localPosition.x) <= halfSize.x + localMarginX &&
                Mathf.Abs(localPosition.z) <= halfSize.z + localMarginZ;
        }
    }
}
