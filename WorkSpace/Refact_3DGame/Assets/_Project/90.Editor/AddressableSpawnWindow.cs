using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Characters.Player.Lifecycle;
using GameUI.CombatHud;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using World.Loading;
using Object = UnityEngine.Object;

namespace EditorTools
{
    // 등록 목록을 읽고 이 창이 생성한 객체·성공한 로드 요청만 소유한다. 게임 소유 요청은 변경하지 않는다.
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

        /// <summary>등록된 Addressables를 직접 선택해 생성·제거하는 창을 연다.</summary>
        [MenuItem("Tools/Cheat/치트 창")]
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
            ui.Q("playerPage").Add(playerPanel.CreateView());
            ui.Q<Button>("addressableTab").clicked += () => ShowPlayerTab(false);
            ui.Q<Button>("playerTab").clicked += () => ShowPlayerTab(true);
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

        // 공통 왼쪽 탭에서 리소스와 플레이어 치트를 전환한다.
        private void ShowPlayerTab(bool showPlayer)
        {
            ui.Q("addressablePage").EnableInClassList("hidden", showPlayer);
            ui.Q("playerPage").EnableInClassList("hidden", !showPlayer);
            ui.Q("addressableTab").EnableInClassList("selected", !showPlayer);
            ui.Q("playerTab").EnableInClassList("selected", showPlayer);
            SetMessage(null);
            if (showPlayer) playerPanel.RefreshTargets();
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

        // 선택 항목과 이 창이 가진 수만 표시한다.
        private void ShowSelection()
        {
            if (this == null || ui == null) return;
            int count = 0;
            foreach (var item in created)
                if (selected != null && item.Item.Address == selected.Address && item.Item.IsScene == selected.IsScene) count++;
            ui.Q<Label>("selectedAddress").text = selected == null ? "등록 목록에서 항목을 선택하세요." :
                selected.Address + "\n이 창에서 생성·로드한 수: " + count;
            RefreshButtons();
        }

        // 선택한 주소로 이 창이 만든 마지막 객체·요청 하나를 삭제한다.
        private void DeleteSelected()
        {
            if (selected == null) return;
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i].Item.Address == selected.Address && created[i].Item.IsScene == selected.IsScene)
                { RemoveOne(created[i]); return; }
        }

        // 로드 현황에서도 같은 소유 요청을 찾아 기존 삭제 순서를 따른다.
        private void DeleteLoaded()
        {
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i].Item.Address == selectedLoadedAddress && created[i].Item.IsScene == selectedLoadedIsScene)
                { RemoveOne(created[i]); return; }
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
                    Scene start = SceneManager.GetSceneByName("Start");
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
            bool ownsSelection = false;
            foreach (var item in created)
                if (selected != null && item.Item.Address == selected.Address && item.Item.IsScene == selected.IsScene)
                { ownsSelection = true; break; }
            ui.Q<Button>("deleteSelected").SetEnabled(canChange && ownsSelection && selected != null &&
                ReferenceCount(selected.Address, selected.IsScene) > 0);
            RefreshLoadedDeleteButton(canChange);
            ui.Q<Label>("loadState").text = !playing ? "Play 중에 확인할 수 있습니다." :
                loaderBusy || busy ? "로드·정리 중" : snapshot.Count + "개 보관 중";
        }

        // 선택 이벤트에서 목록을 다시 갱신하지 않고 삭제 버튼만 반영한다.
        private void RefreshLoadedDeleteButton(bool canChange)
        {
            bool ownsSelection = false;
            foreach (var item in created)
                if (item.Item.Address == selectedLoadedAddress && item.Item.IsScene == selectedLoadedIsScene)
                { ownsSelection = true; break; }
            ui.Q<Button>("deleteLoaded").SetEnabled(canChange && ownsSelection &&
                ReferenceCount(selectedLoadedAddress, selectedLoadedIsScene) > 0);
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
