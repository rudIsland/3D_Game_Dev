using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UI;
using Zone;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace EditorTools
{
    // 지하 바닥 충돌체를 편집 시에만 읽어 층별 지도 메시를 저장한다.
    public static class UndergroundMinimapBuilder
    {
        private const string ScenePath = "Assets/_Project/00.Scene/UnderGround.unity";
        private const string AssetFolder = "Assets/_Project/UI/Minimap/Underground";
        private const float CellSize = 0.5f;
        private const float FloorBoundary = -10.5f;
        private const int MapLayer = 23;
        private static readonly Vector2Int[] Neighbors =
        {
            Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up
        };

        /// <summary>편집 모드에서 지하 씬의 바닥 충돌체를 읽어 층별 미니맵 메시를 생성하고 저장한다.</summary>
        [MenuItem("Tools/Minimap/지하 층별 지도 갱신")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("편집 모드에서 지도를 갱신하세요.");
            UnityScene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            Transform mapParent = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "UndergroundObjects") mapParent = root.transform;
            if (mapParent == null) throw new InvalidOperationException("지하 배치 부모 UndergroundObjects가 필요합니다.");

            var cells = new[] { new HashSet<Vector2Int>(), new HashSet<Vector2Int>() };
            var stairs = new[] { new HashSet<Vector2Int>(), new HashSet<Vector2Int>() };
            var heights = new[] { new Dictionary<Vector2Int, float>(), new Dictionary<Vector2Int, float>() };
            var walls = new HashSet<Collider>();
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy)
                    continue;
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(collider.gameObject)
                    .ToLowerInvariant();
                if (path.Contains("wall") || path.Contains("column") || path.Contains("arch") ||
                    path.Contains("door")) walls.Add(collider);
                bool isStair = path.Contains("/stairs/");
                if (!(isStair || path.Contains("/grounds/") || path.Contains("/woodenbridge/") ||
                    path.EndsWith("/sm_plane.prefab")) || path.Contains("rubble")) continue;
                SampleFloor(collider, isStair, cells, stairs, heights);
            }
            RemoveWallCells(cells, stairs, heights, walls);
            if (cells[0].Count == 0 || cells[1].Count == 0)
                throw new InvalidOperationException("두 층의 바닥을 확인한 뒤 지도를 갱신하세요.");

            if (!AssetDatabase.IsValidFolder(AssetFolder))
                AssetDatabase.CreateFolder("Assets/_Project/UI/Minimap", "Underground");
            Material fill = GetMaterial("Floor", new Color(0.27f, 0.42f, 0.49f));
            Material edge = GetMaterial("Boundary", new Color(0.64f, 0.79f, 0.83f));
            Material stair = GetMaterial("Stairs", new Color(0.85f, 0.66f, 0.30f));
            Transform mapRoot = GetChild(mapParent, "UndergroundMinimap");
            for (int i = 0; i < cells.Length; i++)
            {
                Transform floorRoot = GetChild(mapRoot, "Floor" + (i + 1));
                float mapHeight = 252.11f + i * 200f;
                Undo.RecordObject(floorRoot, "Position minimap floor");
                floorRoot.SetPositionAndRotation(new Vector3(0f, mapHeight, 0f), Quaternion.identity);
                floorRoot.localScale = Vector3.one;
                MinimapFloor floor = floorRoot.GetComponent<MinimapFloor>();
                if (floor == null) floor = Undo.AddComponent<MinimapFloor>(floorRoot.gameObject);
                var serialized = new SerializedObject(floor);
                serialized.FindProperty("displayName").stringValue = i == 0 ? "지하 1층" : "지하 2층";
                serialized.FindProperty("minimumPlayerHeight").floatValue = i == 0 ? FloorBoundary : -100f;
                serialized.FindProperty("maximumPlayerHeight").floatValue = i == 0 ? 0f : FloorBoundary;
                serialized.FindProperty("mapSurfaceHeight").floatValue = mapHeight;
                serialized.ApplyModifiedProperties();
                Draw(floorRoot, "Floor", BuildCells(cells[i]), fill, i, 0f);
                Draw(floorRoot, "Stairs", BuildCells(stairs[i]), stair, i, 0.02f);
                Draw(floorRoot, "Boundary", BuildBoundary(cells[i]), edge, i, 0.04f);
            }

            MinimapCameraController camera = null;
            foreach (MinimapCameraController candidate in UnityEngine.Object.FindObjectsByType<MinimapCameraController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.gameObject.scene.name == "Common") camera = candidate;
            if (camera != null)
            {
                foreach (MinimapInfoController info in UnityEngine.Object.FindObjectsByType<MinimapInfoController>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (info.gameObject.scene != camera.gameObject.scene) continue;
                    var serialized = new SerializedObject(info);
                    serialized.FindProperty("minimapCamera").objectReferenceValue = camera;
                    serialized.ApplyModifiedProperties();
                }
                EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = mapRoot.gameObject;
            Debug.Log($"지하 지도 갱신: 1층 {cells[0].Count}칸, 2층 {cells[1].Count}칸. 씬을 저장하세요.");
        }

        private static void SampleFloor(Collider collider, bool isStair,
            HashSet<Vector2Int>[] cells, HashSet<Vector2Int>[] stairs,
            Dictionary<Vector2Int, float>[] heights)
        {
            Bounds bounds = collider.bounds;
            int minX = Mathf.FloorToInt(bounds.min.x / CellSize);
            int maxX = Mathf.CeilToInt(bounds.max.x / CellSize);
            int minZ = Mathf.FloorToInt(bounds.min.z / CellSize);
            int maxZ = Mathf.CeilToInt(bounds.max.z / CellSize);
            for (int x = minX; x < maxX; x++)
            for (int z = minZ; z < maxZ; z++)
            {
                var origin = new Vector3((x + 0.5f) * CellSize, bounds.max.y + 1f,
                    (z + 0.5f) * CellSize);
                if (!collider.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit,
                    bounds.size.y + 2f) || hit.normal.y < 0.55f) continue;
                int floor = hit.point.y >= FloorBoundary ? 0 : 1;
                var cell = new Vector2Int(x, z);
                cells[floor].Add(cell);
                if (!heights[floor].TryGetValue(cell, out float existing) || hit.point.y > existing)
                    heights[floor][cell] = hit.point.y;
                if (isStair) stairs[floor].Add(cell);
            }
        }

        private static void RemoveWallCells(HashSet<Vector2Int>[] cells,
            HashSet<Vector2Int>[] stairs, Dictionary<Vector2Int, float>[] heights,
            HashSet<Collider> walls)
        {
            Physics.SyncTransforms();
            var overlaps = new Collider[128];
            var remove = new List<Vector2Int>();
            for (int floor = 0; floor < cells.Length; floor++)
            {
                remove.Clear();
                foreach (Vector2Int cell in cells[floor])
                {
                    var center = new Vector3((cell.x + 0.5f) * CellSize,
                        heights[floor][cell] + 0.9f, (cell.y + 0.5f) * CellSize);
                    int count = Physics.OverlapBoxNonAlloc(center, new Vector3(0.2f, 0.7f, 0.2f),
                        overlaps, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                    if (count == overlaps.Length)
                        throw new InvalidOperationException("지도 검사 범위에 충돌체가 너무 많습니다.");
                    for (int i = 0; i < count; i++)
                    {
                        if (!walls.Contains(overlaps[i])) continue;
                        remove.Add(cell);
                        break;
                    }
                }
                foreach (Vector2Int cell in remove)
                {
                    cells[floor].Remove(cell);
                    stairs[floor].Remove(cell);
                }
            }
        }

        private static Mesh BuildCells(HashSet<Vector2Int> cells)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            // 가로로 이어진 칸은 직사각형 하나로 합친다.
            foreach (Vector2Int cell in cells)
            {
                if (cells.Contains(cell + Vector2Int.left)) continue;
                int end = cell.x + 1;
                while (cells.Contains(new Vector2Int(end, cell.y))) end++;
                AddQuad(vertices, triangles, cell.x * CellSize, cell.y * CellSize,
                    end * CellSize, (cell.y + 1) * CellSize);
            }
            return CreateMesh(vertices, triangles);
        }

        private static Mesh BuildBoundary(HashSet<Vector2Int> cells)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const float width = 0.06f;
            foreach (Vector2Int cell in cells)
            {
                float x = cell.x * CellSize, z = cell.y * CellSize;
                for (int side = 0; side < Neighbors.Length; side++)
                {
                    if (cells.Contains(cell + Neighbors[side])) continue;
                    if (side == 0) AddQuad(vertices, triangles, x - width, z, x + width, z + CellSize);
                    if (side == 1) AddQuad(vertices, triangles, x + CellSize - width, z, x + CellSize + width, z + CellSize);
                    if (side == 2) AddQuad(vertices, triangles, x, z - width, x + CellSize, z + width);
                    if (side == 3) AddQuad(vertices, triangles, x, z + CellSize - width, x + CellSize, z + CellSize + width);
                }
            }
            return CreateMesh(vertices, triangles);
        }

        private static void AddQuad(List<Vector3> vertices, List<int> triangles,
            float x0, float z0, float x1, float z1)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(x0, 0f, z0)); vertices.Add(new Vector3(x0, 0f, z1));
            vertices.Add(new Vector3(x1, 0f, z1)); vertices.Add(new Vector3(x1, 0f, z0));
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }

        private static Mesh CreateMesh(List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static void Draw(Transform parent, string name, Mesh generated,
            Material material, int floor, float height)
        {
            string path = AssetFolder + "/Floor" + (floor + 1) + name + ".asset";
            generated.name = "Floor" + (floor + 1) + name;
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = generated; AssetDatabase.CreateAsset(mesh, path); }
            else { EditorUtility.CopySerialized(generated, mesh); UnityEngine.Object.DestroyImmediate(generated); EditorUtility.SetDirty(mesh); }
            Transform child = GetChild(parent, name);
            Undo.RecordObject(child, "Position minimap shape");
            child.localPosition = new Vector3(0f, height, 0f);
            child.localRotation = Quaternion.identity; child.localScale = Vector3.one;
            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter == null) filter = Undo.AddComponent<MeshFilter>(child.gameObject);
            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = Undo.AddComponent<MeshRenderer>(child.gameObject);
            Undo.RecordObject(filter, "Connect minimap mesh");
            Undo.RecordObject(renderer, "Connect minimap material");
            filter.sharedMesh = mesh; renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Transform GetChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            var gameObject = new GameObject(name) { layer = MapLayer };
            Undo.RegisterCreatedObjectUndo(gameObject, "Create underground minimap");
            SceneManager.MoveGameObjectToScene(gameObject, parent.gameObject.scene);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
        }

        private static Material GetMaterial(string name, Color color)
        {
            string path = AssetFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("미니맵 Unlit 셰이더가 없습니다.");
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
