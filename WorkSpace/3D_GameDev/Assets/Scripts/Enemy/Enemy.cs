using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public abstract class Enemy : MonoBehaviour
{
    public EnemyMemory enemyMemory;
    protected ENode behaviorTree;

    [SerializeField] protected float detectRange = 10f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float angularSpeed = 180f;

    public CharacterStatsComponent stats;


    public WeaponColider weapon;
    public NavMeshAgent agent;

    public bool isAttacking = false;

    private void Awake()
    {
        stats = GetComponent<CharacterStatsComponent>();
        stats.OnDeath += HandleDeath;
    }

    protected virtual void Start()
    {
        weapon.gameObject.SetActive(false);

        SetupStats();

        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange;
        agent.angularSpeed = angularSpeed;
        agent.updateRotation = true;   // �ڵ� ȸ�� Ȱ��ȭ
        agent.updateUpAxis = true;     // �⺻ Y�� ȸ��

        enemyMemory = new EnemyMemory
        {
            self = transform,
            player = GameObject.FindWithTag("Player").transform
        };
        SetupTree(); // Ʈ�� ������ �ڽ��� ����
    }

    protected virtual void Update()
    {
        UpdateDistanceToPlayer();
        UpdateDetectionStatus();

        // Ž�� �� �Ǹ� ���߱�
        if (!enemyMemory.isPlayerDetected)
        {
            agent.isStopped = true;
            return;
        }

        // Ž���Ǹ� Ʈ�� �۵�
        agent.isStopped = false;
        behaviorTree?.Evaluate();

        // ���� �Ÿ� ���̸� ȸ���� (�̵��� �� ��)
        if (enemyMemory.isInAttackRange && !isAttacking)
        {
            if (agent.updateRotation) // ȸ�� ���� �� �Ǿ� ������ ��
                agent.updateRotation = false;

            RotateTowardsPlayer(); // ���� ȸ�� ó��
        }
        else
        {
            if (!agent.updateRotation) // ȸ�� ���� ������ �ٽ� ��
                agent.updateRotation = true;
        }
    }

    public void TakeDamage(double damage)
    {
        stats.TakeDamage(damage);
    }

    private void HandleDeath()
    {
        Debug.Log("�� ���");
        // ���� ���� ó��
    }

    protected void RotateTowardsPlayer()
    {
        Vector3 direction = (enemyMemory.player.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }


    //�÷��̾���� �Ÿ����
    protected void UpdateDistanceToPlayer()
    {
        Vector3 toPlayer = enemyMemory.player.position - transform.position;
        toPlayer.y = 0f; // ��� �Ÿ�
        enemyMemory.distanceToPlayer = toPlayer.magnitude;
    }

    //Ž������ üũ
    protected void UpdateDetectionStatus()
    {
        enemyMemory.isPlayerDetected = enemyMemory.distanceToPlayer <= detectRange;
        enemyMemory.isInAttackRange = enemyMemory.distanceToPlayer <= attackRange;
    }

    protected abstract void SetupStats(); 
    protected abstract void SetupTree(); // �ڽ� Ŭ������ override


    //Animation Functions
    private void OnWeapon()
    {
        weapon.gameObject.SetActive(true);
    }
    private void OffWeapon()
    {
        weapon.gameObject.SetActive(false);
    }
    
}
