using UnityEngine;

public class EnemyMemory
{
    public Transform self; //�ڱ� �ڽ� ��ġ
    public Transform player; //�÷��̾� ��ġ

    public float distanceToPlayer; //�÷��̾���� �Ÿ�
    public bool isPlayerDetected; //�÷��̾� Ž������
    public bool isInAttackRange; //���ݹ��� ����
}
