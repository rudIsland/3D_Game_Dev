using Characters;
using System;
using Characters.Player.Inventory;
using Characters.Player.Lifecycle;
using Items;
using UnityEngine;

namespace World.Quests
{
    // 인벤토리의 실제 행동과 씬의 출구 도착을 퀘스트 진행에 전달한다.
    [DisallowMultipleComponent]
    public sealed class GroundQuestController : MonoBehaviour
    {
        [SerializeField] private ItemCatalog itemCatalog;
        [SerializeField] private PlayerController player;
        [Tooltip("출구가 만들어지면 도착 범위를 연결합니다.")]
        [SerializeField] private BoxCollider exitArea;

        private GroundQuestProgress progress;
        private PlayerInventory inventory;
        [SerializeField] private ItemDefinition book;
        [SerializeField] private ItemDefinition scroll;
        private bool connected;

        public GroundQuestStep Step => progress != null ? progress.Step : GroundQuestStep.FindBook;
        public bool HasExitArea => exitArea != null;
        public event Action Changed;

        public void Connect(PlayerController playerController, GroundQuestProgress state)
        {
            Disconnect();
            progress = state;
            player = playerController;
            if (itemCatalog != null)
            {
                if (book == null && itemCatalog.TryGetItem(ItemType.Book, out ItemCatalogEntry bookEntry)) book = bookEntry.ItemDefinition;
                if (scroll == null && itemCatalog.TryGetItem(ItemType.Scroll, out ItemCatalogEntry scrollEntry)) scroll = scrollEntry.ItemDefinition;
            }
            itemCatalog = null;
            if (progress == null || player == null || book == null || scroll == null)
            {
                Debug.LogError("GroundQuestController에 퀘스트 진행 기록, 플레이어, 책과 스크롤 목록을 연결하세요.", this);
                enabled = false;
                return;
            }
            if (isActiveAndEnabled) OnEnable();
        }

        private void OnEnable()
        {
            if (connected || progress == null || player == null || book == null || scroll == null)
            {
                return;
            }

            connected = true;
            progress.Changed += HandleProgressChanged;
            player.PlayerEnabled += HandlePlayerEnabled;
            player.PlayerDisabled += HandlePlayerDisabled;
            if (player.RuntimeUnit != null && player.RuntimeUnit.IsEnabled)
                HandlePlayerEnabled(player.RuntimeUnit);
        }

        private void OnDisable() => Disconnect();

        public void Disconnect()
        {
            if (!connected)
            {
                return;
            }

            connected = false;
            progress.Changed -= HandleProgressChanged;
            if (player != null)
            {
                player.PlayerEnabled -= HandlePlayerEnabled;
                player.PlayerDisabled -= HandlePlayerDisabled;
            }
            DisconnectInventory();
        }

        public void Tick()
        {
            if (!connected || player == null || player.IsPaused) return;
            if (progress.Step != GroundQuestStep.ReachExit || exitArea == null ||
                !exitArea.enabled || !exitArea.gameObject.activeInHierarchy ||
                player == null || !player.isActiveAndEnabled || player.IsDead)
            {
                return;
            }

            Vector3 local = exitArea.transform.InverseTransformPoint(player.transform.position) - exitArea.center;
            Vector3 halfSize = exitArea.size * 0.5f;
            if (Mathf.Abs(local.x) <= halfSize.x && Mathf.Abs(local.y) <= halfSize.y &&
                Mathf.Abs(local.z) <= halfSize.z)
            {
                progress.RecordExitReached();
            }
        }

        private void HandlePlayerEnabled(Unit worldObject)
        {
            if (!(worldObject is PlayerWorldUnit playerUnit) ||
                ReferenceEquals(inventory, playerUnit.Inventory))
            {
                return;
            }

            DisconnectInventory();
            inventory = playerUnit.Inventory;
            inventory.Changed += HandleInventoryChanged;
            inventory.ItemExchanged += HandleItemExchanged;
            HandleInventoryChanged(inventory);
        }

        private void HandlePlayerDisabled(Unit worldObject)
        {
            if (worldObject is PlayerWorldUnit playerUnit &&
                ReferenceEquals(inventory, playerUnit.Inventory))
            {
                DisconnectInventory();
            }
        }

        private void DisconnectInventory()
        {
            if (inventory == null)
            {
                return;
            }
            inventory.Changed -= HandleInventoryChanged;
            inventory.ItemExchanged -= HandleItemExchanged;
            inventory = null;
        }

        private void HandleInventoryChanged(PlayerInventory changedInventory)
        {
            if (changedInventory.HasItem(book))
            {
                progress.RecordBookFound();
            }
        }

        private void HandleItemExchanged(ItemDefinition costItem, ItemDefinition rewardItem)
        {
            if (costItem == book && rewardItem == scroll)
            {
                progress.RecordBookExchanged();
            }
        }

        private void HandleProgressChanged()
        {
            Changed?.Invoke();
        }
    }
}
