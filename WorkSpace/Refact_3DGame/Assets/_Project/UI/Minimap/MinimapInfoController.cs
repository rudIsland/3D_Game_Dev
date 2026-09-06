using UnityEngine;
using UnityEngine.UIElements;
using World.Quests;
using World.Zones;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace GameUI.Minimap
{
    // 플레이어의 현재 영역과 퀘스트 진행을 미니맵 아래에 표시한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MinimapInfoController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform zonesRoot;
        [SerializeField] private Transform roadsRoot;
        [SerializeField] private GroundQuestController quest;
        [SerializeField] private MinimapCameraController minimapCamera;

        private UIDocument document;
        private MapArea[] zones = System.Array.Empty<MapArea>();
        private MapArea[] roads = System.Array.Empty<MapArea>();
        private VisualElement documentRoot;
        private Label locationText;
        private Label bookText;
        private Label exchangeText;
        private Label exitText;
        private Label hintText;
        private MapArea currentArea;
        private string displayedAreaName;
        private bool locationShown;
        private float nextLocationUpdate;

        private void Awake()
        {
            document = GetComponent<UIDocument>();
            if (player == null || quest == null)
            {
                Debug.LogError("MinimapInfoController에 플레이어, Zone, Road, 퀘스트를 연결하세요.", this);
                enabled = false;
                return;
            }

            // 영역 검색은 생성 시 한 번만 한다. 이동 중에는 캐시된 범위만 확인한다.
            zones = zonesRoot != null ? zonesRoot.GetComponentsInChildren<MapArea>(true) : System.Array.Empty<MapArea>();
            roads = roadsRoot != null ? roadsRoot.GetComponentsInChildren<MapArea>(true) : System.Array.Empty<MapArea>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += ReadLoadedMapAreas;
            SceneManager.sceneUnloaded += ReadRemainingMapAreas;
            ReadMapAreas();
            if (quest != null)
            {
                quest.Changed += UpdateQuestText;
            }
            documentRoot = null;
            locationShown = false;
            nextLocationUpdate = 0f;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= ReadLoadedMapAreas;
            SceneManager.sceneUnloaded -= ReadRemainingMapAreas;
            zones = roads = System.Array.Empty<MapArea>();
            currentArea = null;
            if (quest != null)
            {
                quest.Changed -= UpdateQuestText;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextLocationUpdate)
            {
                return;
            }
            nextLocationUpdate = Time.unscaledTime + 0.15f;
            if (player == null || !CacheLabels())
            {
                return;
            }

            Vector3 position = player.position;
            float nearestStatueDistance = float.PositiveInfinity;
            MapArea statue = FindNearbyStatue(zones, position, ref nearestStatueDistance);
            MapArea roadStatue = FindNearbyStatue(roads, position, ref nearestStatueDistance);
            if (roadStatue != null)
            {
                statue = roadStatue;
            }

            // 석상 반경 안에서는 석상 이름, 밖에서는 Zone 다음 Road 순으로 표시한다.
            MapArea area = statue != null ? statue : FindContainingArea(zones, position);
            if (area == null)
            {
                area = FindContainingArea(roads, position);
            }
            string areaName = statue != null ? statue.StatueDisplayName :
                area != null ? area.DisplayName : "구역 밖";
            if (area == null && minimapCamera != null &&
                minimapCamera.CurrentFloor != null)
                areaName = minimapCamera.CurrentFloor.DisplayName;
            if (!locationShown || currentArea != area || displayedAreaName != areaName)
            {
                currentArea = area;
                displayedAreaName = areaName;
                locationShown = true;
                locationText.text = areaName;
            }
        }

        private bool CacheLabels()
        {
            VisualElement root = document != null ? document.rootVisualElement : null;
            if (root == null)
            {
                return false;
            }
            if (ReferenceEquals(documentRoot, root) && locationText != null)
            {
                return true;
            }

            locationText = root.Q<Label>("current-location");
            bookText = root.Q<Label>("quest-book");
            exchangeText = root.Q<Label>("quest-exchange");
            exitText = root.Q<Label>("quest-exit");
            hintText = root.Q<Label>("quest-hint");
            if (locationText == null || bookText == null || exchangeText == null ||
                exitText == null || hintText == null)
            {
                return false;
            }
            documentRoot = root;
            locationShown = false;
            UpdateQuestText();
            return true;
        }

        private void ReadLoadedMapAreas(Scene scene, LoadSceneMode mode) => ReadMapAreas();
        private void ReadRemainingMapAreas(Scene scene) => ReadMapAreas();

        // 지도 표시가 자신에게 필요한 영역만 씬 변경 시 캐시한다. 컨테이너를 참조하지 않는다.
        private void ReadMapAreas()
        {
            var zoneAreas = new List<MapArea>();
            var roadAreas = new List<MapArea>();
            foreach (MapArea area in FindObjectsByType<MapArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool road = false;
                for (Transform parent = area.transform; parent != null; parent = parent.parent)
                    if (parent == roadsRoot || parent.name == "Road") { road = true; break; }
                if (road) roadAreas.Add(area); else zoneAreas.Add(area);
            }
            zones = zoneAreas.ToArray();
            roads = roadAreas.ToArray();
            currentArea = null;
            displayedAreaName = null;
            locationShown = false;
        }

        private void UpdateQuestText()
        {
            if (documentRoot == null || quest == null)
            {
                return;
            }
            int step = (int)quest.Step;
            SetStepText(bookText, "책 찾기", 0, step);
            SetStepText(exchangeText, "책을 스크롤로 교환하기", 1, step);
            SetStepText(exitText, "출구로 이동하기", 2, step);
            hintText.text = quest.Step == GroundQuestStep.Complete
                ? "지상 탐험을 완료했습니다."
                : "선택 · 석상을 찾아 능력치 강화";
        }

        private static void SetStepText(Label label, string title, int index, int current)
        {
            bool completed = index < current;
            bool active = index == current;
            label.text = (completed ? "완료 · " : active ? "진행 중 · " : "대기 · ") + title;
            label.EnableInClassList("quest-current", active);
            label.EnableInClassList("quest-complete", completed);
        }

        private static MapArea FindNearbyStatue(MapArea[] areas, Vector3 position,
            ref float nearestDistanceSquared)
        {
            MapArea nearestArea = null;
            for (int index = 0; index < areas.Length; index++)
            {
                MapArea area = areas[index];
                if (area == null || !area.isActiveAndEnabled ||
                    area.StatuePosition == null || !area.StatuePosition.gameObject.activeInHierarchy ||
                    string.IsNullOrWhiteSpace(area.StatueDisplayName) || area.StatueDisplayRadius <= 0f)
                {
                    continue;
                }

                // 지상 위치 표시이므로 높이를 제외한 수평 거리로 반경을 판단한다.
                Vector3 offset = area.StatuePosition.position - position;
                float distanceSquared = offset.x * offset.x + offset.z * offset.z;
                float radius = area.StatueDisplayRadius;
                if (distanceSquared <= radius * radius && distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestArea = area;
                }
            }
            return nearestArea;
        }

        private static MapArea FindContainingArea(MapArea[] areas, Vector3 position)
        {
            MapArea nearestArea = null;
            float smallestSize = float.PositiveInfinity;
            for (int index = 0; index < areas.Length; index++)
            {
                MapArea mapArea = areas[index];
                if (mapArea == null || !mapArea.isActiveAndEnabled)
                {
                    continue;
                }
                BoxCollider area = mapArea.AreaCollider;
                if (area == null || !area.enabled || !area.gameObject.activeInHierarchy)
                {
                    continue;
                }
                Vector3 local = area.transform.InverseTransformPoint(position) - area.center;
                Vector3 halfSize = area.size * 0.5f;
                if (Mathf.Abs(local.x) > halfSize.x || Mathf.Abs(local.z) > halfSize.z)
                {
                    continue;
                }
                Vector3 scale = area.transform.lossyScale;
                float size = Mathf.Abs(area.size.x * scale.x * area.size.z * scale.z);
                if (size < smallestSize)
                {
                    smallestSize = size;
                    nearestArea = mapArea;
                }
            }
            return nearestArea;
        }
    }
}
