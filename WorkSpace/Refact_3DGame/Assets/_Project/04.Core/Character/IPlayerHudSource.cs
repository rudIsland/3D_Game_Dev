using System;
using UnityEngine;

namespace Core
{
    /// <summary>초기화된 플레이어의 표시 값과 알림이다. HUD는 IsAvailable 동안 수치·사망 알림을 구독하고 해제한다.</summary>
    public interface IPlayerHudSource
    {
        // 미니맵 추적에만 사용한다. 소유 객체가 파괴되면 null이다.
        Transform FollowTarget { get; }
        bool IsAvailable { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
        float MaximumHealthScale { get; }
        float CurrentStamina { get; }
        float MaxStamina { get; }
        float MaximumStaminaScale { get; }
        PlayerInteractionGuide CurrentInteractionGuide { get; }

        /// <summary>표시할 슬롯의 아이템을 조회한다. 빈 슬롯이나 범위 밖이면 null이다.</summary>
        ItemDefinition GetInventoryItem(int slotIndex);

        // 활성·비활성 알림은 연결 동안, 수치 알림은 표시하는 동안만 구독한다.
        event Action<bool> AvailabilityChanged;
        event Action HealthChanged;
        event Action Died;
        event Action StaminaChanged;
        event Action InventoryChanged;
        event Action<PlayerInteractionGuide> InteractionGuideChanged;
    }
}
