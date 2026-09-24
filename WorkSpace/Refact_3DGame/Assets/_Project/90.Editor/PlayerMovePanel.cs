using Characters.Player.Lifecycle;
using Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using World.Interaction;

namespace EditorTools
{
    // 씬의 상호작용 물체를 골라 Play 중인 플레이어를 그 앞으로 옮긴다.
    public sealed class PlayerMovePanel
    {
        private const float FrontGap = 0.35f;
        private const float GroundSearchHeight = 2f;
        private const float GroundSearchDepth = 6f;
        private static readonly float[] FrontSideOffsets = { 0f, 0.6f, -0.6f, 1.2f, -1.2f };
        private readonly MoveTarget[] targets =
        {
            new MoveTarget("스태미나"),
            new MoveTarget("공격력"),
            new MoveTarget("체력"),
            new MoveTarget("책"),
            new MoveTarget("스크롤")
        };
        private PlayerController player;
        private VisualElement view;
        private Button playButton;
        private Label playerLabel;
        private Label resultLabel;
        private string moveMessage;

        private sealed class MoveTarget
        {
            public MonoBehaviour Object;
            public readonly GUIContent Label;
            public Button Button;

            public MoveTarget(string name)
            {
                Label = new GUIContent(name + " 앞으로 이동");
            }
        }

        // 공용 치트 창의 플레이어 탭에 기존 이동 기능을 연결한다.
        public VisualElement CreateView()
        {
            view = new VisualElement();
            view.AddToClassList("tab-page");
            var title = new Label("플레이어 이동");
            title.AddToClassList("section-title");
            view.Add(title);
            playButton = new Button(() => EditorApplication.isPlaying = !EditorApplication.isPlaying);
            playButton.AddToClassList("player-move-button");
            view.Add(playButton);
            foreach (MoveTarget target in targets)
            {
                target.Button = new Button(() => { MovePlayer(target.Object); RefreshButtons(); });
                target.Button.text = target.Label.text;
                target.Button.AddToClassList("player-move-button");
                view.Add(target.Button);
            }
            var refresh = new Button(RefreshTargets) { text = "목적지 새로고침" };
            refresh.AddToClassList("player-move-button");
            view.Add(refresh);
            playerLabel = new Label();
            playerLabel.AddToClassList("selection");
            view.Add(playerLabel);
            resultLabel = new Label();
            resultLabel.AddToClassList("selection");
            view.Add(resultLabel);
            RefreshTargets();
            return view;
        }

        // 이동 가능한 상태와 목적지를 버튼에 반영한다.
        public void RefreshButtons()
        {
            if (view == null) return;
            bool playing = EditorApplication.isPlaying;
            bool changing = playing != EditorApplication.isPlayingOrWillChangePlaymode;
            playButton.text = playing ? "Stop" : "Play";
            playButton.SetEnabled(!EditorApplication.isCompiling && !changing);
            bool canMove = playing && !changing && player != null && !player.IsDead && player.isActiveAndEnabled;
            foreach (MoveTarget target in targets)
            {
                target.Button.SetEnabled(canMove && target.Object != null && target.Object.gameObject.activeInHierarchy);
                target.Button.tooltip = target.Label.tooltip;
            }
            playerLabel.text = player == null ? "플레이어 없음" : "플레이어: " + player.name;
            resultLabel.text = !string.IsNullOrEmpty(moveMessage) ? moveMessage :
                !playing ? "Play 중에 이동할 수 있습니다." : !canMove ? "목적지를 새로고침해 플레이어를 확인하세요." : string.Empty;
        }

        public void RefreshTargets()
        {
            foreach (MoveTarget target in targets)
            {
                target.Object = null;
                target.Label.tooltip = "현재 씬에서 목적지를 찾을 수 없습니다.";
            }
            player = null;
            moveMessage = null;
            foreach (PlayerController candidate in
                Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (candidate.isActiveAndEnabled && !candidate.IsDead)
                {
                    player = candidate;
                    break;
                }
            }

            foreach (MonoBehaviour candidate in
                Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID))
            {
                if (!candidate.gameObject.activeInHierarchy ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded)
                {
                    continue;
                }

                int index = FindDestinationIndex(candidate);
                if (index < 0)
                {
                    continue;
                }

                MoveTarget target = targets[index];
                // 같은 조각상의 부모와 자식에 모두 붙어 있으면 부모 위치를 쓴다.
                if (target.Object != null &&
                    !target.Object.transform.IsChildOf(candidate.transform))
                {
                    continue;
                }

                string path = candidate.name;
                for (Transform parent = candidate.transform.parent;
                     parent != null; parent = parent.parent)
                {
                    path = parent.name + "/" + path;
                }

                target.Object = candidate;
                target.Label.tooltip = candidate.gameObject.scene.name + "/" + path;
            }
            RefreshButtons();
        }

        private static int FindDestinationIndex(MonoBehaviour candidate)
        {
            if (candidate is StatueUpgradeInteraction statue)
            {
                switch (statue.UpgradeType)
                {
                    case StatueUpgradeType.MaxStamina: return 0;
                    case StatueUpgradeType.Strength: return 1;
                    case StatueUpgradeType.MaxHealth: return 2;
                }
            }

            // 책을 주워도 사라지지 않는 생성 위치를 목적지로 사용한다.
            if (candidate is ItemSpawnPoint spawnPoint && spawnPoint.ItemType == ItemType.Book)
            {
                return 3;
            }

            if (candidate is ItemExchangeInteraction)
            {
                using (var exchange = new SerializedObject(candidate))
                {
                    SerializedProperty reward = exchange.FindProperty("rewardItemType");
                    if (reward != null && reward.intValue == (int)ItemType.Scroll)
                    {
                        return 4;
                    }
                }
            }

            return -1;
        }

        public bool MovePlayer(MonoBehaviour target)
        {
            if (!EditorApplication.isPlaying || player == null || player.IsDead ||
                !player.isActiveAndEnabled || target == null ||
                !target.gameObject.activeInHierarchy ||
                (!(target is IPlayerInteractable) && !(target is ItemSpawnPoint)))
            {
                moveMessage = "Play 중인 플레이어와 활성 목적지 오브젝트가 필요합니다.";
                return false;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller == null || !controller.enabled)
            {
                moveMessage = "플레이어의 CharacterController가 활성화되어 있어야 합니다.";
                return false;
            }

            Physics.SyncTransforms();
            Vector3 forward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up);
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (Collider collider in target.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                else
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
            }

            Vector3 scale = player.transform.lossyScale;
            float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float frontEdge = Mathf.Abs(forward.x) * bounds.extents.x +
                Mathf.Abs(forward.z) * bounds.extents.z;
            Vector3 frontPosition = bounds.center + forward * (frontEdge + radius + FrontGap);
            frontPosition.y = target.transform.position.y;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float height = Mathf.Max(controller.height * Mathf.Abs(scale.y), radius * 2f);
            Vector3 halfSegment = Vector3.up * (height * 0.5f - radius);
            Vector3 destination = default;
            Quaternion rotation = Quaternion.identity;
            bool foundSpace = false;
            // 정면이 막혔으면 같은 앞쪽에서 좌우로 한 걸음씩 확인한다.
            foreach (float sideOffset in FrontSideOffsets)
            {
                destination = frontPosition + right * sideOffset;
                if (!TryFindGround(destination, target.transform, controller, out Vector3 ground))
                {
                    continue;
                }

                Vector3 lookDirection = target.transform.position - destination;
                lookDirection.y = 0f;
                rotation = Quaternion.LookRotation(lookDirection.sqrMagnitude > 0.0001f
                    ? lookDirection : -forward);
                Vector3 centerOffset = rotation * Vector3.Scale(controller.center, scale);
                destination.y = ground.y - centerOffset.y + height * 0.5f + 0.05f;
                Vector3 center = destination + centerOffset;
                bool blocked = false;
                foreach (Collider obstacle in Physics.OverlapCapsule(
                    center + halfSegment, center - halfSegment,
                    Mathf.Max(0.01f, radius - controller.skinWidth),
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (!obstacle.transform.IsChildOf(player.transform))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    foundSpace = true;
                    break;
                }
            }

            if (!foundSpace)
            {
                moveMessage = "오브젝트 앞에서 플레이어가 설 수 있는 빈 바닥을 찾지 못했습니다.";
                return false;
            }

            if (!player.TryMoveToPosition(destination, rotation))
            {
                moveMessage = "플레이어 준비가 끝난 뒤 다시 시도하세요.";
                return false;
            }

            moveMessage = target.name + " 앞으로 이동했습니다.";
            RefreshButtons();
            return true;
        }

        private bool TryFindGround(Vector3 destination, Transform target,
            CharacterController controller, out Vector3 ground)
        {
            ground = default;
            float nearestDistance = float.PositiveInfinity;
            // 버튼을 누를 때만 검색하므로 결과 배열을 매 프레임 만들지 않는다.
            RaycastHit[] hits = Physics.RaycastAll(
                destination + Vector3.up * GroundSearchHeight, Vector3.down,
                GroundSearchHeight + GroundSearchDepth,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(player.transform) ||
                    hit.transform.IsChildOf(target) ||
                    Vector3.Angle(hit.normal, Vector3.up) > controller.slopeLimit ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                ground = hit.point;
            }
            return !float.IsPositiveInfinity(nearestDistance);
        }
    }
}
