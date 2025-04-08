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
        agent.updateRotation = true;   // 자동 회전 활성화
        agent.updateUpAxis = true;     // 기본 Y축 회전

        enemyMemory = new EnemyMemory
        {
            self = transform,
            player = GameObject.FindWithTag("Player").transform
        };
        SetupTree(); // 트리 구성은 자식이 정의
    }

    protected virtual void Update()
    {
        UpdateDistanceToPlayer();
        UpdateDetectionStatus();

        // 탐지 안 되면 멈추기
        if (!enemyMemory.isPlayerDetected)
        {
            agent.isStopped = true;
            return;
        }

        // 탐지되면 트리 작동
        agent.isStopped = false;
        behaviorTree?.Evaluate();

        // 공격 거리 안이면 회전만 (이동은 안 함)
        if (enemyMemory.isInAttackRange && !isAttacking)
        {
            if (agent.updateRotation) // 회전 중지 안 되어 있으면 끔
                agent.updateRotation = false;

            RotateTowardsPlayer(); // 직접 회전 처리
        }
        else
        {
            if (!agent.updateRotation) // 회전 꺼져 있으면 다시 켬
                agent.updateRotation = true;
        }
    }

    public void TakeDamage(double damage)
    {
        stats.TakeDamage(damage);
    }

    private void HandleDeath()
    {
        Debug.Log("적 사망");
        // 게임 오버 처리
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


    //플레이어와의 거리계산
    protected void UpdateDistanceToPlayer()
    {
        Vector3 toPlayer = enemyMemory.player.position - transform.position;
        toPlayer.y = 0f; // 평면 거리
        enemyMemory.distanceToPlayer = toPlayer.magnitude;
    }

    //탐지범위 체크
    protected void UpdateDetectionStatus()
    {
        enemyMemory.isPlayerDetected = enemyMemory.distanceToPlayer <= detectRange;
        enemyMemory.isInAttackRange = enemyMemory.distanceToPlayer <= attackRange;
    }

    protected abstract void SetupStats(); 
    protected abstract void SetupTree(); // 자식 클래스가 override


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
