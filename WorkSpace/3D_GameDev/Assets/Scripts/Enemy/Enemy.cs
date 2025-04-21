using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using static Cinemachine.DocumentationSortingAttribute;

public abstract class Enemy : CharacterBase
{
    public Animator animator;
    public EnemyMemory enemyMemory;
    protected ENode behaviorTree;

    [SerializeField] protected float detectRange = 10f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float angularSpeed = 180f;

    [Header("����")]
    [SerializeField] public Level level;

    public NavMeshAgent agent;

    public bool isAttacking = false;

    //public bool isDead = false;
    public override CharacterStatsComponent statComp => GetComponent<EnemyStatComponent>();

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        
        // Level
        level = new Level();
    }

    protected virtual void Start()
    {

        SetupStats(); //�ڽĿ��� �������̵�

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

    public override void ApplyDamage(double damage) { } //IDamagable interface

    protected virtual void HandleDeath()
    {
        Debug.Log("�� ���");
    }

    protected virtual void RotateTowardsPlayer()
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
    protected virtual void UpdateDetectionStatus()
    {
        enemyMemory.isPlayerDetected = enemyMemory.distanceToPlayer <= detectRange && !GameManager.Instance.playerStateMachine.isDead;
        enemyMemory.isInAttackRange = enemyMemory.distanceToPlayer <= attackRange && !GameManager.Instance.playerStateMachine.isDead;
    }

    protected abstract void SetupStats(); 
    protected abstract void SetupTree(); // �ڽ� Ŭ������ override

    /*********************  Animation Functions ***********************/

    
}
