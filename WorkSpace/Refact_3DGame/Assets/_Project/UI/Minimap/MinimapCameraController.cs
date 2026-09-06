using UnityEngine;
using UnityEngine.SceneManagement;
using World.Zones;

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
        [SerializeField, Min(0f)] private float floorBoundaryMargin = 0.5f;

        private Transform cameraTransform;
        private float defaultMapHeight;
        private MinimapFloor[] floors = System.Array.Empty<MinimapFloor>();
        public MinimapFloor CurrentFloor { get; private set; }
        private static readonly Quaternion DownwardRotation =
            Quaternion.Euler(90f, 0f, 0f);

        private void Awake()
        {
            cameraTransform = transform;
            defaultMapHeight = mapSurfaceHeight;

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

        private void OnEnable()
        {
            SceneManager.sceneLoaded += ReadLoadedFloors;
            SceneManager.sceneUnloaded += ReadRemainingFloors;
            ReadFloors();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= ReadLoadedFloors;
            SceneManager.sceneUnloaded -= ReadRemainingFloors;
            floors = System.Array.Empty<MinimapFloor>();
            CurrentFloor = null;
        }

        private void ReadLoadedFloors(Scene scene, LoadSceneMode mode) => ReadFloors();
        private void ReadRemainingFloors(Scene scene) => ReadFloors();

        private void ReadFloors()
        {
            floors = FindObjectsByType<MinimapFloor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            CurrentFloor = null;
            mapSurfaceHeight = defaultMapHeight;
        }

        private void UpdateMinimapPosition()
        {
            Vector3 playerPosition = player.position;
            CurrentFloor = MinimapFloor.Select(floors, playerPosition.y, CurrentFloor,
                floorBoundaryMargin);
            if (CurrentFloor != null) mapSurfaceHeight = CurrentFloor.MapSurfaceHeight;

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
