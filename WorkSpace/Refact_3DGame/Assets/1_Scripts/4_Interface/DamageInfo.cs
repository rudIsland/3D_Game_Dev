using UnityEngine;

public struct DamageInfo
{
    public float amount;      // 최종 데미지 수치
    public GameObject attacker; // 공격 주체 (경험치나 킬 카운트 처리용)
    public Vector3 hitPoint;  // 맞은 지점 (이펙트 생성용)

    public DamageInfo(float amount, GameObject attacker)
    {
        this.amount = amount;
        this.attacker = attacker;
        this.hitPoint = Vector3.zero;
    }
}