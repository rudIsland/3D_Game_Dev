using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Manager;
using Boot;
using Player;
using UI;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace EditorTools
{
    // 전체 로드 요청을 표시하고 삭제는 실제 소유자에게 전달한다. 창을 닫을 때는 자체 요청만 반환한다.
    public sealed class AddressableSpawnWindow : EditorWindow
    {
        private sealed class CatalogItem
        {
            public string Address;
            public string Name;
            public string Group;
            public Type Type;
            public bool IsScene => Type == typeof(SceneAsset);
            public bool IsPrefab => Type == typeof(GameObject);
        }

        private sealed class CreatedResource
        {
            public CatalogItem Item;
            public GameObject Root;
            public GameObject Instance;
            public bool RequestHeld = true;
            public string Description;
        }

        private readonly List<CatalogItem> catalog = new List<CatalogItem>();
        private readonly List<CreatedResource> created = new List<CreatedResource>();
        private readonly List<AddressableManager.LoadInfo> snapshot = new List<AddressableManager.LoadInfo>();
        private VisualElement ui;
        private ListView catalogList;
        private ListView loadedList;
        private CatalogItem selected;
        private string selectedLoadedAddress;
        private bool selectedLoadedIsScene;
        private AddressableManager manager;
        private bool busy;
        private bool closing;
        private CancellationToken playExit;
        private double nextRefresh;
        private PlayerMovePanel playerPanel;
        private PlayerHealthPanel healthPanel;
        private ItemCheatPanel itemPanel;
        private QuestCheatPanel questPanel;
        private enum CheatTab { Addressable, Player, Item, Quest }

        /// <summary>등록된 Addressables를 직접 선택해 생성·제거하는 창을 연다.</summary>
        [MenuItem("Tools/Cheat/치트 창", false, 0)]
        public static void OpenWindow() => GetWindow<AddressableSpawnWindow>("치트");

        // UI 상태 갱신과 Play 경계 정리를 등록한다.
        private void OnEnable()
        {
            minSize = new Vector2(800f, 480f);
            closing = false;
            if (Application.isPlaying) playExit = Application.exitCancellationToken;
            EditorApplication.update += RefreshWhenDue;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            EditorApplication.projectChanged += ReadCatalog;
        }

        // 로딩 도중 닫혀도 완료된 요청을 확인한 뒤 창이 소유한 항목만 정리한다.
        private void OnDisable()
        {
            closing = true;
            EditorApplication.update -= RefreshWhenDue;
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.projectChanged -= ReadCatalog;
            EditorApplication.update -= CloseWhenIdle;
            if (Application.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode && !playExit.IsCancellationRequested)
                EditorApplication.update += CloseWhenIdle;
        }

        // Play 종료는 Unity 정리를 따르고 다음 Play에서 이전 요청을 재사용하지 않는다.
        private void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                closing = true;
                EditorApplication.update -= CloseWhenIdle;
            }
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                created.Clear();
                manager = null;
                busy = false;
                closing = false;
                if (Application.isPlaying) playExit = Application.exitCancellationToken;
                ShowSelection();
                SetMessage(null);
                playerPanel?.RefreshTargets();
                healthPanel?.Refresh();
                itemPanel?.RefreshTargets();
                questPanel?.Refresh();
            }
            RefreshButtons();
        }

        /// <summary>등록 목록·생성/삭제 버튼·현재 로드 현황만 연결한다.</summary>
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/90.Editor/AddressableSpawnWindow.uxml").CloneTree(rootVisualElement);
            ui = rootVisualElement.Q("toolsRoot");
            ui.EnableInClassList("tools-root--dark", EditorGUIUtility.isProSkin);
            titleContent = new GUIContent("치트");
            playerPanel = new PlayerMovePanel();
            healthPanel = new PlayerHealthPanel();
            var playerScroll = new ScrollView();
            playerScroll.AddToClassList("cheat-scroll");
            playerScroll.Add(healthPanel.CreateView());
            playerScroll.Add(playerPanel.CreateView());
            ui.Q("playerPage").Add(playerScroll);
            itemPanel = new ItemCheatPanel();
            ui.Q("itemPage").Add(itemPanel.CreateView());
            questPanel = new QuestCheatPanel();
            ui.Q("questPage").Add(questPanel.CreateView());
            ui.Q<Button>("addressableTab").clicked += () => ShowCheatTab(CheatTab.Addressable);
            ui.Q<Button>("playerTab").clicked += () => ShowCheatTab(CheatTab.Player);
            ui.Q<Button>("itemTab").clicked += () => ShowCheatTab(CheatTab.Item);
            ui.Q<Button>("questTab").clicked += () => ShowCheatTab(CheatTab.Quest);
            catalogList = ui.Q<ListView>("catalogList");
            catalogList.itemsSource = catalog;
            catalogList.makeItem = MakeListRow;
            catalogList.bindItem = (row, index) =>
            {
                var item = catalog[index];
                row.userData = item;
                row.Q<Label>("itemName").text = item.Name;
                ShowReferenceCount(row, ReferenceCount(item.Address, item.IsScene));
                row.Q<Label>("itemDetail").text = item.Group + " / " + KindName(item);
                row.tooltip = item.Address;
            };
            catalogList.selectionChanged += items =>
            {
                selected = null;
                foreach (CatalogItem item in items) { selected = item; break; }
                ShowSelection();
            };
            loadedList = ui.Q<ListView>("loadedList");
            loadedList.itemsSource = snapshot;
            loadedList.selectionChanged += items =>
            {
                selectedLoadedAddress = null;
                foreach (var value in items)
                {
                    if (!(value is AddressableManager.LoadInfo item)) continue;
                    selectedLoadedAddress = item.Address;
                    selectedLoadedIsScene = item.IsScene;
                    break;
                }
                RefreshLoadedDeleteButton(CanChange());
            };
            loadedList.makeItem = MakeListRow;
            loadedList.bindItem = (row, index) =>
            {
                var item = snapshot[index];
                row.Q<Label>("itemName").text = System.IO.Path.GetFileNameWithoutExtension(item.Address);
                ShowReferenceCount(row, item.RequestCount);
                row.Q<Label>("itemDetail").text = (item.IsScene ? "씬" : "에셋") +
                    " / " + (!item.HandleIsValid ? "핸들 무효" :
                    item.RequestCount == 0 ? "정리 대기" : "사용 중");
                row.tooltip = item.Address;
            };
            ui.Q<Button>("createSelected").clicked += CreateSelected;
            ui.Q<Button>("deleteSelected").clicked += DeleteSelected;
            ui.Q<Button>("deleteLoaded").clicked += DeleteLoaded;
            ui.Q<Button>("spawnTab").clicked += () => ShowTab(false);
            ui.Q<Button>("loadedTab").clicked += () => ShowTab(true);
            ReadCatalog();
            RefreshButtons();
        }

        // 왼쪽 탭으로 같은 창의 내용만 바꾼다.
        private void ShowTab(bool showLoaded)
        {
            ui.Q("spawnPage").EnableInClassList("hidden", showLoaded);
            ui.Q("loadedPage").EnableInClassList("hidden", !showLoaded);
            ui.Q<Button>("spawnTab").EnableInClassList("selected", !showLoaded);
            ui.Q<Button>("loadedTab").EnableInClassList("selected", showLoaded);
        }

        // 공통 왼쪽 탭에서 리소스·플레이어·현재 맵 아이템 치트를 전환한다.
        private void ShowCheatTab(CheatTab tab)
        {
            ui.Q("addressablePage").EnableInClassList("hidden", tab != CheatTab.Addressable);
            ui.Q("playerPage").EnableInClassList("hidden", tab != CheatTab.Player);
            ui.Q("itemPage").EnableInClassList("hidden", tab != CheatTab.Item);
            ui.Q("questPage").EnableInClassList("hidden", tab != CheatTab.Quest);
            ui.Q("addressableTab").EnableInClassList("selected", tab == CheatTab.Addressable);
            ui.Q("playerTab").EnableInClassList("selected", tab == CheatTab.Player);
            ui.Q("itemTab").EnableInClassList("selected", tab == CheatTab.Item);
            ui.Q("questTab").EnableInClassList("selected", tab == CheatTab.Quest);
            SetMessage(null);
            if (tab == CheatTab.Player) { playerPanel.RefreshTargets(); healthPanel.Refresh(); }
            if (tab == CheatTab.Item) itemPanel.RefreshTargets();
            if (tab == CheatTab.Quest) questPanel.Refresh();
        }

        // 두 목록에서 이름과 종류·상태를 같은 간격으로 표시한다.
        private VisualElement MakeListRow()
        {
            var row = new VisualElement();
            row.AddToClassList("list-row");
            var name = new Label { name = "itemName" };
            name.AddToClassList("item-name");
            var heading = new VisualElement();
            heading.AddToClassList("item-heading");
            var count = new Label { name = "referenceCount" };
            count.AddToClassList("reference-count");
            heading.Add(name);
            heading.Add(count);
            var detail = new Label { name = "itemDetail" };
            detail.AddToClassList("item-detail");
            row.Add(heading); row.Add(detail);
            return row;
        }

        // 실제 등록 목록만 읽는다. 설정 변경 때 갱신하며 원본 에셋은 직접 로드하지 않는다.
        private void ReadCatalog()
        {
            if (this == null || ui == null) return;
            catalogList.ClearSelection();
            selected = null;
            catalog.Clear();
            var entries = new List<AddressableAssetEntry>();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null) settings.GetAllAssets(entries, false);
            foreach (var entry in entries)
            {
                if (entry.MainAssetType == null || AssetDatabase.IsValidFolder(entry.AssetPath)) continue;
                catalog.Add(new CatalogItem { Address = entry.address,
                    Name = System.IO.Path.GetFileNameWithoutExtension(entry.AssetPath),
                    Group = entry.parentGroup != null ? entry.parentGroup.Name : "그룹 없음", Type = entry.MainAssetType });
            }
            catalog.Sort((a, b) =>
            {
                int group = string.Compare(a.Group, b.Group, StringComparison.OrdinalIgnoreCase);
                return group != 0 ? group : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            catalogList.Rebuild();
            ShowSelection();
        }

        // 선택 항목의 전체 참조 수와 실제 삭제 범위를 함께 표시한다.
        private void ShowSelection()
        {
            if (this == null || ui == null) return;
            RefreshButtons();
        }

        // 두 탭의 삭제 입력을 같은 소유자 조회 경로로 전달한다.
        private void DeleteSelected()
        {
            if (selected != null) DeleteAddress(selected.Address, selected.IsScene);
        }

        private void DeleteLoaded() => DeleteAddress(selectedLoadedAddress, selectedLoadedIsScene);

        // 자체 요청을 먼저 반환하고, 없으면 게임 소유자가 객체·구독·원본을 순서대로 정리한다.
        private async void DeleteAddress(string address, bool isScene)
        {
            if (!CanChange() || string.IsNullOrEmpty(address)) return;
            manager = AddressableManager.Instance;
            int count = isScene ? manager.GetSceneRefCount(address) : manager.GetRefCount(address);
            if (count <= 0) { RefreshButtons(); return; }
            CreatedResource resource = FindCreatedResource(address, isScene);
            if (resource != null) { RemoveOne(resource); return; }
            Boots owner = FindGameOwner(address, isScene);
            if (owner == null)
            {
                SetMessage("이 주소의 사용 객체를 정리할 소유자를 찾지 못했습니다: " + address, true);
                return;
            }

            busy = true;
            var token = Application.exitCancellationToken;
            string description = owner.GetResourceDeleteDescription(address, isScene);
            SetMessage(description);
            RefreshButtons();
            try
            {
                await owner.DeleteResource(address, isScene);
                if (!token.IsCancellationRequested) SetMessage("삭제 완료 · " + description);
            }
            catch (Exception exception)
            {
                if (!token.IsCancellationRequested) SetMessage(exception.GetBaseException().Message, true);
            }
            finally
            {
                if (!token.IsCancellationRequested) { busy = false; RefreshButtons(); }
            }
        }

        // 같은 주소를 여러 번 만든 경우 가장 최근의 창 소유 요청 하나를 선택한다.
        private CreatedResource FindCreatedResource(string address, bool isScene)
        {
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i].Item.Address == address && created[i].Item.IsScene == isScene)
                    return created[i];
            return null;
        }

        // 에디터 조회 시점에만 진입점을 찾아 실제 게임의 소유 여부를 확인한다.
        private Boots FindGameOwner(string address, bool isScene)
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(address)) return null;
            foreach (Boots boot in Object.FindObjectsByType<Boots>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (boot.GetResourceDeleteDescription(address, isScene) != null) return boot;
            return null;
        }

        // 삭제에 따라 함께 사라지는 대상을 클릭 전에 표시한다.
        private string DeleteDescription(string address, bool isScene)
        {
            if (ReferenceCount(address, isScene) <= 0) return "반환할 참조가 없습니다.";
            if (FindCreatedResource(address, isScene) != null) return "치트가 만든 마지막 객체·요청 하나를 정리합니다.";
            Boots owner = FindGameOwner(address, isScene);
            return owner != null ? owner.GetResourceDeleteDescription(address, isScene) :
                "사용 객체를 정리할 소유자를 찾지 못했습니다.";
        }

        // 표시용 종류명을 반환한다.
        private static string KindName(CatalogItem item) => item.IsScene ? "씬" : item.IsPrefab ? "프리팹" : item.Type.Name;

        // 로더가 가진 현재 요청 수를 두 탭에서 같은 값으로 사용한다.
        private int ReferenceCount(string address, bool isScene)
        {
            foreach (var item in snapshot)
                if (item.Address == address && item.IsScene == isScene) return item.RequestCount;
            return 0;
        }

        // 참조 횟수는 이름과 분리해 행의 오른쪽 끝에 표시한다.
        private static void ShowReferenceCount(VisualElement row, int count)
        {
            Label label = row.Q<Label>("referenceCount");
            label.text = count > 0 ? "참조 " + count : string.Empty;
        }

        // 성공한 요청부터 기록해 복제 중 오류가 나도 원본 요청을 반환할 수 있게 한다.
        private async void CreateSelected()
        {
            if (!CanChange() || selected == null) return;
            busy = true;
            var token = Application.exitCancellationToken;
            playExit = token;
            manager = AddressableManager.Instance;
            var owner = manager;
            CatalogItem item = selected;
            Vector3 position = Vector3.zero;
            CreatedResource resource = null;
            SetMessage(item.Name + " 로드 중...");
            RefreshButtons();
            try
            {
                Object original = null;
                if (item.IsScene) await owner.LoadSceneAsync(item.Address);
                else
                {
                    // 등록된 실제 에셋 타입으로 기존 공개 제네릭 로더를 호출한다.
                    var method = typeof(AddressableManager).GetMethod(nameof(AddressableManager.LoadAssetAsync)).MakeGenericMethod(item.Type);
                    Task load = (Task)method.Invoke(owner, new object[] { item.Address });
                    await load;
                    original = (Object)load.GetType().GetProperty("Result").GetValue(load);
                }
                if (token.IsCancellationRequested || !Application.isPlaying) return;
                resource = new CreatedResource { Item = item, Description = item.IsScene ? "씬 요청 1개" : "원본 요청 1개" };
                created.Add(resource);
                if (item.IsPrefab)
                {
                    resource.Root = new GameObject("Cheat_" + item.Name);
                    resource.Root.SetActive(false);
                    UnityScene start = SceneManager.GetSceneByName("Start");
                    if (start.IsValid() && start.isLoaded) SceneManager.MoveGameObjectToScene(resource.Root, start);
                    resource.Instance = Object.Instantiate((GameObject)original, resource.Root.transform, false);
                    resource.Instance.transform.position = position;
                    bool needsGameSetup = resource.Instance.GetComponentInChildren<PlayerController>(true) != null ||
                        resource.Instance.GetComponentInChildren<CombatHudController>(true) != null;
                    resource.Root.SetActive(!needsGameSetup);
                    resource.Description = needsGameSetup ? "비활성 복제 · 게임 전용 초기화 필요" :
                        "활성 복제 · 원본 요청 1개";
                }
                SetMessage(item.Name + " 생성 완료 · " + resource.Description);
            }
            catch (Exception exception)
            {
                if (!token.IsCancellationRequested)
                {
                    if (resource != null)
                    {
                        try { await RemoveResource(resource, owner, token); }
                        catch (Exception cleanup) { Debug.LogException(cleanup); }
                    }
                    SetMessage(exception.GetBaseException().Message, true);
                }
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    busy = false;
                    ShowSelection();
                    RefreshButtons();
                }
            }
        }

        // 선택된 소유 항목만 제거한다. 같은 주소의 다른 소유 요청은 남긴다.
        private async void RemoveOne(CreatedResource resource)
        {
            if (!CanChange()) return;
            busy = true;
            var token = Application.exitCancellationToken;
            RefreshButtons();
            try { await RemoveResource(resource, manager, token); SetMessage("선택한 항목을 제거하고 요청을 반환했습니다."); }
            catch (Exception exception) { if (!token.IsCancellationRequested) SetMessage(exception.GetBaseException().Message, true); }
            finally { if (!token.IsCancellationRequested) { busy = false; ShowSelection(); RefreshButtons(); } }
        }

        // 창이 가진 항목을 생성의 역순으로 제거한다. 닫힌 창에서도 반환 완료까지 실행한다.
        private async void RemoveAll()
        {
            if (busy || !Application.isPlaying || playExit.IsCancellationRequested) return;
            busy = true;
            var token = Application.exitCancellationToken;
            RefreshButtons();
            try
            {
                for (int i = created.Count - 1; i >= 0; i--)
                {
                    await RemoveResource(created[i], manager, token);
                    if (token.IsCancellationRequested) return;
                }
                SetMessage("이 창의 생성 객체와 요청을 모두 반환했습니다.");
            }
            catch (Exception exception)
            {
                if (!token.IsCancellationRequested) { SetMessage(exception.GetBaseException().Message, true); Debug.LogException(exception); }
            }
            finally { if (!token.IsCancellationRequested) { busy = false; ShowSelection(); RefreshButtons(); } }
        }

        // 객체 파괴 완료 → 로더 대기 → 자신의 요청 반환 → 실제 정리 순서를 지킨다.
        private async Task RemoveResource(CreatedResource resource, AddressableManager owner, CancellationToken token)
        {
            if (resource.Instance != null) { resource.Instance.SetActive(false); Object.Destroy(resource.Instance); }
            if (resource.Root != null) { resource.Root.SetActive(false); Object.Destroy(resource.Root); }
            while (resource.Instance != null || resource.Root != null || owner.IsBusy)
            {
                if (token.IsCancellationRequested || !Application.isPlaying) return;
                await Task.Yield();
            }
            if (token.IsCancellationRequested || !Application.isPlaying) return;
            if (resource.RequestHeld)
            {
                bool returned = resource.Item.IsScene ? owner.ReleaseScene(resource.Item.Address) : owner.Release(resource.Item.Address);
                if (!returned) throw new InvalidOperationException("치트의 반환할 요청을 찾지 못했습니다: " + resource.Item.Address);
                resource.RequestHeld = false;
            }
            if (resource.Item.IsScene) await owner.CleanUpUnusedScenesAsync();
            else owner.CleanUpUnusedAssets();
            created.Remove(resource);
        }

        // 종료 중 새 비동기 해제를 시작하지 않고, 일반 창 닫기는 로딩 완료 후 정리한다.
        private void CloseWhenIdle()
        {
            if (!Application.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode || playExit.IsCancellationRequested)
            {
                EditorApplication.update -= CloseWhenIdle;
                return;
            }
            if (busy || manager != null && manager.IsBusy) return;
            EditorApplication.update -= CloseWhenIdle;
            RemoveAll();
        }

        // 로더의 공통 진행 상태를 확인하며 동시에 두 요청을 시작하지 않는다.
        private bool CanChange() => Application.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode &&
            !closing && !busy && (manager == null || !manager.IsBusy);

        // 외부 게임 요청의 로딩 상태도 버튼에 반영한다.
        private void RefreshWhenDue()
        {
            if (EditorApplication.timeSinceStartup < nextRefresh) return;
            nextRefresh = EditorApplication.timeSinceStartup + 0.5;
            RefreshButtons();
            playerPanel?.RefreshButtons();
            healthPanel?.Refresh();
            itemPanel?.RefreshButtons();
            questPanel?.Refresh();
        }

        // 버튼과 현재 보관 목록을 갱신한다. 조회만으로 로더를 새로 만들지 않는다.
        private void RefreshButtons()
        {
            if (this == null || ui == null) return;
            bool playing = Application.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode;
            bool loaderBusy = false;
            if (playing) AddressableManager.CopyLoadInfo(snapshot, out loaderBusy);
            else snapshot.Clear();
            loadedList?.RefreshItems();
            // 목록 선택을 다시 만들지 않고 화면에 보이는 행의 숫자만 갱신한다.
            catalogList?.Query<VisualElement>(className: "list-row").ForEach(row =>
            {
                if (row.userData is CatalogItem item)
                    ShowReferenceCount(row, ReferenceCount(item.Address, item.IsScene));
            });
            bool canChange = CanChange() && !loaderBusy;
            ui.Q<Button>("createSelected").SetEnabled(canChange && selected != null);
            ui.Q<Button>("deleteSelected").SetEnabled(canChange && selected != null &&
                ReferenceCount(selected.Address, selected.IsScene) > 0);
            ui.Q<Label>("selectedAddress").text = selected == null ? "등록 목록에서 항목을 선택하세요." :
                selected.Address + "\n전체 참조: " + ReferenceCount(selected.Address, selected.IsScene) +
                "\n" + DeleteDescription(selected.Address, selected.IsScene);
            RefreshLoadedDeleteButton(canChange);
            ui.Q<Label>("loadState").text = !playing ? "Play 중에 확인할 수 있습니다." :
                loaderBusy || busy ? "로드·정리 중" : snapshot.Count + "개 보관 중";
        }

        // 선택 이벤트에서 목록을 다시 갱신하지 않고 삭제 버튼만 반영한다.
        private void RefreshLoadedDeleteButton(bool canChange)
        {
            ui.Q<Button>("deleteLoaded").SetEnabled(canChange &&
                ReferenceCount(selectedLoadedAddress, selectedLoadedIsScene) > 0);
            ui.Q<Label>("loadedSelection").text = string.IsNullOrEmpty(selectedLoadedAddress)
                ? "항목을 선택하면 함께 정리할 사용 객체를 표시합니다."
                : DeleteDescription(selectedLoadedAddress, selectedLoadedIsScene);
        }

        // 창이 살아 있을 때만 결과를 표시한다.
        private void SetMessage(string message, bool error = false)
        {
            if (this == null || ui == null) return;
            Label label = ui.Q<Label>("resultMessage");
            label.text = message ?? string.Empty;
            label.EnableInClassList("hidden", string.IsNullOrEmpty(message));
            label.EnableInClassList("message--error", error);
        }
    }
}
