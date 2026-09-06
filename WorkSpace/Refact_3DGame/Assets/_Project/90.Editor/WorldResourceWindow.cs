using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using World;

namespace EditorTools
{
    // 열린 씬과 닫힌 씬의 배치별 리소스를 표시하며 객체 참조는 보관하지 않는다.
    public sealed class WorldResourceWindow : EditorWindow
    {
        private const int PageSize = 80;
        private static readonly string[] ScenePaths =
        {
            "Assets/_Project/0_Scenes/Common.unity",
            "Assets/_Project/0_Scenes/Ground.unity",
            "Assets/_Project/0_Scenes/Underground.unity"
        };
        private static readonly string[] Tabs = { "전체", "지상 전용", "지하 전용", "공용" };
        private static readonly string[] ActiveFilters = { "전체", "활성", "비활성" };

        private sealed class ResourceUse
        {
            public string Path;
            public string SceneName;
            public string HierarchyPath;
            public int InstanceId;
            public int SceneMask;
            public int UsageMask;
            public string PlacementIssue;
            public string ObjectName;
            public bool SavedScene;
            public bool IsActive;
        }

        private sealed class ResourceRow
        {
            public string Path;
            public int SceneMask;
            public readonly List<ResourceUse> Uses = new List<ResourceUse>();
            public readonly List<ResourceUse> VisibleUses = new List<ResourceUse>();
            public string SceneNames;
            public string ActiveText;
            public GUIContent ObjectNames;
        }

        private List<ResourceUse> rows = new List<ResourceUse>();
        private readonly List<ResourceRow> resources = new List<ResourceRow>();
        private readonly List<ResourceRow> visibleRows = new List<ResourceRow>();
        private readonly int[] counts = new int[4];
        private string search = "";
        private string error;
        private string countText;
        private bool needsRefresh;
        private int matchesOutsideTab;
        private int selectedTab;
        private int page;
        private Vector2 scroll;
        private WorldObjectContainer container;
        private string placementCheckSummary;
        private bool placementCheckHasIssues;
        private int activeFilter = 1;
        private bool isReadingScenes;

        [MenuItem("Tools/World/리소스 소속")]
        public static void OpenWindow() => GetWindow<WorldResourceWindow>("리소스 소속");

        [MenuItem("GameObject/리소스 소속 찾기", false, 49)]
        public static void FindSelectedObjectResources()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || !selected.scene.IsValid()) return;
            var window = GetWindow<WorldResourceWindow>("리소스 소속");
            window.search = selected.name;
            window.selectedTab = 0;
            window.RefreshResources();
            window.Repaint();
        }

        [MenuItem("GameObject/리소스 소속 찾기", true)]
        private static bool CanFindSelectedObjectResources() =>
            Selection.activeGameObject != null && Selection.activeGameObject.scene.IsValid();

        private void OnEnable()
        {
            minSize = new Vector2(760f, 380f);
            EditorApplication.projectChanged += MarkChanged;
            EditorApplication.hierarchyChanged += MarkChanged;
            RefreshResources();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= MarkChanged;
            EditorApplication.hierarchyChanged -= MarkChanged;
        }

        private void OnFocus()
        {
            if (needsRefresh) RefreshResources();
        }
        private void MarkChanged()
        {
            if (isReadingScenes) return;
            needsRefresh = true;
            ClearPlacementCheck();
            Repaint();
        }

        private void ClearPlacementCheck()
        {
            placementCheckSummary = null;
            placementCheckHasIssues = false;
            foreach (ResourceUse row in rows) row.PlacementIssue = null;
        }

        private void RefreshResources()
        {
            if (isReadingScenes) return;
            isReadingScenes = true;
            ClearPlacementCheck();
            try
            {
                rows = ReadSceneResources();
                GroupResources();
                Array.Clear(counts, 0, counts.Length);
                foreach (ResourceRow row in resources)
                    for (int tab = 0; tab < Tabs.Length; tab++)
                        if (MatchesTab(row, tab)) counts[tab]++;
                countText = $"리소스 원본: 전체 {counts[0]}  |  지상 전용 {counts[1]}  |  지하 전용 {counts[2]}  |  공용 {counts[3]}";
                error = null;
                needsRefresh = false;
                FilterResources();
            }
            catch (Exception exception) { error = exception.Message; }
            finally { isReadingScenes = false; }
        }

        private static List<ResourceUse> ReadSceneResources(bool includeClosed = true)
        {
            var result = new List<ResourceUse>();
            var dependencies = new Dictionary<string, string[]>(StringComparer.Ordinal);
            var usage = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int sceneIndex = 0; sceneIndex < ScenePaths.Length; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneByPath(ScenePaths[sceneIndex]);
                bool savedScene = !scene.isLoaded;
                if (savedScene && !includeClosed) continue;
                try
                {
                    if (savedScene) scene = EditorSceneManager.OpenPreviewScene(ScenePaths[sceneIndex]);
                    foreach (GameObject root in scene.GetRootGameObjects())
                        ReadPlacement(root.transform, scene.name, scene.name, 1 << sceneIndex,
                            result, dependencies, usage, savedScene);
                }
                finally
                {
                    if (savedScene && scene.IsValid() && EditorSceneManager.IsPreviewScene(scene))
                        EditorSceneManager.ClosePreviewScene(scene);
                }
            }
            foreach (ResourceUse row in result)
                row.UsageMask = usage[row.Path == "파일 없음 (씬 객체)" ? row.Path + row.SceneMask + ":" + row.InstanceId : row.Path];
            return result;
        }

        // 비활성 자식도 조사하며 표시 목록에는 객체 참조를 보관하지 않는다.
        private static void ReadPlacement(Transform current, string parentPath, string sceneName, int sceneMask,
            List<ResourceUse> result, Dictionary<string, string[]> dependencies, Dictionary<string, int> usage, bool savedScene)
        {
            GameObject target = current.gameObject;
            string hierarchyPath = parentPath + "/" + target.name;
            var paths = new HashSet<string>(StringComparer.Ordinal);
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            if (!string.IsNullOrEmpty(prefabPath)) paths.Add(prefabPath);
            var visited = new HashSet<int>();
            foreach (Component component in target.GetComponents<Component>())
            {
                if (component == null || component is Transform) continue;
                ReadReferences(component, paths, dependencies, visited);
            }
            if (paths.Count == 0) paths.Add("파일 없음 (씬 객체)");
            var sortedPaths = new List<string>(paths);
            sortedPaths.Sort(StringComparer.Ordinal);
            foreach (string path in sortedPaths)
            {
                // 파일이 없는 빈 객체들은 같은 리소스를 공유하는 것으로 세지 않는다.
                string key = path == "파일 없음 (씬 객체)" ? path + sceneMask + ":" + target.GetInstanceID() : path;
                usage.TryGetValue(key, out int mask);
                usage[key] = mask | sceneMask;
                result.Add(new ResourceUse
                {
                    Path = path, SceneName = sceneName, HierarchyPath = hierarchyPath,
                    InstanceId = target.GetInstanceID(), SceneMask = sceneMask,
                    ObjectName = target.name, SavedScene = savedScene, IsActive = IsPlacementActive(target)
                });
            }
            for (int i = 0; i < current.childCount; i++)
                ReadPlacement(current.GetChild(i), hierarchyPath, sceneName, sceneMask, result, dependencies, usage, savedScene);
        }

        // 미리보기 씬에서도 부모를 포함한 저장 상태로 활성 여부를 판단한다.
        private static bool IsPlacementActive(GameObject target)
        {
            for (Transform current = target.transform; current != null; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true;
        }

        // 직렬화된 객체 참조를 조사하므로 material 같은 getter로 새 자원을 만들지 않는다.
        private static void ReadReferences(UnityEngine.Object source, HashSet<string> paths,
            Dictionary<string, string[]> dependencies, HashSet<int> visited)
        {
            if (source == null || !visited.Add(source.GetInstanceID())) return;
            using (var serialized = new SerializedObject(source))
            using (SerializedProperty property = serialized.GetIterator())
            {
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    UnityEngine.Object resource = property.objectReferenceValue;
                    if (resource == null || resource is MonoScript) continue;
                    string path = AssetDatabase.GetAssetPath(resource);
                    if (!string.IsNullOrEmpty(path) && IsResource(path))
                    {
                        paths.Add(path);
                        if (!dependencies.TryGetValue(path, out string[] related))
                        {
                            related = AssetDatabase.GetDependencies(path, true);
                            dependencies.Add(path, related);
                        }
                        foreach (string dependency in related)
                            if (IsResource(dependency)) paths.Add(dependency);
                    }
                    else if (!(resource is GameObject) && !(resource is Component) && string.IsNullOrEmpty(path))
                    {
                        paths.Add("파일 없음 · " + resource.GetType().Name + " · " + resource.name +
                            " [" + resource.GetInstanceID() + "]");
                        ReadReferences(resource, paths, dependencies, visited);
                    }
                }
            }
        }

        private static bool IsResource(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return false;
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".unity": case ".cs": case ".asmdef": case ".asmref": case ".dll":
                case ".md": case ".pdb": return false;
                default: return true;
            }
        }

        // 파일 경로를 기준으로 합친다. 빈 씬 객체는 개수 검사에만 남긴다.
        private void GroupResources()
        {
            resources.Clear();
            var byPath = new Dictionary<string, ResourceRow>(StringComparer.Ordinal);
            foreach (ResourceUse use in rows)
            {
                if (use.Path == "파일 없음 (씬 객체)") continue;
                if (!byPath.TryGetValue(use.Path, out ResourceRow resource))
                {
                    resource = new ResourceRow { Path = use.Path };
                    byPath.Add(use.Path, resource);
                    resources.Add(resource);
                }
                resource.SceneMask |= use.SceneMask;
                resource.Uses.Add(use);
            }
            resources.Sort((left, right) => StringComparer.Ordinal.Compare(left.Path, right.Path));
        }

        private static bool MatchesTab(ResourceRow row, int tab)
        {
            int regions = row.SceneMask & 6;
            switch (tab)
            {
                case 1: return regions == 2;
                case 2: return regions == 4;
                case 3: return regions == 6;
                default: return true;
            }
        }

        private void FilterResources()
        {
            ClearPlacementCheck();
            visibleRows.Clear();
            matchesOutsideTab = 0;
            foreach (ResourceRow row in resources)
            {
                row.VisibleUses.Clear();
                bool pathMatches = string.IsNullOrEmpty(search) ||
                    row.Path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                bool anyActive = false;
                bool anyInactive = false;
                var names = new List<string>();
                var locations = new List<string>();
                var sceneNames = new List<string>();
                // 씬 표시와 공용 판정은 필터로 숨긴 사용처까지 포함한다.
                foreach (ResourceUse use in row.Uses)
                {
                    string sceneName = use.SceneName + (use.SavedScene ? " (닫힘)" : "");
                    if (!sceneNames.Contains(sceneName)) sceneNames.Add(sceneName);
                    GameObject target = use.SavedScene ? null : EditorUtility.InstanceIDToObject(use.InstanceId) as GameObject;
                    if (!use.SavedScene && target == null) continue;
                    bool isActive = use.SavedScene ? use.IsActive : target.activeInHierarchy;
                    if (activeFilter == 1 && !isActive || activeFilter == 2 && isActive) continue;
                    string objectName = target != null ? target.name : use.ObjectName;
                    if (!pathMatches && use.HierarchyPath.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                        objectName.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    row.VisibleUses.Add(use);
                    anyActive |= isActive;
                    anyInactive |= !isActive;
                    names.Add(objectName);
                    locations.Add(use.HierarchyPath + (use.SavedScene ? " (저장 상태)" : ""));
                }
                if (row.VisibleUses.Count == 0) continue;
                row.SceneNames = string.Join(", ", sceneNames);
                row.ActiveText = anyActive && anyInactive ? "활성·비활성" : anyActive ? "활성" : "비활성";
                string label = names[0] + (names.Count > 1 ? $" 외 {names.Count - 1}개" : "");
                row.ObjectNames = new GUIContent(label, string.Join("\n", locations));
                if (MatchesTab(row, selectedTab)) visibleRows.Add(row);
                else if (!string.IsNullOrEmpty(search)) matchesOutsideTab++;
            }
            page = 0;
            scroll = Vector2.zero;
        }

        private static void ShowResourceUses(ResourceRow resource)
        {
            var menu = new GenericMenu();
            foreach (ResourceUse use in resource.VisibleUses)
            {
                ResourceUse selectedUse = use;
                var label = new GUIContent(use.HierarchyPath + " [" + use.InstanceId + "]" +
                    (use.SavedScene ? " (닫힘)" : ""));
                if (use.SavedScene) menu.AddDisabledItem(label);
                else menu.AddItem(label, false, () =>
                {
                    GameObject target = EditorUtility.InstanceIDToObject(selectedUse.InstanceId) as GameObject;
                    if (target == null) return;
                    Selection.activeGameObject = target;
                    EditorGUIUtility.PingObject(target);
                });
            }
            menu.ShowAsContext();
        }

        // 기존 목록을 지우지 않고 현재 씬을 다시 읽어, 사라진 배치와 바뀐 참조를 비교한다.
        private void CheckDisplayedPlacements()
        {
            ClearPlacementCheck();
            try
            {
                // 필터 결과가 아닌 전체 목록과 실제 Hierarchy를 비교한다.
                string objectCounts = CheckSceneObjectCounts(out bool countsDiffer);
                var currentPaths = new Dictionary<int, HashSet<string>>();
                foreach (ResourceUse current in ReadSceneResources(false))
                {
                    if (!currentPaths.TryGetValue(current.InstanceId, out HashSet<string> paths))
                    {
                        paths = new HashSet<string>(StringComparer.Ordinal);
                        currentPaths.Add(current.InstanceId, paths);
                    }
                    paths.Add(current.Path);
                }

                var fileExists = new Dictionary<string, bool>(StringComparer.Ordinal);
                int passed = 0;
                int failed = 0;
                int skipped = 0;
                foreach (ResourceRow resource in visibleRows)
                foreach (ResourceUse row in resource.Uses)
                {
                    int sceneIndex = row.SceneMask == 1 ? 0 : row.SceneMask == 2 ? 1 : 2;
                    string scenePath = ScenePaths[sceneIndex];
                    Scene scene = SceneManager.GetSceneByPath(scenePath);
                    if (!scene.isLoaded || row.SavedScene)
                    {
                        row.PlacementIssue = "검사 제외: 저장된 씬 목록입니다. 씬을 열고 목록을 다시 읽은 뒤 검사하세요.";
                        skipped++;
                        continue;
                    }

                    GameObject target = EditorUtility.InstanceIDToObject(row.InstanceId) as GameObject;
                    if (target == null)
                        row.PlacementIssue = "배치 객체가 제거됐습니다. 목록을 다시 읽어 주세요.";
                    else if (target.scene != scene)
                        row.PlacementIssue = "객체가 다른 씬으로 이동했습니다: " + target.scene.name;
                    else if (row.Path.StartsWith("Assets/", StringComparison.Ordinal) ||
                        row.Path.StartsWith("Packages/", StringComparison.Ordinal))
                    {
                        if (!fileExists.TryGetValue(row.Path, out bool exists))
                        {
                            exists = !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(row.Path)) && File.Exists(row.Path);
                            // 패키지 파일은 Packages 가상 경로로 표시될 수 있다.
                            if (!exists && row.Path.StartsWith("Packages/", StringComparison.Ordinal))
                                exists = AssetDatabase.LoadMainAssetAtPath(row.Path) != null;
                            fileExists.Add(row.Path, exists);
                        }
                        if (!exists) row.PlacementIssue = "원본 파일을 찾을 수 없습니다: " + row.Path;
                    }

                    if (row.PlacementIssue == null &&
                        (!currentPaths.TryGetValue(row.InstanceId, out HashSet<string> paths) || !paths.Contains(row.Path)))
                        row.PlacementIssue = "이 배치의 현재 프리팹·리소스 참조에서 해당 경로를 찾지 못했습니다.";

                    if (row.PlacementIssue == null) passed++;
                    else failed++;
                }
                placementCheckHasIssues = countsDiffer || failed > 0 || skipped > 0;
                placementCheckSummary = objectCounts + "\n" +
                    $"현재 탭·검색 결과 {visibleRows.Count}개 리소스의 전체 사용처 검사: 정상 {passed} · 문제 {failed} · 씬 닫힘 {skipped}.";
            }
            catch (Exception exception)
            {
                ClearPlacementCheck();
                placementCheckHasIssues = true;
                placementCheckSummary = "배치 검사를 완료하지 못했습니다: " + exception.Message;
            }
            Repaint();
        }

        private string CheckSceneObjectCounts(out bool countsDiffer)
        {
            countsDiffer = false;
            var results = new List<string>();
            for (int index = 0; index < ScenePaths.Length; index++)
            {
                string scenePath = ScenePaths[index];
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                var listedIds = new HashSet<int>();
                bool savedScene = false;
                foreach (ResourceUse row in rows)
                    if (row.SceneMask == (1 << index))
                    {
                        listedIds.Add(row.InstanceId);
                        savedScene |= row.SavedScene;
                    }

                Scene scene = SceneManager.GetSceneByPath(scenePath);
                if (!scene.isLoaded || savedScene)
                {
                    results.Add($"{sceneName}: 저장된 배치 {listedIds.Count}개 · 실제 객체 비교는 씬을 열고 다시 읽은 뒤 가능합니다.");
                    continue;
                }

                var sceneIds = new HashSet<int>();
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        sceneIds.Add(child.gameObject.GetInstanceID());

                int missing = 0;
                foreach (int id in sceneIds)
                    if (!listedIds.Contains(id)) missing++;
                int stale = 0;
                foreach (int id in listedIds)
                    if (!sceneIds.Contains(id)) stale++;

                bool differs = missing > 0 || stale > 0;
                countsDiffer |= differs;
                results.Add($"{sceneName}: 실제 {sceneIds.Count}개 / 목록 {listedIds.Count}개 · " +
                    (differs ? $"불일치 (목록 누락 {missing}, 더 이상 해당 씬에 없음 {stale})" : "일치"));
            }
            return "씬 전체 GameObject 비교 (비활성·자식 포함, 리소스 중복 제외)\n" + string.Join("\n", results);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("월드 리소스 소속", EditorStyles.boldLabel);
            if (Application.isPlaying && container == null) container = FindFirstObjectByType<WorldObjectContainer>();
            using (new EditorGUI.DisabledScope(!Application.isPlaying || container == null || !container.isActiveAndEnabled || container.IsBusy))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("지상 불러오기")) container.Load("Ground");
                if (GUILayout.Button("지상 해제")) container.Unload("Ground");
                if (GUILayout.Button("지하 불러오기")) container.Load("Underground");
                if (GUILayout.Button("지하 해제")) container.Unload("Underground");
            }
            if (Application.isPlaying && container != null)
            {
                using (new EditorGUI.DisabledScope(!container.isActiveAndEnabled || container.IsBusy))
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("현재 환경: " + SceneManager.GetActiveScene().name);
                    using (new EditorGUI.DisabledScope(!container.IsLoaded("Ground")))
                        if (GUILayout.Button("지상 환경 적용")) container.ApplyEnvironment("Ground");
                    using (new EditorGUI.DisabledScope(!container.IsLoaded("Underground")))
                        if (GUILayout.Button("지하 환경 적용")) container.ApplyEnvironment("Underground");
                }
                EditorGUILayout.LabelField($"로딩된 씬: {container.LoadedCount}개 · 지상 {(container.IsLoaded("Ground") ? "로딩됨" : "해제됨")} · 지하 {(container.IsLoaded("Underground") ? "로딩됨" : "해제됨")}" +
                    (container.IsBusy ? " · 처리 중" : ""));
                if (!string.IsNullOrEmpty(container.LastError)) EditorGUILayout.HelpBox(container.LastError, MessageType.Error);
            }
            EditorGUILayout.LabelField(countText ?? "목록을 불러오는 중");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("에셋·배치 다시 읽기", GUILayout.Width(190f))) RefreshResources();
                using (new EditorGUI.DisabledScope(container != null && container.IsBusy))
                    if (GUILayout.Button("현재 목록 배치 검사", GUILayout.Width(155f))) CheckDisplayedPlacements();
                if (GUILayout.Button("조명 소속 확인", GUILayout.Width(120f))) SceneLightWindow.OpenWindow();
                GUILayout.FlexibleSpace();
            }
            if (needsRefresh) EditorGUILayout.HelpBox("씬 또는 배치가 변경됐습니다. 다시 읽기를 눌러 갱신하세요.", MessageType.Info);
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (placementCheckSummary != null)
                EditorGUILayout.HelpBox(placementCheckSummary, placementCheckHasIssues ? MessageType.Warning : MessageType.Info);
            EditorGUI.BeginChangeCheck();
            selectedTab = Mathf.Clamp(selectedTab, 0, Tabs.Length - 1);
            selectedTab = GUILayout.Toolbar(selectedTab, Tabs);
            search = EditorGUILayout.TextField("배치 이름·파일 경로", search);
            activeFilter = EditorGUILayout.Popup("활성화 여부", activeFilter, ActiveFilters);
            if (EditorGUI.EndChangeCheck()) FilterResources();

            if (matchesOutsideTab > 0 && GUILayout.Button($"다른 분류에도 검색 결과 {matchesOutsideTab}개가 있습니다 · 전체에서 보기"))
            {
                selectedTab = 0;
                FilterResources();
            }
            int pages = Math.Max(1, (visibleRows.Count + PageSize - 1) / PageSize);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{visibleRows.Count}개 · {page + 1}/{pages} 페이지");
                using (new EditorGUI.DisabledScope(page == 0))
                    if (GUILayout.Button("이전", GUILayout.Width(60f))) { page--; scroll = Vector2.zero; }
                using (new EditorGUI.DisabledScope(page + 1 >= pages))
                    if (GUILayout.Button("다음", GUILayout.Width(60f))) { page++; scroll = Vector2.zero; }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("사용 씬", EditorStyles.boldLabel, GUILayout.Width(180f));
                GUILayout.Label("활성화 여부", EditorStyles.boldLabel, GUILayout.Width(90f));
                GUILayout.Label("사용 오브젝트", EditorStyles.boldLabel, GUILayout.Width(200f));
                GUILayout.Label("파일 경로", EditorStyles.boldLabel);
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            int end = Math.Min(visibleRows.Count, (page + 1) * PageSize);
            for (int i = page * PageSize; i < end; i++)
            {
                ResourceRow row = visibleRows[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent(row.SceneNames, row.SceneNames), GUILayout.Width(180f));
                    GUILayout.Label(row.ActiveText, GUILayout.Width(90f));
                    if (GUILayout.Button(row.ObjectNames, EditorStyles.linkLabel, GUILayout.Width(200f)))
                        ShowResourceUses(row);
                    if (GUILayout.Button(new GUIContent(row.Path, row.Path), EditorStyles.linkLabel))
                    {
                        UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(row.Path);
                        if (asset != null)
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                    }
                }
                foreach (ResourceUse use in row.Uses)
                    if (use.PlacementIssue != null)
                        EditorGUILayout.HelpBox(use.HierarchyPath + "\n" + use.PlacementIssue, MessageType.Warning);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("공용은 Ground·Underground 양쪽에서 쓰는 동일 리소스입니다. Common 단독 사용은 전체에만 표시합니다. 오브젝트 이름을 누르면 사용처 목록이 열립니다.", MessageType.None);
            EditorGUILayout.HelpBox("열린 씬은 현재 상태, 닫힌 씬은 저장된 상태입니다. 활성 필터는 비활성 부모 아래의 자식도 제외합니다. 씬 변경 후 다시 읽기로 목록을 갱신하세요.", MessageType.None);
            EditorGUILayout.HelpBox("닫힌 씬의 객체는 선택할 수 없습니다. 활성 표시는 메모리 상주 여부가 아니며 직렬화되지 않은 코드 참조와 씬 전역 설정은 목록에 포함되지 않습니다.", MessageType.None);
        }
    }
}
