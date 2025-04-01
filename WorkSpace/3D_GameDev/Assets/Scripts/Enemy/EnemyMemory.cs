using UnityEngine;

public class EnemyMemory
{
    public Transform self; //자기 자신 위치
    public Transform player; //플레이어 위치

    public float distanceToPlayer; //플레이어와의 거리
    public bool isPlayerDetected; //플레이어 탐지여부
    public bool isInAttackRange; //공격범위 여부
}
