using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Core.ConstantValid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    // 편집 중인 씬의 배치 범위로 소속 후보를 설명한다. 씬 이동과 자동 확정은 하지 않는다.
    public sealed class SceneLightWindow : EditorWindow
    {
        private const int PageSize = 25;
        private static readonly string[] Filters = { "전체", "지상 후보", "지하 후보", "확인 필요" };

        private sealed class Geometry
        {
            public int Id;
            public Bounds Bounds;
        }

        private sealed class Entry
        {
            public int ComponentId;
            public string Path;
            public string Kind;
            public string State;
            public string Prefab;
            public int Candidate;
            public string Reason;
            public Vector3 Position;
            public Bounds Area;
            public bool HasArea;
            public int GroundHits;
            public int UndergroundHits;
            public Geometry GroundNear;
            public Geometry UndergroundNear;
        }

        private Transform source;
        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<Entry> visible = new List<Entry>();
        private Entry selected;
        private Vector2 scroll;
        private string search = "";
        private int filter;
        private int page;
        private bool stale;
        private string summary = "분석을 누르면 현재 열린 지상·지하와 비교합니다.";
        private string error;
        private int previewLightId;
        private bool showArea = true;

        [MenuItem("Tools/World/조명 소속 확인")]
        public static void OpenWindow() => GetWindow<SceneLightWindow>("조명 소속 확인");

        private void OnEnable()
        {
            minSize = new Vector2(960f, 650f);
            SceneView.duringSceneGui += DrawSceneArea;
            Selection.selectionChanged += RestorePreview;
            EditorApplication.hierarchyChanged += MarkStale;
            Undo.undoRedoPerformed += MarkStale;
            AssemblyReloadEvents.beforeAssemblyReload += RestorePreview;
            EditorApplication.quitting += RestorePreview;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            FindSource();
        }

        private void OnDisable()
        {
            RestorePreview();
            SceneView.duringSceneGui -= DrawSceneArea;
            Selection.selectionChanged -= RestorePreview;
            EditorApplication.hierarchyChanged -= MarkStale;
            Undo.undoRedoPerformed -= MarkStale;
            AssemblyReloadEvents.beforeAssemblyReload -= RestorePreview;
            EditorApplication.quitting -= RestorePreview;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            entries.Clear();
            visible.Clear();
            selected = null;
            SceneView.RepaintAll();
        }

        private void OnPlayModeChanged(PlayModeStateChange state) { RestorePreview(); MarkStale(); }
        private void OnSceneSaving(Scene scene, string path) => RestorePreview();
        private void MarkStale() { stale = entries.Count > 0; Repaint(); }

        // 열린 맵 씬에서 조명 루트를 찾아 분석 대상으로 보관한다.
        private void FindSource()
        {
            if (source != null) return;
            string[] scenes = { ConstantValid.GroundMapAddress, ConstantValid.UnderGroundMapAddress, "Common" };
            foreach (string name in scenes)
            {
                Scene scene = SceneManager.GetSceneByName(name);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (root.name == "Setup&Lights") { source = root.transform; return; }
            }
        }

        /// <summary>열린 맵의 배치와 조명 대상을 비교해 소속 후보를 갱신한다. 분석할 루트와 맵 씬이 열려 있어야 한다.</summary>
        public void Analyze()
        {
            RestorePreview();
            FindSource();
            entries.Clear();
            visible.Clear();
            selected = null;
            error = null;
            if (source == null || !source.gameObject.scene.IsValid())
            {
                error = "Hierarchy의 Setup&Lights를 대상으로 지정하세요.";
                return;
            }
            try
            {
                List<Geometry> ground = ReadGeometry(ConstantValid.GroundMapAddress, "GroundObjects");
                List<Geometry> underground = ReadGeometry(ConstantValid.UnderGroundMapAddress, "UndergroundObjects");
                foreach (Light light in source.GetComponentsInChildren<Light>(true)) AddEntry(light, ground, underground);
                foreach (Volume volume in source.GetComponentsInChildren<Volume>(true)) AddEntry(volume, ground, underground);
                foreach (ReflectionProbe probe in source.GetComponentsInChildren<ReflectionProbe>(true)) AddEntry(probe, ground, underground);
                // 하늘·발광 판 등 Light 컴포넌트가 없는 메시도 누락하지 않는다.
                foreach (MeshRenderer mesh in source.GetComponentsInChildren<MeshRenderer>(true)) AddEntry(mesh, ground, underground);
                entries.Sort((left, right) => string.Compare(left.Path, right.Path, StringComparison.Ordinal));
                int groundCount = 0, undergroundCount = 0, reviewCount = 0;
                foreach (Entry entry in entries)
                    if (entry.Candidate == 1) groundCount++;
                    else if (entry.Candidate == 2) undergroundCount++;
                    else reviewCount++;
                summary = $"대상 {entries.Count}개 · 지상 후보 {groundCount} · 지하 후보 {undergroundCount} · 확인 필요 {reviewCount} | 비교 메시: 지상 {ground.Count}, 지하 {underground.Count}";
                if (ground.Count == 0 || underground.Count == 0)
                    error = "Ground와 Underground 씬을 모두 열고 GroundObjects·UndergroundObjects 아래 메시를 확인하세요. 비교 자료가 없으면 후보를 확정하지 않습니다.";
                stale = false;
                FilterEntries();
            }
            catch (Exception exception) { error = exception.Message; }
            Repaint();
            SceneView.RepaintAll();
        }

        private static List<Geometry> ReadGeometry(string sceneName, string rootName)
        {
            var result = new List<Geometry>();
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded) return result;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != rootName) continue;
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!renderer.TryGetComponent<MeshFilter>(out var mesh) || mesh.sharedMesh == null) continue;
                    bool map = false;
                    for (Transform parent = renderer.transform; parent != null; parent = parent.parent)
                        if (parent.name.IndexOf("Minimap", StringComparison.OrdinalIgnoreCase) >= 0) { map = true; break; }
                    if (!map) result.Add(new Geometry { Id = renderer.gameObject.GetInstanceID(), Bounds = renderer.bounds });
                }
                foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(true))
                    if (terrain.terrainData != null)
                    {
                        Vector3 size = terrain.terrainData.size;
                        result.Add(new Geometry { Id = terrain.gameObject.GetInstanceID(), Bounds = new Bounds(terrain.transform.position + size * 0.5f, size) });
                    }
            }
            return result;
        }

        private void AddEntry(Component component, List<Geometry> ground, List<Geometry> underground)
        {
            var entry = new Entry
            {
                ComponentId = component.GetInstanceID(), Path = ObjectPath(component.transform),
                Position = component.transform.position, Candidate = 3,
                Prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component.gameObject)
            };
            Light light = component as Light;
            if (light != null)
            {
                entry.Kind = light.type + " / " + light.lightmapBakeType;
                entry.State = light.enabled && light.gameObject.activeInHierarchy ? "켜짐" : "꺼짐";
                entry.HasArea = light.type == LightType.Point || light.type == LightType.Spot;
                entry.Area = new Bounds(entry.Position, Vector3.one * light.range * 2f);
                if (!entry.HasArea) entry.Reason = "태양·면광원은 위치와 거리만으로 소속을 판단하지 않습니다. 지역의 의도한 조명을 확인하세요.";
            }
            else if (component is Volume volume)
            {
                entry.Kind = volume.isGlobal ? "Global Volume" : "Local Volume";
                entry.State = volume.enabled && volume.gameObject.activeInHierarchy ? "켜짐" : "꺼짐";
                if (!volume.isGlobal)
                    foreach (Collider collider in volume.GetComponents<Collider>())
                    {
                        if (!collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                        if (!entry.HasArea) entry.Area = collider.bounds; else entry.Area.Encapsulate(collider.bounds);
                        entry.HasArea = true;
                    }
                if (entry.HasArea) { entry.Area.Expand(volume.blendDistance * 2f); entry.Position = entry.Area.center; }
                else entry.Reason = volume.isGlobal ? "전역 Volume은 위치로 소속을 판단할 수 없습니다. 적용할 지역 분위기를 확인하세요." : "활성 Collider 범위가 없습니다. Volume 설정을 확인하세요.";
            }
            else if (component is ReflectionProbe probe)
            {
                entry.Kind = "Reflection Probe / " + probe.mode;
                entry.State = probe.enabled && probe.gameObject.activeInHierarchy ? "켜짐" : "꺼짐";
                entry.HasArea = true;
                entry.Area = probe.bounds;
                entry.Position = entry.Area.center;
            }
            else
            {
                entry.Kind = "하늘·발광 등 환경 메시";
                entry.State = component.gameObject.activeInHierarchy ? "활성" : "비활성";
                entry.Reason = "표면의 발광·하늘 효과는 메시 위치만으로 판단하지 않습니다. 머티리얼과 실제 장면을 확인하세요.";
            }
            entry.GroundNear = CompareGeometry(entry, light, ground, out entry.GroundHits);
            entry.UndergroundNear = CompareGeometry(entry, light, underground, out entry.UndergroundHits);
            if (entry.HasArea)
            {
                if (ground.Count == 0 || underground.Count == 0) entry.Reason = "두 씬의 비교 메시가 모두 필요합니다.";
                else if (entry.GroundHits > 0 && entry.UndergroundHits == 0) { entry.Candidate = 1; entry.Reason = "근사 영향 범위가 지상 메시와만 겹칩니다. 실제 조명 효과를 확인한 뒤 소속을 결정하세요."; }
                else if (entry.UndergroundHits > 0 && entry.GroundHits == 0) { entry.Candidate = 2; entry.Reason = "근사 영향 범위가 지하 메시와만 겹칩니다. 실제 조명 효과를 확인한 뒤 소속을 결정하세요."; }
                else entry.Reason = entry.GroundHits > 0 ? "양쪽 메시와 겹칩니다. 공용이라는 뜻은 아니며 원래 비추려던 지역을 확인해야 합니다." : "어느 쪽 메시와도 겹치지 않습니다. 범위·위치·배치 목적을 확인하세요.";
            }
            entries.Add(entry);
        }

        private static Geometry CompareGeometry(Entry entry, Light light, List<Geometry> geometry, out int hits)
        {
            hits = 0;
            Geometry nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (Geometry item in geometry)
            {
                float distance = item.Bounds.SqrDistance(entry.Position);
                if (distance < nearestDistance) { nearestDistance = distance; nearest = item; }
                if (!entry.HasArea) continue;
                bool overlaps = light != null ? distance <= light.range * light.range : item.Bounds.Intersects(entry.Area);
                if (overlaps && light != null && light.type == LightType.Spot)
                {
                    // AABB를 감싼 구와 원뿔의 보수적 비교다. 차폐·삼각형 교차 판정은 아니다.
                    Vector3 delta = item.Bounds.center - entry.Position;
                    float radius = item.Bounds.extents.magnitude;
                    float depth = Vector3.Dot(delta, light.transform.forward);
                    float angle = light.spotAngle * 0.5f * Mathf.Deg2Rad;
                    float radial = (delta - light.transform.forward * depth).magnitude;
                    overlaps = depth >= -radius && depth <= light.range + radius &&
                        radial <= Mathf.Max(0f, depth) * Mathf.Tan(angle) + radius / Mathf.Max(0.001f, Mathf.Cos(angle));
                }
                if (overlaps) hits++;
            }
            return nearest;
        }

        private static string ObjectPath(Transform target)
        {
            string path = target.name;
            for (Transform parent = target.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
            return target.gameObject.scene.name + "/" + path;
        }

        private void FilterEntries()
        {
            visible.Clear();
            foreach (Entry entry in entries)
                if ((filter == 0 || filter == entry.Candidate) && (string.IsNullOrEmpty(search) || entry.Path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)) visible.Add(entry);
            page = 0;
            scroll = Vector2.zero;
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                source = (Transform)EditorGUILayout.ObjectField("대상", source, typeof(Transform), true);
                if (EditorGUI.EndChangeCheck()) { RestorePreview(); entries.Clear(); visible.Clear(); selected = null; stale = false; summary = "대상이 바뀌었습니다. 다시 분석하세요."; }
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("분석", GUILayout.Width(65f))) Analyze();
                using (new EditorGUI.DisabledScope(entries.Count == 0))
                    if (GUILayout.Button("분류표 저장", GUILayout.Width(95f)))
                    {
                        string path = EditorUtility.SaveFilePanel("조명 소속 후보", "", "SceneLights", "csv");
                        if (!string.IsNullOrEmpty(path))
                            try { WriteReport(path); } catch (Exception exception) { error = exception.Message; }
                    }
            }
            EditorGUILayout.HelpBox("메시의 바깥 상자와 조명 범위를 비교한 후보입니다. 벽 차폐·조명 레이어·실제 밝기는 계산하지 않으며 비활성 메시와 LOD도 포함합니다. 자동 이동하지 않습니다.", MessageType.None);
            EditorGUILayout.LabelField(summary, EditorStyles.wordWrappedLabel);
            if (stale) EditorGUILayout.HelpBox("Hierarchy 또는 Undo 내용이 바뀌었습니다. 다시 분석하세요. Inspector에서 범위를 바꾼 뒤에도 다시 분석해야 합니다.", MessageType.Info);
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                filter = EditorGUILayout.Popup("판단", filter, Filters);
                search = EditorGUILayout.TextField("이름 검색", search);
                if (EditorGUI.EndChangeCheck()) FilterEntries();
            }
            int pages = Math.Max(1, (visible.Count + PageSize - 1) / PageSize);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{visible.Count}개 · {page + 1}/{pages} 페이지");
                using (new EditorGUI.DisabledScope(page == 0)) if (GUILayout.Button("이전", GUILayout.Width(60f))) { page--; scroll = Vector2.zero; }
                using (new EditorGUI.DisabledScope(page + 1 >= pages)) if (GUILayout.Button("다음", GUILayout.Width(60f))) { page++; scroll = Vector2.zero; }
            }
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160f));
            int end = Math.Min(visible.Count, (page + 1) * PageSize);
            for (int i = page * PageSize; i < end; i++)
            {
                Entry entry = visible[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button(entry.Path, entry == selected ? EditorStyles.boldLabel : EditorStyles.linkLabel)) SelectEntry(entry);
                    EditorGUILayout.LabelField($"{Filters[entry.Candidate]} | {entry.Kind} | {entry.State} | 교차 메시: 지상 {entry.GroundHits} / 지하 {entry.UndergroundHits}");
                    EditorGUILayout.LabelField(entry.Reason, EditorStyles.wordWrappedLabel);
                }
            }
            EditorGUILayout.EndScrollView();
            DrawSelected();
        }

        private void SelectEntry(Entry entry)
        {
            RestorePreview();
            selected = entry;
            Component component = EditorUtility.EntityIdToObject(entry.ComponentId) as Component;
            if (component == null) return;
            Selection.activeGameObject = component.gameObject;
            EditorGUIUtility.PingObject(component.gameObject);
            FocusBounds(new Bounds(component.transform.position, Vector3.one * 5f));
        }

        private static void FocusBounds(Bounds bounds)
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) view = GetWindow<SceneView>();
            view.Frame(bounds, false);
            view.Repaint();
        }

        private void DrawSelected()
        {
            if (selected == null) { EditorGUILayout.LabelField("목록의 이름을 누르면 해당 위치와 주변 메시를 확인할 수 있습니다."); return; }
            Component component = EditorUtility.EntityIdToObject(selected.ComponentId) as Component;
            if (component == null) { EditorGUILayout.HelpBox("선택한 객체가 제거됐습니다. 다시 분석하세요.", MessageType.Info); return; }
            EditorGUILayout.LabelField(selected.Path, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selected.Reason, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("원본 프리팹", string.IsNullOrEmpty(selected.Prefab) ? "연결 없음" : selected.Prefab);
            EditorGUI.BeginChangeCheck();
            showArea = EditorGUILayout.Toggle("영향 범위 표시", showArea);
            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("대상 위치 보기")) FocusBounds(new Bounds(component.transform.position, Vector3.one * 5f));
                if (GUILayout.Button("범위 전체 보기")) FocusBounds(selected.HasArea ? selected.Area : new Bounds(component.transform.position, Vector3.one * 10f));
                DrawNearbyButton("지상 주변 메시", selected.GroundNear);
                DrawNearbyButton("지하 주변 메시", selected.UndergroundNear);
            }
            Light light = component as Light;
            using (new EditorGUI.DisabledScope(light == null || light.lightmapBakeType != LightmapBakeType.Realtime || !light.gameObject.activeInHierarchy || (!light.enabled && previewLightId == 0) || EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button(previewLightId == 0 ? "선택한 실시간 조명만 잠시 끄기" : "조명 원래대로 복원"))
                {
                    if (previewLightId != 0) RestorePreview();
                    else { previewLightId = light.GetInstanceID(); light.enabled = false; SceneView.RepaintAll(); }
                }
            EditorGUILayout.LabelField("임시 비교는 실시간 조명만 지원합니다. 선택 변경·창 닫기·씬 저장·Play 진입 시 복원합니다. Mixed/Baked는 베이크 결과를 별도로 확인하세요.", EditorStyles.wordWrappedMiniLabel);
        }

        private static void DrawNearbyButton(string label, Geometry nearby)
        {
            using (new EditorGUI.DisabledScope(nearby == null))
                if (GUILayout.Button(label))
                {
                    UnityEngine.Object target = EditorUtility.EntityIdToObject(nearby.Id);
                    if (target != null) { Selection.activeObject = target; EditorGUIUtility.PingObject(target); FocusBounds(nearby.Bounds); }
                }
        }

        private void RestorePreview()
        {
            if (previewLightId == 0) return;
            Light light = EditorUtility.EntityIdToObject(previewLightId) as Light;
            if (light != null) light.enabled = true;
            previewLightId = 0;
            SceneView.RepaintAll();
            Repaint();
        }

        private void DrawSceneArea(SceneView view)
        {
            if (!showArea || selected == null || Event.current.type != EventType.Repaint) return;
            Component component = EditorUtility.EntityIdToObject(selected.ComponentId) as Component;
            if (component == null) return;
            Color previous = Handles.color;
            Handles.color = Color.yellow;
            if (component is Light light && light.type == LightType.Point)
            {
                Handles.DrawWireDisc(light.transform.position, Vector3.up, light.range);
                Handles.DrawWireDisc(light.transform.position, Vector3.right, light.range);
                Handles.DrawWireDisc(light.transform.position, Vector3.forward, light.range);
            }
            else if (component is Light spot && spot.type == LightType.Spot)
            {
                Transform t = spot.transform;
                Vector3 end = t.position + t.forward * spot.range;
                float radius = spot.range * Mathf.Tan(spot.spotAngle * 0.5f * Mathf.Deg2Rad);
                Handles.DrawWireDisc(end, t.forward, radius);
                Handles.DrawLine(t.position, end + t.right * radius); Handles.DrawLine(t.position, end - t.right * radius);
                Handles.DrawLine(t.position, end + t.up * radius); Handles.DrawLine(t.position, end - t.up * radius);
            }
            else if (selected.HasArea) Handles.DrawWireCube(selected.Area.center, selected.Area.size);
            if (selected.GroundNear != null) { Handles.color = Color.green; Handles.DrawWireCube(selected.GroundNear.Bounds.center, selected.GroundNear.Bounds.size); }
            if (selected.UndergroundNear != null) { Handles.color = Color.cyan; Handles.DrawWireCube(selected.UndergroundNear.Bounds.center, selected.UndergroundNear.Bounds.size); }
            Handles.color = previous;
        }

        public void WriteReport(string path)
        {
            var csv = new StringBuilder("배치 경로,종류,상태,소속 후보,근거,지상 교차 메시,지하 교차 메시,X,Y,Z,원본 프리팹\r\n");
            foreach (Entry entry in entries)
            {
                string[] columns = { entry.Path, entry.Kind, entry.State, Filters[entry.Candidate], entry.Reason,
                    entry.GroundHits.ToString(), entry.UndergroundHits.ToString(),
                    entry.Position.x.ToString("F3", CultureInfo.InvariantCulture), entry.Position.y.ToString("F3", CultureInfo.InvariantCulture), entry.Position.z.ToString("F3", CultureInfo.InvariantCulture), entry.Prefab ?? "" };
                for (int i = 0; i < columns.Length; i++)
                { if (i > 0) csv.Append(','); csv.Append('"').Append(columns[i].Replace("\"", "\"\"")).Append('"'); }
                csv.Append("\r\n");
            }
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
        }
    }
}
