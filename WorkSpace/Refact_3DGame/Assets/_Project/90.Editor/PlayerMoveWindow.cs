using Characters.Player.Lifecycle;
using Items;
using UnityEditor;
using UnityEngine;
using World.Interaction;

namespace EditorTools
{
    // 씬의 상호작용 물체를 골라 Play 중인 플레이어를 그 앞으로 옮긴다.
    public sealed class PlayerMoveWindow : EditorWindow
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
        private Vector2 scrollPosition;
        private string moveMessage;

        private sealed class MoveTarget
        {
            public MonoBehaviour Object;
            public readonly GUIContent Label;

            public MoveTarget(string name)
            {
                Label = new GUIContent(name + " 앞으로 이동");
            }
        }

        [MenuItem("Tools/Player/플레이어 이동")]
        private static void OpenWindow()
        {
            GetWindow<PlayerMoveWindow>("플레이어 이동");
        }

        private void OnEnable()
        {
            minSize = new Vector2(420f, 320f);
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            RefreshTargets();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
        }

        private void OnFocus()
        {
            RefreshTargets();
        }

        private void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                RefreshTargets();
            }
        }

        private void OnGUI()
        {
            bool isPlaying = EditorApplication.isPlaying;
            bool isChangingPlayMode =
                isPlaying != EditorApplication.isPlayingOrWillChangePlaymode;
            using (new EditorGUI.DisabledScope(
                EditorApplication.isCompiling || isChangingPlayMode))
            {
                if (GUILayout.Button(isPlaying ? "Stop" : "Play", GUILayout.Height(30f)))
                {
                    EditorApplication.isPlaying = !isPlaying;
                }
            }

            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (MoveTarget target in targets)
            {
                using (new EditorGUI.DisabledScope(
                    !isPlaying || isChangingPlayMode || player == null || player.IsDead ||
                    !player.isActiveAndEnabled || target.Object == null ||
                    !target.Object.gameObject.activeInHierarchy))
                {
                    if (GUILayout.Button(target.Label, GUILayout.Height(28f)))
                    {
                        MovePlayer(target.Object);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("목적지 새로고침"))
            {
                RefreshTargets();
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("플레이어", player, typeof(PlayerController), true);
            }

            if (!isPlaying)
            {
                EditorGUILayout.HelpBox("위의 Play 버튼을 누르면 이동 버튼을 사용할 수 있습니다.",
                    MessageType.Info);
            }
            else if (player == null || player.IsDead || !player.isActiveAndEnabled)
            {
                EditorGUILayout.HelpBox("이동할 수 있는 플레이어를 찾을 수 없습니다.", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(moveMessage))
            {
                EditorGUILayout.HelpBox(moveMessage, MessageType.Info);
            }
        }

        private void RefreshTargets()
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
            Repaint();
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
            Repaint();
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
