using UnityEngine;

namespace GameUI.Minimap
{
    // 플레이어 위치를 미니맵 카메라와 방향 표시에 연결한다.
    [RequireComponent(typeof(Camera))]
    public sealed class MinimapCameraController : MonoBehaviour
    {
        [Header("필수 연결")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform playerMarker;

        [Header("지도 높이")]
        [SerializeField] private float mapSurfaceHeight = 52.11f;
        [SerializeField] private float cameraHeight = 30f;
        [SerializeField] private float markerHeight = 0.3f;

        private Transform cameraTransform;
        private static readonly Quaternion DownwardRotation =
            Quaternion.Euler(90f, 0f, 0f);

        private void Awake()
        {
            cameraTransform = transform;

            if (player == null || playerMarker == null)
            {
                Debug.LogError(
                    "MinimapCameraController에 Player와 Player Marker 연결이 필요합니다.",
                    this);
                enabled = false;
                return;
            }

            UpdateMinimapPosition();
        }

        private void LateUpdate()
        {
            UpdateMinimapPosition();
        }

        private void UpdateMinimapPosition()
        {
            Vector3 playerPosition = player.position;

            cameraTransform.SetPositionAndRotation(
                new Vector3(
                    playerPosition.x,
                    mapSurfaceHeight + cameraHeight,
                    playerPosition.z),
                DownwardRotation);

            playerMarker.SetPositionAndRotation(
                new Vector3(
                    playerPosition.x,
                    mapSurfaceHeight + markerHeight,
                    playerPosition.z),
                Quaternion.AngleAxis(
                    player.eulerAngles.y,
                    Vector3.up));
        }

        public void ConnectForEditor(
            Transform playerTransform,
            Transform markerTransform,
            float surfaceHeight)
        {
            player = playerTransform;
            playerMarker = markerTransform;
            mapSurfaceHeight = surfaceHeight;
            cameraTransform = transform;
            UpdateMinimapPosition();
        }

        private void OnValidate()
        {
            cameraHeight = Mathf.Max(1f, cameraHeight);
            markerHeight = Mathf.Max(0.01f, markerHeight);
        }
    }
}
