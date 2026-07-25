using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;

        IDamageable victim = other.GetComponentInParent<IDamageable>();

        if (victim != null)
        {
            // 계산기 실행 (크리티컬 없이 순수 데미지만)
            DamageInfo info = DamageCalculator.CalculatePlayerDamage(StageManager.Instance.player, victim);

            // 맞은 지점 기록
            info.hitPoint = other.ClosestPoint(transform.position);

            // 데미지 전달
            victim.TakeDamage(info);
        }
    }
}