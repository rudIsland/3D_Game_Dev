using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    protected EnemyMemory enemyMemory;
    protected ENode behaviorTree;

    [SerializeField] protected float detectRange = 10f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float moveSpeed = 3f;

    protected virtual void Start()
    {
        SetupStats();

        enemyMemory = new EnemyMemory
        {
            self = transform,
            player = GameObject.FindWithTag("Player").transform
        };
        SetupTree(); // 트리 구성은 자식이 정의
    }

    protected virtual void Update()
    {
        enemyMemory.distanceToPlayer = Vector3.Distance(transform.position, enemyMemory.player.position);
        enemyMemory.isPlayerDetected = enemyMemory.distanceToPlayer <= detectRange;
        enemyMemory.isInAttackRange = enemyMemory.distanceToPlayer <= attackRange;

        behaviorTree?.Evaluate();
    }

    protected abstract void SetupStats(); 

    protected abstract void SetupTree(); // 자식 클래스가 override
}
