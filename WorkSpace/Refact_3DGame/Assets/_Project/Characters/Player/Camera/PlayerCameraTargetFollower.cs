using UnityEngine;

namespace rudIsland.RPG3D.Player.Camera
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    // 플레이어의 큰 이동을 짧게 늦춰서 카메라에 전달한다.
    public sealed class PlayerCameraTargetFollower : MonoBehaviour
    {
        [SerializeField] private Transform playerCameraPoint;
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.14f;
        [SerializeField, Min(0.01f)] private float maximumFollowDistance = 0.55f;
        [SerializeField, Min(0.01f)] private float maximumFollowSpeed = 25f;

        private Vector3 followVelocity;

        private void Awake()
        {
            if (playerCameraPoint != null)
            {
                return;
            }

            Debug.LogError(
                "PlayerCameraTargetFollower에 플레이어 카메라 기준점이 필요합니다.",
                this);
            enabled = false;
        }

        private void OnEnable()
        {
            SnapToPlayer();
        }

        private void LateUpdate()
        {
            if (playerCameraPoint == null)
            {
                return;
            }

            Vector3 wantedPosition = playerCameraPoint.position;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                wantedPosition,
                ref followVelocity,
                followSmoothTime,
                maximumFollowSpeed,
                Time.deltaTime);

            LimitFollowDistance(wantedPosition);
        }

        public void SnapToPlayer()
        {
            if (playerCameraPoint == null)
            {
                return;
            }

            transform.position = playerCameraPoint.position;
            followVelocity = Vector3.zero;
        }

        private void LimitFollowDistance(Vector3 wantedPosition)
        {
            Vector3 followDifference = transform.position - wantedPosition;
            float maximumDistanceSquared =
                maximumFollowDistance * maximumFollowDistance;
            if (followDifference.sqrMagnitude <= maximumDistanceSquared)
            {
                return;
            }

            float distance = Mathf.Sqrt(followDifference.sqrMagnitude);
            transform.position = wantedPosition +
                followDifference * (maximumFollowDistance / distance);
        }

        private void OnValidate()
        {
            followSmoothTime = Mathf.Max(0.01f, followSmoothTime);
            maximumFollowDistance = Mathf.Max(0.01f, maximumFollowDistance);
            maximumFollowSpeed = Mathf.Max(0.01f, maximumFollowSpeed);
        }
    }
}
