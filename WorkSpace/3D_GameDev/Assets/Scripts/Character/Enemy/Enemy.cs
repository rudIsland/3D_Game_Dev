using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Collections;
using System;
using System.Linq;

public abstract class Enemy : CharacterBase
{
    public Animator animator;
    public EnemyMemory enemyMemory;
    protected ENode behaviorTree;

    [SerializeField] protected float detectRange = 10f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float angularSpeed = 180f;

    [Header("���� and ����ġ")]
    [SerializeField] public Level level;
    protected float deathEXP;

    [Header("���� ����")]
    [SerializeField] private EnemyStats EnemyStat = new EnemyStats(); // Inspector�� ǥ�õ�
    public override CharacterStats Stat => EnemyStat; //�θ�����
    public EnemyStats enemyStat => EnemyStat;        //�ڽĿ��� ���ٿ�

    public NavMeshAgent agent;

    public bool isAttacking = false;

    [Header("���� ü��UI")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    public static event Action<float> OnEnemyKilled; // ����ġ ���� �̺�Ʈ

    [Header("���͸���")]
    public Material[] defaultMtl;
    public Material[] detectedMtl;
    public Material[] TargetMtl;
    public Material[] DeadMtl;

    public bool isTarget; //���� ����� �÷��̾��� Ÿ������ Ȯ��


    public Material[] currentMtl;

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

        //���� ���͸���
        currentMtl = new Material[GetComponentInChildren<SkinnedMeshRenderer>().materials.Count()];
        defaultMtl = GetComponentInChildren<SkinnedMeshRenderer>().materials.ToArray(); // ���纻 ����

        //�⺻ ���͸���
        defaultMtl = new Material[GetComponentInChildren<SkinnedMeshRenderer>().materials.Count()];
        defaultMtl = GetComponentInChildren<SkinnedMeshRenderer>().materials.ToArray(); // ���纻 ����


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

        UpdateHPUI();
        SetupTree(); // Ʈ�� ������ �ڽ��� ����
    }

    protected virtual void Update()
    {
        if (Player.Instance.playerStateMachine.currentState is PlayerDeadState) return;
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
        //�÷��̾�� ����ġ �ֱ�
        // ����ġ �̺�Ʈ �߻�
        ChangeDeadMtl();

        OnEnemyKilled?.Invoke(deathEXP);

        //������� ����
        StartCoroutine(DissolveCoroutine());

        StartCoroutine(DestroyEnemy());
    }

    private IEnumerator DissolveCoroutine()
    {
        float t = 0f;
        float duration = 5f; // �� �� ���� ������ �������

        while (t < duration)
        {
            t += Time.deltaTime;
            float dissolve = Mathf.Clamp01(t / duration);

            foreach (var mat in GetComponentInChildren<SkinnedMeshRenderer>().materials)
            {
                if (mat.HasProperty("_NoiseAmount"))
                    mat.SetFloat("_NoiseAmount", dissolve);
            }

            yield return null;
        }

        // ������ �����
        foreach (var mat in GetComponentInChildren<SkinnedMeshRenderer>().materials)
        {
            mat.SetFloat("_NoiseAmount", 1f);
        }
    }

    private IEnumerator DestroyEnemy()
    {
        yield return new WaitForSeconds(5f);

        Destroy(gameObject);
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
        // if (!GameManager.Instance.player.isDead) return; 
        enemyMemory.isPlayerDetected = enemyMemory.distanceToPlayer <= detectRange;
        enemyMemory.isInAttackRange = enemyMemory.distanceToPlayer <= attackRange;

        //Ž���� ���� ���͸��� ��ȭ
        if (enemyMemory.isPlayerDetected) ChangeDetectedMtl();
        else ChangeDefaultMtl();
    }

    //�⺻ ���͸���� ����
    public virtual void ChangeDefaultMtl()
    {
        currentMtl = defaultMtl;
        GetComponentInChildren<SkinnedMeshRenderer>().materials = defaultMtl;
    }

    public virtual void ChangeDeadMtl()
    {
        currentMtl = DeadMtl;
        GetComponentInChildren<SkinnedMeshRenderer>().materials = DeadMtl;
    }

    //�߰��Ҷ� ���͸��� ����
    public virtual void ChangeDetectedMtl()
    {
        if (!isTarget)
        {
            currentMtl = detectedMtl;
            GetComponentInChildren<SkinnedMeshRenderer>().materials = detectedMtl;
        }
    }

    public virtual void ChangeTargettMtl()
    {
        if (isTarget)
        {
            currentMtl = TargetMtl;
            GetComponentInChildren<SkinnedMeshRenderer>().materials = TargetMtl;
        }
    }

    protected abstract void SetupStats(); 
    protected abstract void SetupTree(); // �ڽ� Ŭ������ override

    /*********************  Animation Functions ***********************/

    
}
