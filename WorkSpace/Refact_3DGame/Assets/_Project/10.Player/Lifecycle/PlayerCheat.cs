#if UNITY_EDITOR
using UnityEngine;
using Core;

namespace Player
{
    // Inspector·치트 창 입력으로 기존 플레이어 체력·피격·가방 처리를 호출한다.
    public sealed partial class PlayerController
    {
        /// <summary>활성화된 살아 있는 플레이어에게 치트를 실행할 수 있는지 확인한다.</summary>
        public bool CanRunCheat => Application.isPlaying && isActiveAndEnabled && playerUnit != null && !IsDead;

        /// <summary>방어·구르기 판정 없이 기존 체력 API로 지정량을 감소시킨다. 실제 감소량을 반환한다.</summary>
        public float CheatDecreaseHealth(float amount)
        {
            if (!CanRunCheat || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return 0f;
            float before = playerUnit.CurrentHealth;
            playerUnit.Health.TakeDamage(amount);
            return before - playerUnit.CurrentHealth;
        }

        /// <summary>남은 체력만큼 피해를 적용해 기존 체력 변경·사망 이벤트를 실행한다.</summary>
        public bool CheatKill()
        {
            if (!CanRunCheat) return false;
            CheatDecreaseHealth(playerUnit.CurrentHealth);
            return IsDead;
        }

        /// <summary>기존 피격 판정을 실행한다. 구르기 무적 등 게임의 판정 결과를 그대로 반환한다.</summary>
        public PlayerHitResult CheatTakeHit(float amount)
        {
            if (!CanRunCheat || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
                return PlayerHitResult.Ignored;
            var damage = new AttackDamage(amount, AttackStrength.Light, 0f, 0f, 0f, false);
            var request = new PlayerHitRequest(damage, transform.position, Vector3.zero);
            return TryTakeHit(in request);
        }

        /// <summary>월드 아이템을 건드리지 않고 기존 획득 API로 아이템 한 개를 지급한다.</summary>
        public bool CheatGiveItem(ItemDefinition item) => CanRunCheat && TryStoreInventoryItem(item);

        /// <summary>효과나 교환 없이 기존 가방 API로 한 개를 소모한다. Changed 알림은 그대로 발생한다.</summary>
        public bool CheatConsumeItem(ItemDefinition item) => CanRunCheat && playerUnit.Inventory.TryRemove(item, 1);

        /// <summary>현재 가방에 있는 지정 아이템의 수량을 읽는다.</summary>
        public int CheatItemCount(ItemDefinition item)
        {
            if (playerUnit == null || item == null) return 0;
            for (int i = 0; i < PlayerInventory.SlotCount; i++)
                if (playerUnit.Inventory.GetItem(i) == item) return playerUnit.Inventory.GetCount(i);
            return 0;
        }

        // 준비된 플레이어에게 피해를 주고 전후 체력을 기록한다.
        [ContextMenu("Test Damage")]
        private void TestDamage()
        {
            if (!Application.isPlaying || playerUnit == null)
            {
                Debug.LogWarning("Test Damage는 Play 중이고 플레이어 준비가 끝난 뒤 사용할 수 있습니다.", this);
                return;
            }

            float healthBeforeDamage = playerUnit.CurrentHealth;
            var damage = new AttackDamage(
                10f,
                AttackStrength.Light,
                0f,
                0f,
                0f,
                false);
            var hitRequest = new PlayerHitRequest(
                damage,
                transform.position,
                Vector3.zero);
            TryTakeHit(in hitRequest);

            Debug.Log($"플레이어 체력: {healthBeforeDamage} → {playerUnit.CurrentHealth}", this);
        }
    }
}
#endif
