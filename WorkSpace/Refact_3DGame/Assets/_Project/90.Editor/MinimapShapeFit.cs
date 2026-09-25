using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zone;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace EditorTools
{
    // MapArea에 연결된 실제 영역과 미니맵 도형의 위치 및 크기를 맞춘다.
    public static class MinimapShapeFit
    {
        private const string MenuPath = "Tools/Minimap/Zone·Road 크기 맞추기";

        [MenuItem(MenuPath)]
        public static void FitToZonesAndRoads()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Play를 종료한 뒤 미니맵 크기를 맞춰 주세요.");
                return;
            }

            UnityScene scene = SceneManager.GetActiveScene();
            var areas = new List<MapArea>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                areas.AddRange(root.GetComponentsInChildren<MapArea>(true));
            }
            if (areas.Count == 0)
            {
                Debug.LogWarning("현재 씬의 Zone·Road에 MapArea를 추가하고 영역과 미니맵 도형을 연결해 주세요.");
                return;
            }

            // 하나의 도형이나 영역을 두 번 연결했으면 둘 다 건너뛰어 덮어쓰기를 막는다.
            var sources = new HashSet<BoxCollider>();
            var shapes = new HashSet<MeshFilter>();
            var repeatedSources = new HashSet<BoxCollider>();
            var repeatedShapes = new HashSet<MeshFilter>();
            foreach (MapArea area in areas)
            {
                if (area.AreaCollider != null && !sources.Add(area.AreaCollider))
                    repeatedSources.Add(area.AreaCollider);
                if (area.MinimapShape != null && !shapes.Add(area.MinimapShape))
                    repeatedShapes.Add(area.MinimapShape);
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("미니맵 Zone·Road 크기 맞추기");
            int matchedCount = 0;
            int changedCount = 0;
            int skippedCount = 0;
            foreach (MapArea area in areas)
            {
                BoxCollider source = area.AreaCollider;
                MeshFilter shape = area.MinimapShape;
                if (source == null || shape == null ||
                    source.gameObject.scene != scene || shape.gameObject.scene != scene ||
                    repeatedSources.Contains(source) || repeatedShapes.Contains(shape))
                {
                    skippedCount++;
                    Debug.LogWarning($"{area.DisplayName}: MapArea의 영역과 미니맵 도형을 현재 씬에서 중복 없이 연결해 주세요.", area);
                    continue;
                }
                if (!FitShape(source, shape, out bool changed))
                {
                    skippedCount++;
                    continue;
                }
                matchedCount++;
                if (changed) changedCount++;
            }
            Undo.CollapseUndoOperations(undoGroup);

            if (changedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                SceneView.RepaintAll();
            }

            Debug.Log(
                $"미니맵 크기 맞추기 완료: 대응 {matchedCount}개, 변경 {changedCount}개, 건너뜀 {skippedCount}개. " +
                "변경 내용은 씬 저장 시 저장됩니다.");
        }

        [MenuItem(MenuPath, true)]
        private static bool CanFit()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
        }

        private static bool FitShape(BoxCollider source, MeshFilter shape, out bool changed)
        {
            changed = false;
            Mesh mesh = shape.sharedMesh;
            if (mesh == null || mesh.bounds.size.x <= 0f || mesh.bounds.size.y <= 0f ||
                mesh.bounds.size.z > 0.0001f)
            {
                Debug.LogWarning($"{shape.name}: XY 평면의 미니맵 도형이 필요합니다.", shape);
                return false;
            }

            Transform target = shape.transform;
            Transform parent = target.parent;
            Vector3 right = source.transform.TransformVector(Vector3.right * source.size.x);
            Vector3 forward = source.transform.TransformVector(Vector3.forward * source.size.z);
            right.y = 0f;
            forward.y = 0f;
            if (parent != null)
            {
                right = parent.InverseTransformVector(right);
                forward = parent.InverseTransformVector(forward);
            }
            if (right.sqrMagnitude < 0.000001f || forward.sqrMagnitude < 0.000001f ||
                Mathf.Abs(Vector3.Dot(right.normalized, forward.normalized)) > 0.0001f)
            {
                Debug.LogWarning($"{source.name}: 기울기 또는 부모 배율 때문에 사각형으로 정확히 맞출 수 없습니다.", source);
                return false;
            }

            Quaternion rotation = Quaternion.LookRotation(Vector3.Cross(right, forward), forward);
            Vector3 scale = new Vector3(
                right.magnitude / mesh.bounds.size.x,
                forward.magnitude / mesh.bounds.size.y,
                target.localScale.z);
            Vector3 center = source.transform.TransformPoint(source.center);
            center.y = target.TransformPoint(mesh.bounds.center).y;
            Vector3 position = (parent != null ? parent.InverseTransformPoint(center) : center) -
                rotation * Vector3.Scale(mesh.bounds.center, scale);

            if ((target.localPosition - position).sqrMagnitude < 0.00000001f &&
                (target.localScale - scale).sqrMagnitude < 0.00000001f &&
                Quaternion.Angle(target.localRotation, rotation) < 0.001f)
            {
                return true;
            }

            Undo.RecordObject(target, "미니맵 도형 맞추기");
            target.SetLocalPositionAndRotation(position, rotation);
            target.localScale = scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            changed = true;
            return true;
        }
    }
}
