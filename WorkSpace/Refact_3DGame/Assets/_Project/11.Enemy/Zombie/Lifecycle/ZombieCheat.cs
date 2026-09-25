using Core;
#if UNITY_EDITOR
using UnityEngine;
using Enemy;

namespace Zombie
{
    // Inspector 치트 수치와 실행 메뉴를 좀비 런타임 구현에서 분리한다.
    public sealed partial class ZombieController
    {
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f;
        [SerializeField, Min(0f)] private float testStaggerDamage = 50f;

        // 기존 OnValidate에서 요청한 치트 경직 수치의 하한을 유지한다.
        private void ValidateCheatValues()
        {
            testStaggerDamage = Mathf.Max(0f, testStaggerDamage);
        }

        // 준비된 좀비에게 지정한 피해·경직을 적용하고 체력을 기록한다.
        [ContextMenu("Test Damage")]
        private void TestDamage()
        {
            if (!Application.isPlaying || zombieWorldUnit == null)
            {
                Debug.LogWarning("Test Damage는 Play 중이고 좀비 준비가 끝난 뒤 사용할 수 있습니다.", this);
                return;
            }

            float healthBeforeDamage = zombieWorldUnit.CurrentHealth;

            var hitRequest = new EnemyHitRequest(
                testDamage,
                testStaggerDamage,
                transform.position,
                -transform.forward,
                0.25f);
            zombieWorldUnit.TakeHit(in hitRequest);

            Debug.Log($"좀비 체력: {healthBeforeDamage} → {zombieWorldUnit.CurrentHealth}", this);
        }
    }
}
#endif
