#if UNITY_EDITOR
using Characters.Combat;
using Characters.Combat.AttackData;
using Characters.Player.Combat.Hit;
using UnityEngine;

namespace Characters.Player.Lifecycle
{
    // Inspector 치트 입력으로 기존 플레이어 피해 처리를 호출한다.
    public sealed partial class PlayerController
    {
        // 준비된 플레이어에게 피해를 주고 전후 체력을 기록한다.
        [ContextMenu("Test Damage")]
        private void TestDamage()
        {
            if (!Application.isPlaying || playerWorldUnit == null)
            {
                Debug.LogWarning("Test Damage는 Play 중이고 플레이어 준비가 끝난 뒤 사용할 수 있습니다.", this);
                return;
            }

            float healthBeforeDamage = playerWorldUnit.CurrentHealth;
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

            Debug.Log($"플레이어 체력: {healthBeforeDamage} → {playerWorldUnit.CurrentHealth}", this);
        }
    }
}
#endif
