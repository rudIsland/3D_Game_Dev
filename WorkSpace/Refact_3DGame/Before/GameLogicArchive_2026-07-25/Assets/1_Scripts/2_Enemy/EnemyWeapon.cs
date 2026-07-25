using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    public int weaponIndex;
    private Enemy _owner;

    private void Awake() => _owner = GetComponentInParent<Enemy>();

    private void OnTriggerEnter(Collider other)
    {
        // 1. 같은 적끼리는 팀킬 방지 및 트리거 중복 체크 방지
        if (other.CompareTag("Enemy") || other.isTrigger) return;

        // 2. 맞는 대상이 IDamageable(플레이어 등)인지 확인
        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            // [핵심] 계산기를 통해 적의 공격력과 맞는 대상의 방어력을 대조하여 데미지 산출
            DamageInfo info = DamageCalculator.CalculateEnemyDamage(_owner, weaponIndex, damageable);

            // 맞은 지점 기록
            info.hitPoint = other.ClosestPoint(transform.position);

            // 최종 데미지 전달
            damageable.TakeDamage(info);

            Debug.Log($"적의 공격! {other.name}이 {info.amount:F1}의 피해를 입었습니다.");
        }
    }
}