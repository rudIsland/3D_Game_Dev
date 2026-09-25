using System;
using Core;
using UnityEngine;

namespace Player
{
    // HUD 계약 구현을 모은다. 표시 값은 기존 실행 객체에서 읽고 구독 중에만 기존 알림을 전달한다.
    public sealed partial class PlayerController : IPlayerHudSource
    {
        Transform IPlayerHudSource.FollowTarget => this != null ? transform : null;
        bool IPlayerHudSource.IsAvailable => this != null && playerUnit != null && playerUnit.IsEnabled;
        float IPlayerHudSource.CurrentHealth => playerUnit?.CurrentHealth ?? 0f;
        float IPlayerHudSource.MaxHealth => playerUnit?.Health.MaxHealth ?? 0f;
        float IPlayerHudSource.MaximumHealthScale => playerUnit?.MaximumHealthScale ?? 1f;
        float IPlayerHudSource.CurrentStamina => playerUnit?.CurrentStamina ?? 0f;
        float IPlayerHudSource.MaxStamina => playerUnit?.MaxStamina ?? 0f;
        float IPlayerHudSource.MaximumStaminaScale => playerUnit?.MaximumStaminaScale ?? 1f;
        PlayerInteractionGuide IPlayerHudSource.CurrentInteractionGuide => interactionController != null
            ? interactionController.CurrentInteractionGuide : PlayerInteractionGuide.Hidden;

        // 가방 구현을 노출하지 않고 기존 슬롯 조회 결과만 전달한다.
        ItemDefinition IPlayerHudSource.GetInventoryItem(int slotIndex) => playerUnit?.Inventory.GetItem(slotIndex);

        private event Action<bool> hudAvailabilityChanged;
        private event Action hudHealthChanged;
        private event Action hudStaminaChanged;
        private event Action hudInventoryChanged;
        private event Action<PlayerInteractionGuide> hudInteractionGuideChanged;

        // 첫 구독 때 기존 이벤트에 연결하고 마지막 해제 때 끊어 원래 구독 시점을 유지한다.
        event Action<bool> IPlayerHudSource.AvailabilityChanged
        {
            add
            {
                if (value == null) return;
                if (hudAvailabilityChanged == null)
                {
                    PlayerEnabled += NotifyHudEnabled;
                    PlayerDisabled += NotifyHudDisabled;
                }
                hudAvailabilityChanged += value;
            }
            remove
            {
                hudAvailabilityChanged -= value;
                if (hudAvailabilityChanged != null) return;
                PlayerEnabled -= NotifyHudEnabled;
                PlayerDisabled -= NotifyHudDisabled;
            }
        }

        // 체력 변경 시점에 HUD가 기존 체력 값을 다시 읽도록 알린다.
        event Action IPlayerHudSource.HealthChanged
        {
            add
            {
                if (value == null) return;
                if (hudHealthChanged == null) playerUnit.Health.HealthChanged += NotifyHudHealthChanged;
                hudHealthChanged += value;
            }
            remove
            {
                hudHealthChanged -= value;
                if (hudHealthChanged == null && playerUnit != null)
                    playerUnit.Health.HealthChanged -= NotifyHudHealthChanged;
            }
        }

        // 기존 사망 이벤트와 같은 순서로 연결한다. Unit 반환 시 남은 구독도 해제된다.
        event Action IPlayerHudSource.Died
        {
            add { if (value != null) playerUnit.Health.Died += value; }
            remove { if (playerUnit != null) playerUnit.Health.Died -= value; }
        }

        // 스태미나 구현 타입이 HUD로 넘어가지 않도록 변경 알림만 전달한다.
        event Action IPlayerHudSource.StaminaChanged
        {
            add
            {
                if (value == null) return;
                if (hudStaminaChanged == null) playerUnit.Stamina.StaminaChanged += NotifyHudStaminaChanged;
                hudStaminaChanged += value;
            }
            remove
            {
                hudStaminaChanged -= value;
                if (hudStaminaChanged == null && playerUnit != null)
                    playerUnit.Stamina.StaminaChanged -= NotifyHudStaminaChanged;
            }
        }

        // 아이템 변경이 끝난 기존 이벤트에서 같은 슬롯을 조회하도록 알린다.
        event Action IPlayerHudSource.InventoryChanged
        {
            add
            {
                if (value == null) return;
                if (hudInventoryChanged == null) playerUnit.Inventory.Changed += NotifyHudInventoryChanged;
                hudInventoryChanged += value;
            }
            remove
            {
                hudInventoryChanged -= value;
                if (hudInventoryChanged == null && playerUnit != null)
                    playerUnit.Inventory.Changed -= NotifyHudInventoryChanged;
            }
        }

        // 안내 컴포넌트를 공개하지 않고 기존 문구·표시 여부 알림을 전달한다.
        event Action<PlayerInteractionGuide> IPlayerHudSource.InteractionGuideChanged
        {
            add
            {
                if (value == null || interactionController == null) return;
                if (hudInteractionGuideChanged == null)
                    interactionController.InteractionGuideChanged += NotifyHudInteractionGuideChanged;
                hudInteractionGuideChanged += value;
            }
            remove
            {
                hudInteractionGuideChanged -= value;
                if (hudInteractionGuideChanged == null && interactionController != null)
                    interactionController.InteractionGuideChanged -= NotifyHudInteractionGuideChanged;
            }
        }

        // 기존 활성·비활성 알림을 HUD의 표시 가능 여부로 전달한다.
        private void NotifyHudEnabled(Unit unit) => hudAvailabilityChanged?.Invoke(true);
        private void NotifyHudDisabled(Unit unit) => hudAvailabilityChanged?.Invoke(false);

        // 수치와 아이템은 복사해 보관하지 않고 변경 사실만 알린다.
        private void NotifyHudHealthChanged(UnitHealth health) => hudHealthChanged?.Invoke();
        private void NotifyHudStaminaChanged(PlayerStamina stamina) => hudStaminaChanged?.Invoke();
        private void NotifyHudInventoryChanged(PlayerInventory changedInventory) => hudInventoryChanged?.Invoke();
        private void NotifyHudInteractionGuideChanged(PlayerInteractionGuide guide) => hudInteractionGuideChanged?.Invoke(guide);

        // 플레이어 최종 반환에서 중계 구독을 끊는다. Disable은 재활성화를 위해 연결을 유지한다.
        private void ReleaseHudSubscriptions()
        {
            PlayerEnabled -= NotifyHudEnabled;
            PlayerDisabled -= NotifyHudDisabled;
            if (playerUnit != null)
            {
                playerUnit.Health.HealthChanged -= NotifyHudHealthChanged;
                playerUnit.Stamina.StaminaChanged -= NotifyHudStaminaChanged;
                playerUnit.Inventory.Changed -= NotifyHudInventoryChanged;
            }
            if (interactionController != null)
                interactionController.InteractionGuideChanged -= NotifyHudInteractionGuideChanged;
            hudAvailabilityChanged = null;
            hudHealthChanged = null;
            hudStaminaChanged = null;
            hudInventoryChanged = null;
            hudInteractionGuideChanged = null;
        }
    }
}
