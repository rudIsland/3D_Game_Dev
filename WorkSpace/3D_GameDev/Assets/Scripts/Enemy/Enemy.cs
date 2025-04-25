using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public abstract class Enemy : CharacterBase
{
    public Animator animator;
    public EnemyMemory enemyMemory;
    protected ENode behaviorTree;

    [SerializeField] protected float detectRange = 10f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float angularSpeed = 180f;

    [Header("레벨 and 경험치")]
    [SerializeField] public Level level;
    protected float deathEXP;

    [Header("공통 스탯")]
    [SerializeField] private EnemyStats EnemyStat = new EnemyStats(); // Inspector에 표시됨
    public override CharacterStats Stat => EnemyStat; //부모전용
    public EnemyStats enemyStat => EnemyStat;        //자식에서 접근용

    public NavMeshAgent agent;

    public bool isAttacking = false;

    [Header("공통 체력UI")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    public void UpdateResource()
    {
        UpdateHPUI();
    }

    public virtual void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = (float)(enemyStat.currentHP / enemyStat.maxHP);
            hpText.text = (int)enemyStat.currentHP + "/" + enemyStat.maxHP.ToString();
        }
    }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        
        // Level
        level = new Level();
    }

    protected virtual void Start()
    {

        SetupStats(); //자식에서 오버라이딩

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

        UpdateHPUI();
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

    public override void ApplyDamage(double damage) { } //IDamagable interface

    protected virtual void HandleDeath()
    {
        Debug.Log("적 사망");
        //플레이어에게 경험치 넣기
        GameManager.Instance.getPlayerExpKillEnemy(deathEXP);
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


    //플레이어와의 거리계산
    protected void UpdateDistanceToPlayer()
    {
        Vector3 toPlayer = enemyMemory.player.position - transform.position;
        toPlayer.y = 0f; // 평면 거리
        enemyMemory.distanceToPlayer = toPlayer.magnitude;
    }

    //탐지범위 체크
    protected virtual void UpdateDetectionStatus()
    {
        enemyMemory.isPlayerDetected = enemyMemory.distanceToPlayer <= detectRange && !GameManager.Instance.player.isDead;
        enemyMemory.isInAttackRange = enemyMemory.distanceToPlayer <= attackRange && !GameManager.Instance.player.isDead;
    }

    protected abstract void SetupStats(); 
    protected abstract void SetupTree(); // 자식 클래스가 override

    /*********************  Animation Functions ***********************/

    
}
