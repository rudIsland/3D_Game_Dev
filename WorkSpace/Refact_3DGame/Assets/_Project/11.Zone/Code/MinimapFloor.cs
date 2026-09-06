using UnityEngine;

namespace World.Zones
{
    // 실제 플레이어 높이와 해당 층의 지도 표시 높이를 연결한다.
    [DisallowMultipleComponent]
    public sealed class MinimapFloor : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private float minimumPlayerHeight;
        [SerializeField] private float maximumPlayerHeight;
        [SerializeField] private float mapSurfaceHeight;

        public string DisplayName => displayName;
        public float MapSurfaceHeight => mapSurfaceHeight;

        public static MinimapFloor Select(MinimapFloor[] floors, float playerHeight,
            MinimapFloor current, float boundaryMargin)
        {
            if (floors == null || floors.Length == 0) return null;

            // 같은 지역의 현재 층만 경계 여유 범위를 적용한다.
            for (int i = 0; i < floors.Length; i++)
            {
                MinimapFloor floor = floors[i];
                if (floor != null && floor == current && floor.isActiveAndEnabled &&
                    playerHeight >= floor.minimumPlayerHeight - boundaryMargin &&
                    playerHeight < floor.maximumPlayerHeight + boundaryMargin)
                    return floor;
            }

            MinimapFloor nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < floors.Length; i++)
            {
                MinimapFloor floor = floors[i];
                if (floor == null || !floor.isActiveAndEnabled) continue;
                if (playerHeight >= floor.minimumPlayerHeight &&
                    playerHeight < floor.maximumPlayerHeight) return floor;

                float distance = Mathf.Min(Mathf.Abs(playerHeight - floor.minimumPlayerHeight),
                    Mathf.Abs(playerHeight - floor.maximumPlayerHeight));
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = floor;
                }
            }
            return nearest;
        }

        private void OnValidate()
        {
            maximumPlayerHeight = Mathf.Max(minimumPlayerHeight + 0.01f, maximumPlayerHeight);
        }
    }
}
