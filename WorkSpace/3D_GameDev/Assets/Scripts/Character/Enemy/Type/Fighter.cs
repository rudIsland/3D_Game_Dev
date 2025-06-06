using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class Fighter : Enemy
{
    private enum AttackType {
        FlyKick,
        JapCross,
        Kick
    }
    public readonly int _animIDIdle = Animator.StringToHash("Idle"); //���ֱ�
    public readonly int _animIDWalk = Animator.StringToHash("IsWalk"); //�ȱ�
    public readonly int _animIDDectected = Animator.StringToHash("IsDetected"); //Ž��
    public readonly int _animIDFightDectected = Animator.StringToHash("FightDecteced"); //�����غ�Ž��
    public readonly int _animIDFight = Animator.StringToHash("Fight"); //���ݸ�� ����
    public readonly int _animIDAttackIndex = Animator.StringToHash("AttackIndex"); //���ݸ�� ����
    public readonly int _animIDAttackTrigger = Animator.StringToHash("AttackTrigger"); //���ݸ�� ����
    public readonly int _animIDDead = Animator.StringToHash("IsDead"); //����

    [Header("Ž������")]
    private float DectectedRange = 7f;
    private float FightDectectedRange = 4.5f;
    private float FightRange = 1.55f;
    [SerializeField] private bool isDecteced;

    // ���� ���� Ÿ�̸� & ����
    private float jumpAttackTimer = 3f;
    private float FlyKickRange = 1.5f;
    private float JapCrossRange = 1.15f;
    private float KickRange = 1.3f;
    private float HookRange = 1.2f;
    private float JapRange = 1.0f;
    private float LowKickRange = 1.0f;
    //private float StepForwardRange = 1.5f;

    private const int ATTACK_JAP = 1;
    private const int ATTACK_LOWKICK = 2;
    private const int ATTACK_HOOK = 3;
    private const int ATTACK_KICK = 4;
    private const int ATTACK_JABCROSS = 5;
    private const int ATTACK_FLYKICK = 6;

    [Header("����")]
    [SerializeField] private EnemyWeapon LeftHandWeapon; //���� ��
    [SerializeField] private EnemyWeapon RightHandWeapon; //������ ��
    [SerializeField] private EnemyWeapon LeftFootWeapon; //���� ��
    [SerializeField] private EnemyWeapon RightFootWeapon; //������ ��
    protected override void Awake()
    {
        base.Awake(); //�ڱ��ڽ��� ������Ʈ ��������
    }
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        SetupStats();

        OnDeath += HandleDeath; //���� ��������Ʈ
        LeftHandWeapon.gameObject.SetActive(false);
        RightHandWeapon.gameObject.SetActive(false);
        LeftFootWeapon.gameObject.SetActive(false);
        RightFootWeapon.gameObject.SetActive(false);
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (enemyStat.IsDead) return;
        if (Player.Instance.playerStateMachine.currentState is PlayerDeadState)
        {
            ChangeDefaultMtl();
            return;
        }

        UpdateDistanceToPlayer();
        HandleDetectedState();
        

        if (jumpAttackTimer > 0f)
            jumpAttackTimer -= Time.deltaTime;

        behaviorTree?.Evaluate();
    }

    private void HandleDetectedState()
    {
        if (!(FightRange >= enemyMemory.distanceToPlayer))
        {
            // ���� ���� ��: �̵�/ȸ�� ����
            SetAgentStop(false);
            SetAgentRotation(true);
        }
        else if (!isAttacking)
        {
            // ���� ���� ��, ���� ��: ȸ���� ���
            SetAgentStop(true);
            SetAgentRotation(false);
            RotateTowardsPlayer();
        }
        else
        {
            // ���� ��: �̵�, ȸ�� ����
            SetAgentStop(true);
            SetAgentRotation(false);
        }
    }


    protected override void RotateTowardsPlayer()
    {
        Vector3 direction = (enemyMemory.player.position - transform.position).normalized;
        direction.y = 0;
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
    }

    protected override void SetupStats()
    {
        detectRange = DectectedRange; //Ž������
        attackRange = FightRange; //���ݹ���
        moveSpeed = 1.5f; //�̵��ӵ�
        angularSpeed = 260f; //ȸ���ӵ�

        level.SetLevel(20); //��������

        enemyStat.maxHP = 1000;
        enemyStat.ATK = 40f;
        enemyStat.DEF = 30f;
        enemyStat.currentHP = enemyStat.maxHP; //���� ü�¼���
        deathEXP = 4500;

        GetComponentInChildren<EnemyGUI>()?.UpdateLevel(); //GUI��������
    }
    /****************************** Ʈ�����  ************************************/
    protected override void SetupTree()
    {
        // Ž�� Ʈ��
        ENode detectTree = new SelectorNode(new List<ENode> {
            new ActionNode(DectectedPlayer),
        });

        // SetupTree() ����
        ENode attackAndMoveTree = new SelectorNode(new List<ENode> {
            new ActionNode(FightSelector),
            new ActionNode(MoveToPlayer)
        });

        // ��ü Ʈ�� ����
        behaviorTree = new SequenceNode(new List<ENode> {
            detectTree,
            attackAndMoveTree
        });
    }

    private ESTATE DectectedPlayer()
    {
        //if (GameManager.Instance.player.isDead) return ESTATE.FAILED;

        float distance = enemyMemory.distanceToPlayer;

        isDecteced = distance <= DectectedRange;
        // Ž�� ���� ����
        animator.SetBool(_animIDDectected, isDecteced);
        animator.SetBool(_animIDFightDectected, distance <= FightDectectedRange);
        animator.SetBool(_animIDWalk, (isDecteced && distance >= FightRange));
        animator.SetBool(_animIDFight, distance <= FightRange);

        // ���� ���� Ž�� �Ÿ� ���� ������ SUCCESS ��ȯ
        if (distance <= DectectedRange)
        {
            ChangeDetectedMtl();
            return ESTATE.SUCCESS;
        }

        // Ž�� ���� �� �ٷ� ����
        SetAgentStop(true);
        ChangeDefaultMtl();
        return ESTATE.FAILED;

    }

    private ESTATE FacePlayerCheck()
    {
        if (isAttacking) return ESTATE.SUCCESS; // ���� ���̸� ȸ�� ����

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE FightSelector()
    {
        float flyKickWeight = 0f;
        float japCrossWeight = 2f;
        float hookWeight = 2f;
        float kickWeight = 2f;
        float japWeight = 2f;
        float lowKickWeight = 2f;

        float distance = enemyMemory.distanceToPlayer;

        if (distance <= JapRange)
            japWeight += 8f;

        if (distance <= LowKickRange)
            lowKickWeight += 8f;

        if (distance <= HookRange)
            hookWeight += 8f;

        if (distance <= KickRange)
            kickWeight += 8f;

        if (distance <= JapCrossRange)
            japCrossWeight += 8f;

        if (distance <= FlyKickRange && jumpAttackTimer <= 0f)
            flyKickWeight = 5f;

        float totalWeight = flyKickWeight + japCrossWeight + hookWeight + kickWeight + japWeight + lowKickWeight;
        if (totalWeight <= 0f) return ESTATE.FAILED;

        float roll = Random.Range(0f, totalWeight);

        if (roll <= flyKickWeight)
            return FlyKickAttack();

        roll -= flyKickWeight;
        if (roll <= japCrossWeight)
            return JapCrossAttack();

        roll -= japCrossWeight;
        if (roll <= hookWeight)
            return HookAttack();

        roll -= hookWeight;
        if (roll <= kickWeight)
            return KickAttack();

        roll -= kickWeight;
        if (roll <= lowKickWeight)
            return LowKickAttack();

        return JapAttack();
    }


    //�̵�
    private ESTATE MoveToPlayer()
    {
        if (!isDecteced)
        {
            Debug.Log("[�̵�] Ž�� ���� �� �̵� ����");
            SetAgentStop(true);
            return ESTATE.FAILED;
        }

        // ���� �߿� �̵����� ����
        if (isAttacking)
        {
            Debug.Log("[�̵�] ���� �� �� �̵� ����");
            SetAgentStop(true);
            return ESTATE.SUCCESS;
        }

        // ���� ���� �� �̵�
        Debug.Log("[�̵�] �÷��̾�� �̵� ��");
        SetAgentStop(false);
        if (agent.enabled && agent.isOnNavMesh)
            agent.SetDestination(enemyMemory.player.position);

        return ESTATE.RUN;
    }

    //����

    private ESTATE JapCrossAttack()
    {
        // ���� ���� �˻� �� �ִϸ��̼� ���
        if (! (JapCrossRange >= enemyMemory.distanceToPlayer))
        {
            Debug.Log("[����] ��ũ�ν� ���� ����: ���� �ƴ�");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[����] ��ũ�ν� ���� ȸ�� ��...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[����] ��ũ�ν� ���� ����");
            NormalAttackingStart(); //���� ���°ǵ� �߰��غ� �ʹ� �ǰ��� ������

            animator.SetInteger(_animIDAttackIndex, ATTACK_JABCROSS);
            animator.SetTrigger(_animIDAttackTrigger);
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE KickAttack()
    {
        // ���� ���� �˻� �� �ִϸ��̼� ���
        if (!(KickRange >= enemyMemory.distanceToPlayer))
        {
            Debug.Log("[����] ű ���� ����: ���� �ƴ�");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[����] ű ���� ȸ�� ��...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[����] ű ���� ����");
            NormalAttackingStart(); //���� ���°ǵ� �߰��غ� �ʹ� �ǰ��� ������

            animator.SetInteger(_animIDAttackIndex, ATTACK_KICK);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE FlyKickAttack()
    {
        // ���� ���� �˻� �� �ִϸ��̼� ���
        if (!(FlyKickRange >= enemyMemory.distanceToPlayer))
        {
            Debug.Log("[����] �ö���ű ���� ����: ���� �ƴ�");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[����] �ö���ű ���� ȸ�� ��...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[����] �ö���ű ���� ����");
            NormalAttackingStart(); //���� ���°ǵ� �߰��غ� �ʹ� �ǰ��� ������
            jumpAttackTimer = 3f;
            animator.SetInteger(_animIDAttackIndex, ATTACK_FLYKICK);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE HookAttack()
    {
        if (!(HookRange >= enemyMemory.distanceToPlayer)) return ESTATE.FAILED;
        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }
        if (!isAttacking)
        {
            NormalAttackingStart();
            animator.SetInteger(_animIDAttackIndex, ATTACK_HOOK);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE LowKickAttack()
    {
        if (!(LowKickRange >= enemyMemory.distanceToPlayer)) return ESTATE.FAILED;
        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }
        if (!isAttacking)
        {
            NormalAttackingStart();
            animator.SetInteger(_animIDAttackIndex, ATTACK_LOWKICK);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE JapAttack()
    {
        if (!(JapRange >= enemyMemory.distanceToPlayer)) return ESTATE.FAILED;
        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }
        if (!isAttacking)
        {
            NormalAttackingStart();
            animator.SetInteger(_animIDAttackIndex, ATTACK_JAP);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    /****************************** HiT & ����  ************************************/

    private void OffHit()
    {
        isAttacking = false;
        //animator.SetBool(_animIDHit, false);
        //animator.SetBool(_animIDAttack, isAttacking);
    }

    public void OnLeftHandWeapon()
    {
        LeftHandWeapon.gameObject.SetActive(true);
    }
    public void OnRightHandWeapon()
    {
        RightHandWeapon.gameObject.SetActive(true);
    }
    public void OnLeftFootWeapon()
    {
        LeftFootWeapon.gameObject.SetActive(true);
    }
    public void OnRightFootWeapon()
    {
        RightFootWeapon.gameObject.SetActive(true);
    }
    public void OffWeapon()
    {
        LeftHandWeapon.gameObject.SetActive(false);
        RightHandWeapon.gameObject.SetActive(false);
        LeftFootWeapon.gameObject.SetActive(false);
        RightFootWeapon.gameObject.SetActive(false);
    }

    private void NormalAttackingStart()
    {
        isAttacking = true;
        animator.applyRootMotion = true;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        //animator.SetBool(_animIDAttack, true);
    }

    private void NormalAttackingEnd()
    {
        if (enemyStat.IsDead) return;

        isAttacking = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            animator.ResetTrigger(_animIDAttackTrigger);
            animator.SetInteger(_animIDAttackIndex, 0);
        }

    }

    /****************************** Agent And �˻� Logic  ************************************/

    protected override void HandleDeath()
    {
        Debug.Log("�� �״���...");
        animator.applyRootMotion = true;
        animator.SetTrigger(_animIDDead);

        //NavMeshAgent�� ��Ȱ��ȭ�ϱ� ���� ���� ���� ����
        SetAgentStop(true);

        agent.enabled = false; // ���� �����ϰ� ��Ȱ��ȭ
        GetComponent<Collider>().enabled = false;
        GetComponent<Target>().enabled = false;

        base.HandleDeath();
    }

    private bool IsFacingTarget(Vector3 target)
    {
        //Vector�� ������ ���ϰ� ����ȭ(���Ϻ���)
        Vector3 directionToTarget = (target - transform.position).normalized;
        //������ ����� �÷��̾� �������� ���� ���ϱ�
        float dot = Vector3.Dot(transform.forward, directionToTarget);
        return dot > 0.98f; // 0.95 �̻��̸� ���� ������ ���� �ִٰ� �Ǵ�
    }

    private void SetAgentStop(bool stop)
    {
        if (!agent.enabled) return;

        if (agent.isStopped != stop)
            agent.isStopped = stop;
    }

    private void SetAgentRotation(bool enable)
    {
        if (agent.updateRotation != enable)
            agent.updateRotation = enable;
    }

    // ������ �ޱ�
    public override void ApplyDamage(double damage)
    {
        enemyStat.currentHP -= damage;
        enemyStat.currentHP = Mathf.Max((float)enemyStat.currentHP, 0);

        //if (!isAttacking)
        //{
        //    animator.SetBool(_animIDHit, true);
        //}
        CheckDie();

        UpdateHPUI();
    }
}
