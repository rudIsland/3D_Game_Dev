using System;
using System.Collections.Generic;
using Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorTools
{
    // Play 여부와 관계없이 등록 데이터를 표시하고 실행 보관 상태만 주기적으로 읽는다.
    public sealed class GameDataWindow : EditorWindow
    {
        [SerializeField] private GameData gameData;
        [SerializeField] private int containerIndex;
        private readonly GameDataList data = new GameDataList();
        private readonly List<GameDataList.Entry> visible = new List<GameDataList.Entry>();
        private VisualElement ui;
        private ListView list;
        private TextField search;
        private Toggle usedOnly;
        private Label summary;
        private ScrollView inspector;
        private ScriptableObject selected;
        private double nextRefresh;

        /// <summary>등록된 게임 설정과 실행 사용 상태를 조회하는 Data 창을 연다.</summary>
        [MenuItem("Tools/Data/게임 데이터", false, 100)]
        public static void OpenWindow() => GetWindow<GameDataWindow>("Data");

        // 데이터 에셋 변경·Play 전환·정기 조회를 창 수명에 맞춰 연결한다.
        private void OnEnable()
        {
            minSize = new Vector2(1040f, 500f);
            EditorApplication.projectChanged += ReadData;
            EditorApplication.playModeStateChanged += OnPlayChanged;
            EditorApplication.update += RefreshWhenDue;
        }

        // 창을 닫아도 게임의 데이터 참조는 반환하지 않고 UI 구독만 해제한다.
        private void OnDisable()
        {
            EditorApplication.projectChanged -= ReadData;
            EditorApplication.playModeStateChanged -= OnPlayChanged;
            EditorApplication.update -= RefreshWhenDue;
            inspector?.Unbind();
        }

        /// <summary>컨테이너 선택·검색과 보관 데이터의 읽기 전용 상세 보기를 연결한다.</summary>
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/90.Editor/GameDataWindow.uxml").CloneTree(rootVisualElement);
            ui = rootVisualElement.Q("dataRoot");
            ui.EnableInClassList("tools-root--dark", EditorGUIUtility.isProSkin);
            search = ui.Q<TextField>("dataSearch");
            search.label = "검색";
            usedOnly = ui.Q<Toggle>("usedOnly");
            summary = ui.Q<Label>("dataSummary");
            inspector = ui.Q<ScrollView>("dataInspector");
            list = ui.Q<ListView>("dataList");
            list.itemsSource = visible;
            list.fixedItemHeight = 72;
            list.makeItem = MakeRow;
            list.bindItem = BindRow;
            list.selectionChanged += choices =>
            {
                foreach (GameDataList.Entry entry in choices) { ShowSelection(entry); return; }
                ShowSelection(null);
            };
            search.RegisterValueChangedCallback(_ => Filter());
            usedOnly.RegisterValueChangedCallback(_ => Filter());
            ui.Q<Button>("refreshData").clicked += ReadData;
            ui.Q<Button>("selectDataAsset").clicked += () =>
            {
                if (selected == null) return;
                Selection.activeObject = selected;
                EditorGUIUtility.PingObject(selected);
            };
            if (gameData == null) gameData = data.FindGameData();
            var source = ui.Q<ObjectField>("gameDataSource");
            source.objectType = typeof(GameData);
            source.allowSceneObjects = false;
            source.SetValueWithoutNotify(gameData);
            source.RegisterValueChangedCallback(change => { gameData = change.newValue as GameData; ReadData(); });
            var containers = ui.Q<ListView>("dataContainers");
            containers.itemsSource = GameDataList.ContainerTypes;
            containers.fixedItemHeight = 44;
            containers.makeItem = () =>
            {
                var label = new Label();
                label.AddToClassList("data-container-name");
                return label;
            };
            containers.bindItem = (row, index) => ((Label)row).text = GameDataList.ContainerTypes[index].Name;
            containers.selectionChanged += choices =>
            {
                foreach (Type type in choices)
                {
                    containerIndex = Array.IndexOf(GameDataList.ContainerTypes, type);
                    ReadData();
                    break;
                }
            };
            containerIndex = Mathf.Clamp(containerIndex, 0, GameDataList.ContainerTypes.Length - 1);
            containers.SetSelection(containerIndex);
            ReadData();
        }

        // 프로젝트 변경 때 에셋 목록을 다시 읽고 현재 필터를 유지한다.
        private void ReadData()
        {
            if (ui == null) return;
            data.Read(gameData, GameDataList.ContainerTypes[containerIndex]);
            Filter();
        }

        // Play 경계에서도 에셋 목록을 유지하며 상태와 상세 보기만 새로 맞춘다.
        private void OnPlayChanged(PlayModeStateChange state) => ReadData();

        // 실행 상태는 0.5초마다 조회하며 사용 집합이 바뀔 때만 목록을 다시 그린다.
        private void RefreshWhenDue()
        {
            if (ui == null || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < nextRefresh) return;
            nextRefresh = EditorApplication.timeSinceStartup + 0.5d;
            if (data.RefreshUsage()) ReadData();
        }

        // 선택한 컨테이너 안에서만 검색·사용 상태를 적용하고 남아 있는 선택을 유지한다.
        private void Filter()
        {
            ScriptableObject previous = selected;
            visible.Clear();
            int usedCount = 0;
            string query = search.value?.Trim() ?? string.Empty;
            foreach (var entry in data.Entries)
            {
                if (entry.Asset == null) continue;
                bool used = data.IsUsed(entry.Asset);
                if (used) usedCount++;
                if (usedOnly.value && !used) continue;
                string words = entry.Asset.name + " " + entry.Asset.GetType().Name + " " + entry.Path;
                if (query.Length > 0 && words.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                visible.Add(entry);
            }
            list.ClearSelection();
            list.Rebuild();
            int index = visible.FindIndex(entry => entry.Asset == previous);
            if (index >= 0) list.SetSelection(index);
            else ShowSelection(null);
            summary.text = GameDataList.ContainerTypes[containerIndex].Name +
                $" · {(EditorApplication.isPlaying ? "실행 중" : "미실행")} · 실행 보관 원본 {data.HeldCount}개 · 연결 포함 {data.Entries.Count}개 · 사용 중 {usedCount}개 · 표시 {visible.Count}개";
            if (data.Entries.Count == 0) summary.text += gameData == null ? " · 등록 목록을 선택하세요." : " · 연결된 데이터 없음";
        }

        // 고정 높이 행에 이름·종류·경로·상태를 나란히 배치한다.
        private static VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("data-row");
            var heading = new VisualElement();
            heading.AddToClassList("item-heading");
            var name = new Label { name = "name" };
            name.AddToClassList("item-name");
            heading.Add(name);
            var state = new Label { name = "state" };
            state.AddToClassList("data-state");
            heading.Add(state);
            row.Add(heading);
            var kind = new Label { name = "kind" };
            kind.AddToClassList("item-detail");
            row.Add(kind);
            var path = new Label { name = "path" };
            path.AddToClassList("item-detail");
            row.Add(path);
            return row;
        }

        // 재사용 행에 현재 에셋 정보와 실제 참조 상태를 덮어쓴다.
        private void BindRow(VisualElement row, int index)
        {
            var entry = visible[index];
            row.Q<Label>("name").text = entry.Asset.name;
            row.Q<Label>("kind").text = (entry.Direct ? "원본" : "연결 데이터") + " · " + entry.Asset.GetType().Name;
            row.Q<Label>("path").text = entry.Path;
            bool used = data.IsUsed(entry.Asset);
            var state = row.Q<Label>("state");
            state.text = !EditorApplication.isPlaying ? "미실행" : used ? (data.IsHeld(entry.Asset) ? "보관 중" : "연결 사용") : "미사용";
            state.EnableInClassList("data-state--used", used);
            row.tooltip = entry.Source + "\n" + entry.Path;
        }

        // 선택한 설정을 읽기 전용 Inspector로 보여 주고 연결 경로를 함께 표시한다.
        private void ShowSelection(GameDataList.Entry entry)
        {
            selected = entry?.Asset;
            inspector.Unbind();
            inspector.Clear();
            ui.Q<Button>("selectDataAsset").SetEnabled(selected != null);
            ui.Q<Label>("dataSelection").text = selected == null ? "목록에서 데이터를 선택하세요." :
                selected.name + "\n연결: " + entry.Source;
            if (selected == null) return;
            var detail = new InspectorElement(selected);
            detail.SetEnabled(false);
            inspector.Add(detail);
        }
    }
}
