using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    protected EnemyMemory enemyMemory;
    protected ENode behaviorTree;

    [SerializeField] protected float detectRange = 10f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float moveSpeed = 3f;
    public WeaponColider weapon;

    public bool isAttacking = false;

    protected virtual void Start()
    {
        weapon.gameObject.SetActive(false);

        SetupStats();

        enemyMemory = new EnemyMemory
        {
            self = transform,
            player = GameObject.FindWithTag("Player").transform
        };
        SetupTree(); // Ʈ�� ������ �ڽ��� ����
    }

    protected virtual void Update()
    {
        enemyMemory.distanceToPlayer = Vector3.Distance(transform.position, enemyMemory.player.position);
        enemyMemory.isPlayerDetected = enemyMemory.distanceToPlayer <= detectRange;
        enemyMemory.isInAttackRange = enemyMemory.distanceToPlayer <= attackRange;
        //Debug.Log($" �Ÿ�: {enemyMemory.distanceToPlayer}, ���� ����: {attackRange}, isInAttackRange: {enemyMemory.isInAttackRange}");

        behaviorTree?.Evaluate();
    }

    protected abstract void SetupStats(); 

    protected abstract void SetupTree(); // �ڽ� Ŭ������ override

    private void OnWeapon()
    {
        weapon.gameObject.SetActive(true);
    }
    private void OffWeapon()
    {
        weapon.gameObject.SetActive(false);
    }
}
