using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zone;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace UI
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
        private Camera mapCamera;
        private RenderTexture textureTemplate;
        private RenderTexture viewTexture;
        private float defaultMapHeight;
        private MinimapFloor[] floors = System.Array.Empty<MinimapFloor>();
        public MinimapFloor CurrentFloor { get; private set; }
        private static readonly Quaternion DownwardRotation =
            Quaternion.Euler(90f, 0f, 0f);

        /// <summary>이 HUD가 만든 미니맵 영상이다. 공유 원본 텍스처는 변경하지 않는다.</summary>
        public RenderTexture ViewTexture => viewTexture;

        /// <summary>플레이어·표시·렌더 텍스처 연결이 완료되었는지 반환한다.</summary>
        public bool IsReady => player != null && playerMarker != null && viewTexture != null && viewTexture.IsCreated();

        // 비활성 부모 아래에서 Connect가 먼저 호출돼도 같은 카메라 설정을 사용한다.
        private void CacheCamera()
        {
            if (mapCamera != null) return;
            cameraTransform = transform;
            mapCamera = GetComponent<Camera>();
            textureTemplate = mapCamera.targetTexture;
            defaultMapHeight = mapSurfaceHeight;
        }

        /// <summary>플레이어를 연결하고 HUD 전용 렌더 텍스처를 만든다. 활성화 전에도 호출할 수 있다.</summary>
        public void Connect(Transform target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            CacheCamera();
            if (playerMarker == null || textureTemplate == null)
                throw new InvalidOperationException("미니맵의 플레이어 표시와 카메라 Render Texture를 연결하세요.");
            player = target;
            if (viewTexture == null)
            {
                viewTexture = new RenderTexture(textureTemplate) { name = "MinimapView_Runtime" };
                if (!viewTexture.Create())
                    throw new InvalidOperationException("미니맵 Render Texture를 만들지 못했습니다.");
            }
            mapCamera.targetTexture = viewTexture;
            if (isActiveAndEnabled) StartFollowing();
        }

        /// <summary>추적·씬 구독을 중지하고 이 HUD가 만든 렌더 텍스처를 해제한다.</summary>
        public void Disconnect()
        {
            StopFollowing();
            player = null;
            if (mapCamera != null) mapCamera.targetTexture = textureTemplate;
            if (viewTexture != null)
            {
                viewTexture.Release();
                Destroy(viewTexture);
                viewTexture = null;
            }
        }

        private void Awake() => CacheCamera();

        private void OnEnable()
        {
            CacheCamera();
            if (player != null) Connect(player);
            else StopFollowing();
        }

        private void LateUpdate()
        {
            if (player == null) { StopFollowing(); return; }
            UpdateMinimapPosition();
        }

        private void OnDisable() => StopFollowing();
        private void OnDestroy() => Disconnect();

        // 장면 알림은 추적 중에만 구독하며 같은 플레이어의 재연결도 중복 구독하지 않는다.
        private void StartFollowing()
        {
            StopFollowing();
            SceneManager.sceneLoaded += ReadLoadedFloors;
            SceneManager.sceneUnloaded += ReadRemainingFloors;
            ReadFloors();
            playerMarker.gameObject.SetActive(true);
            mapCamera.enabled = true;
            UpdateMinimapPosition();
        }

        private void StopFollowing()
        {
            SceneManager.sceneLoaded -= ReadLoadedFloors;
            SceneManager.sceneUnloaded -= ReadRemainingFloors;
            floors = Array.Empty<MinimapFloor>();
            CurrentFloor = null;
            if (mapCamera != null) mapCamera.enabled = false;
            if (playerMarker != null) playerMarker.gameObject.SetActive(false);
        }

        private void ReadLoadedFloors(UnityScene scene, LoadSceneMode mode) => ReadFloors();
        private void ReadRemainingFloors(UnityScene scene) => ReadFloors();

        private void ReadFloors()
        {
            floors = FindObjectsByType<MinimapFloor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            CurrentFloor = null;
            mapSurfaceHeight = defaultMapHeight;
        }

        private void UpdateMinimapPosition()
        {
            if (player == null || playerMarker == null) return;
            Vector3 playerPosition = player.position;
            CurrentFloor = MinimapFloor.Select(floors, playerPosition.y, CurrentFloor,
                floorBoundaryMargin);
            mapSurfaceHeight = CurrentFloor != null ? CurrentFloor.MapSurfaceHeight : defaultMapHeight;

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
            defaultMapHeight = surfaceHeight;
            UpdateMinimapPosition();
        }

        private void OnValidate()
        {
            cameraHeight = Mathf.Max(1f, cameraHeight);
            markerHeight = Mathf.Max(0.01f, markerHeight);
        }
    }
}
