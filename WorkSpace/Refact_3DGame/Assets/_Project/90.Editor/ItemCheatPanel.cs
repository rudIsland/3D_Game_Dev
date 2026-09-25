using System;
using System.Collections.Generic;
using Boot;
using Core;
using Data;
using Interaction;
using Item;
using Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace EditorTools
{
    // 현재 게임 맵의 아이템·사용처를 읽고 기존 획득·교환·가방 API만 실행한다.
    public sealed class ItemCheatPanel
    {
        private enum ItemAction { Give, Pick, Consume, Use }

        private sealed class ItemRow
        {
            public ItemDefinition Item;
            public readonly List<MonoBehaviour> Pickups = new List<MonoBehaviour>();
            public readonly List<ItemExchangeInteraction> Uses = new List<ItemExchangeInteraction>();
            public Label Count;
            public Button Give;
            public Button Pick;
            public Button Consume;
            public Button Use;
            public DropdownField UseTarget;
        }

        private readonly List<ItemRow> items = new List<ItemRow>();
        private readonly Dictionary<ItemDefinition, ItemRow> itemRows = new Dictionary<ItemDefinition, ItemRow>();
        private Boots boot;
        private UnityScene map;
        private VisualElement view;
        private ScrollView list;
        private Label mapLabel;
        private Label result;

        /// <summary>현재 맵 아이템 탭을 만들고 실제 사용처와 실행 결과를 표시한다.</summary>
        public VisualElement CreateView()
        {
            view = new VisualElement { name = "itemCheatPanel" };
            view.AddToClassList("tab-page");
            var title = new Label("현재 맵 아이템");
            title.AddToClassList("section-title");
            view.Add(title);
            mapLabel = new Label { name = "itemMapStatus" };
            mapLabel.AddToClassList("selection");
            view.Add(mapLabel);
            view.Add(new Button(RefreshTargets) { text = "아이템·사용처 새로고침", name = "refreshMapItems" });
            var help = new Label("지급은 가방만, 맵 획득·사용처 실행은 실제 맵 오브젝트를 처리합니다. 1개 소모는 효과 없이 수량만 줄입니다.");
            help.AddToClassList("selection");
            view.Add(help);
            list = new ScrollView { name = "mapItemList" };
            list.AddToClassList("cheat-scroll");
            view.Add(list);
            result = new Label { name = "itemCheatResult" };
            result.AddToClassList("selection");
            view.Add(result);
            RefreshTargets();
            return view;
        }

        // 활성 씬이나 씬 이름 대신 실제 게임 소유자가 준비한 맵만 사용한다.
        private bool TryGetMap(out UnityScene scene)
        {
            scene = default;
            if (!EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return false;
            if (boot == null) boot = UnityEngine.Object.FindFirstObjectByType<Boots>();
            return boot != null && boot.TryGetCheatMap(out scene);
        }

        /// <summary>현재 맵의 배치 지점·획득 객체·교환 비용/보상으로 아이템 목록을 다시 만든다.</summary>
        public void RefreshTargets()
        {
            if (view == null) return;
            items.Clear();
            itemRows.Clear();
            list.Clear();
            result.text = string.Empty;
            if (!TryGetMap(out map))
            {
                mapLabel.text = "준비된 게임 맵이 없습니다. Play와 맵 로딩이 끝난 뒤 확인하세요.";
                return;
            }

            ItemCatalog catalog = ItemDataContainer.Instance.Catalog;
            foreach (GameObject root in map.GetRootGameObjects())
            {
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component is ItemSpawnPoint point && catalog != null && catalog.TryGetItem(point.ItemType, out ItemCatalogEntry entry))
                        AddItem(entry.ItemDefinition);
                    else if (component is WorldItemPickup pickup)
                        AddPickup(pickup.CheatItem, pickup);
                    else if (component is InventoryItem placed)
                        AddPickup(placed.CheatItem, placed);
                    else if (component is ItemExchangeInteraction exchange)
                    {
                        ItemRow cost = AddItem(exchange.CheatCostItem);
                        if (cost != null) cost.Uses.Add(exchange);
                        AddItem(exchange.CheatRewardItem);
                    }
                }
            }
            items.Sort((a, b) => string.Compare(a.Item.DisplayName, b.Item.DisplayName, StringComparison.Ordinal));
            foreach (ItemRow row in items) CreateRow(row);
            mapLabel.text = $"맵: {map.name} · 아이템 {items.Count}종" + (items.Count == 0 ? " · 연결된 배치·사용처 없음" : "");
            RefreshButtons();
        }

        // 같은 정의의 배치와 사용처는 한 행으로 모아 가방 수량과 함께 표시한다.
        private ItemRow AddItem(ItemDefinition item)
        {
            if (item == null) return null;
            if (itemRows.TryGetValue(item, out ItemRow row)) return row;
            row = new ItemRow { Item = item };
            itemRows.Add(item, row);
            items.Add(row);
            return row;
        }

        private void AddPickup(ItemDefinition item, MonoBehaviour pickup)
        {
            ItemRow row = AddItem(item);
            if (row != null) row.Pickups.Add(pickup);
        }

        // 실행 목적을 버튼별로 구분해 가방과 월드의 효과를 혼동하지 않게 한다.
        private void CreateRow(ItemRow row)
        {
            var box = new VisualElement { name = "item-" + row.Item.name };
            box.AddToClassList("cheat-section");
            var title = new Label(row.Item.DisplayName + " (" + row.Item.name + ")");
            title.AddToClassList("section-title");
            box.Add(title);
            row.Count = new Label();
            row.Count.AddToClassList("selection");
            box.Add(row.Count);
            if (row.Uses.Count > 0)
            {
                var choices = new List<string>();
                foreach (ItemExchangeInteraction use in row.Uses) choices.Add(ObjectPath(use.transform));
                row.UseTarget = new DropdownField("사용처", choices, 0);
                row.UseTarget.RegisterValueChangedCallback(_ => RefreshButtons());
                box.Add(row.UseTarget);
            }
            var actions = new VisualElement();
            actions.AddToClassList("actions");
            row.Give = ActionButton(actions, "1개 지급", "giveItem", () => Execute(row, ItemAction.Give));
            row.Give.tooltip = "맵 오브젝트를 유지하고 실제 가방 추가 API로 한 개 지급합니다.";
            row.Pick = ActionButton(actions, "맵에서 획득", "pickItem", () => Execute(row, ItemAction.Pick));
            row.Consume = ActionButton(actions, "1개 소모", "consumeItem", () => Execute(row, ItemAction.Consume));
            row.Consume.tooltip = "사용 효과·교환 없이 가방 수량만 한 개 줄이고 변경 이벤트를 발생시킵니다.";
            row.Use = ActionButton(actions, "사용처 실행", "useItem", () => Execute(row, ItemAction.Use));
            row.Use.tooltip = "선택한 맵 사용처의 TryInteract를 호출합니다. 거리 검사 없이 기존 비용·보상·퀘스트 처리를 실행합니다.";
            box.Add(actions);
            list.Add(box);
        }

        private static Button ActionButton(VisualElement parent, string title, string name, Action action)
        {
            var button = new Button(action) { text = title, name = name };
            button.AddToClassList("action");
            parent.Add(button);
            return button;
        }

        private static string ObjectPath(Transform target)
        {
            string path = target.name;
            for (Transform parent = target.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
            return path;
        }

        private bool IsMapObject(MonoBehaviour target) => target != null && target.isActiveAndEnabled && target.gameObject.scene == map;

        private MonoBehaviour FindPickup(ItemRow row, PlayerController player)
        {
            foreach (MonoBehaviour pickup in row.Pickups)
                if (IsMapObject(pickup) && pickup is IPlayerInteractable interaction && interaction.CanInteract(player)) return pickup;
            return null;
        }

        private ItemExchangeInteraction SelectedUse(ItemRow row)
        {
            int index = row.UseTarget != null ? row.UseTarget.index : -1;
            return index >= 0 && index < row.Uses.Count ? row.Uses[index] : null;
        }

        // 버튼 표시 후 맵이나 대상이 바뀌어도 실행 직전에 다시 확인한다.
        private void Execute(ItemRow row, ItemAction action)
        {
            var player = PlayerController.Instance;
            if (!TryGetMap(out UnityScene current) || current != map || player == null || !player.CanRunCheat)
            {
                RefreshTargets();
                result.text = "플레이어 또는 맵이 바뀌었습니다. 준비 완료 후 다시 실행하세요.";
                return;
            }
            int before = player.CheatItemCount(row.Item);
            bool success = false;
            string operation;
            switch (action)
            {
                case ItemAction.Give:
                    operation = "지급";
                    success = player.CheatGiveItem(row.Item);
                    break;
                case ItemAction.Pick:
                    operation = "맵 획득";
                    MonoBehaviour pickup = FindPickup(row, player);
                    if (pickup != null) success = ((IPlayerInteractable)pickup).TryInteract(player);
                    break;
                case ItemAction.Consume:
                    operation = "수량 소모(효과 없음)";
                    success = player.CheatConsumeItem(row.Item);
                    break;
                default:
                    operation = "사용처 실행";
                    ItemExchangeInteraction use = SelectedUse(row);
                    if (IsMapObject(use)) success = use.TryInteract(player);
                    break;
            }
            result.text = $"{row.Item.DisplayName} · {operation}: {(success ? "완료" : "실패: 수량·빈 슬롯·활성 사용처를 확인하세요.")} · 수량 {before} → {player.CheatItemCount(row.Item)}";
            RefreshButtons();
        }

        /// <summary>맵 전환 때 목록을 교체하고, 같은 맵에서는 현재 수량·실행 가능 여부만 읽는다.</summary>
        public void RefreshButtons()
        {
            if (view == null) return;
            bool ready = TryGetMap(out UnityScene current);
            if ((ready && current != map) || (!ready && map.IsValid())) { RefreshTargets(); return; }
            var player = PlayerController.Instance;
            bool canRun = ready && player != null && player.CanRunCheat;
            foreach (ItemRow row in items)
            {
                int count = player != null ? player.CheatItemCount(row.Item) : 0;
                row.Count.text = $"보유 {count} / 최대 묶음 {row.Item.MaxStackCount}";
                row.Give.SetEnabled(canRun && player.CanStoreInventoryItem(row.Item));
                MonoBehaviour pickup = canRun ? FindPickup(row, player) : null;
                row.Pick.SetEnabled(pickup != null);
                row.Pick.tooltip = pickup != null ? "실제 획득 처리: " + ObjectPath(pickup.transform) : "활성 획득 객체가 없거나 가방에 넣을 수 없습니다.";
                row.Consume.SetEnabled(canRun && count > 0);
                ItemExchangeInteraction use = SelectedUse(row);
                row.Use.SetEnabled(canRun && IsMapObject(use) && use.CanInteract(player));
            }
        }
    }
}
