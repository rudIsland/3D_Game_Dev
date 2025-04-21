using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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

    [Header("Ž������")]
    private float DectectedRange = 7f;
    private float FightDectectedRange = 4.5f;
    private float FightRange = 2f;
    [SerializeField] private bool isDecteced;

    // ���� ���� Ÿ�̸� & ����
    private float jumpAttackTimer = 3f;
    private float FlyKickRange = 1.5f;
    private float JapCrossRange = 1.15f;
    private float KickRange = 1.3f;
    private float HookRange = 1.2f;
    private float JapRange = 1.0f;
    private float LowKickRange = 1.0f;
    private float StepForwardRange = 1.5f;

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
        OnDeath += HandleDeath; //���� ��������Ʈ
        LeftHandWeapon.gameObject.SetActive(false);
        RightHandWeapon.gameObject.SetActive(false);
        LeftFootWeapon.gameObject.SetActive(false);
        RightFootWeapon.gameObject.SetActive(false);
    }

    // Update is called once per frame
    protected override void Update()
    {
        if(stats.IsDead) return; //������ ����

        UpdateDistanceToPlayer(); //�÷��̾���� �Ÿ� ���


        behaviorTree?.Evaluate();
    }


    protected override void SetupStats()
    {
        detectRange = DectectedRange; //Ž������
        attackRange = 1.0f; //���ݹ���
        moveSpeed = 1.5f; //�̵��ӵ�
        angularSpeed = 180f; //ȸ���ӵ�

        level.SetLevel(20); //��������

        stats.maxHP = 1000;
        stats.ATK = 40f;
        stats.DEF = 30f;
        statComp.stats.currentHP = statComp.stats.maxHP; //���� ü�¼���

        GetComponentInChildren<EnemyGUI>()?.UpdateLevel(); //GUI��������
    }
    /****************************** Ʈ�����  ************************************/
    protected override void SetupTree()
    {
        // Ž�� Ʈ��
        ENode detectTree = new SelectorNode(new List<ENode> {
            new ActionNode(DectectedPlayer),
        });

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
        if (GameManager.Instance.playerStateMachine.isDead) return ESTATE.FAILED;

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
            return ESTATE.SUCCESS;
        }

        return ESTATE.FAILED;

    }

    private ESTATE PlayAttack(int attackIndex)
    {
        if (isAttacking) return ESTATE.SUCCESS;

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        isAttacking = true;

        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        if (attackIndex == ATTACK_FLYKICK)
            jumpAttackTimer = 3f;

        animator.SetInteger(_animIDAttackIndex, attackIndex);
        animator.SetTrigger(_animIDAttackTrigger); // ����: AnyState �� StepForward �� ���� ���µ�� �Ѿ

        return ESTATE.SUCCESS;
    }


    private ESTATE FightSelector()
    {
        float flyKickWeight = 0f;
        float japCrossWeight = 1f;
        float hookWeight = 1f;
        float kickWeight = 1f;
        float japWeight = 1f;
        float lowKickWeight = 1f;

        float distance = enemyMemory.distanceToPlayer;

        if (distance <= JapRange)
            japWeight += 10f;

        if (distance <= LowKickRange)
            lowKickWeight += 10f;

        if (distance <= HookRange)
            hookWeight += 10f;

        if (distance <= KickRange)
            kickWeight += 10f;

        if (distance <= JapCrossRange)
            japCrossWeight += 10f;

        if (distance <= FlyKickRange && jumpAttackTimer <= 0f)
            flyKickWeight = 15f;

        float totalWeight = flyKickWeight + japCrossWeight + hookWeight + kickWeight + japWeight + lowKickWeight;
        if (totalWeight <= 0f) return ESTATE.FAILED;

        float roll = Random.Range(0f, totalWeight);

        if (roll <= flyKickWeight)
            return PlayAttack(ATTACK_FLYKICK);

        roll -= flyKickWeight;
        if (roll <= japCrossWeight)
            return PlayAttack(ATTACK_JABCROSS);

        roll -= japCrossWeight;
        if (roll <= hookWeight)
            return PlayAttack(ATTACK_HOOK);

        roll -= hookWeight;
        if (roll <= kickWeight)
            return PlayAttack(ATTACK_KICK);

        roll -= kickWeight;
        if (roll <= lowKickWeight)
            return PlayAttack(ATTACK_LOWKICK);

        return PlayAttack(ATTACK_JAP);
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
        agent.SetDestination(enemyMemory.player.position);

        return ESTATE.RUN;
    }

    //����

    private ESTATE JapCrossAttack()
    {
        // ���� ���� �˻� �� �ִϸ��̼� ���
        if (! (JapCrossRange <= enemyMemory.distanceToPlayer))
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

            //animator.SetInteger(_animIDAttackIndex, ATTACK_PUNCH);
            //animator.SetTrigger(_animIDAttackTrigger);
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE KickAttack()
    {
        // ���� ���� �˻� �� �ִϸ��̼� ���
        if (!(KickRange <= enemyMemory.distanceToPlayer))
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

            //animator.SetInteger(_animIDAttackIndex, ATTACK_PUNCH);
            //animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE FlyKickAttack()
    {
        // ���� ���� �˻� �� �ִϸ��̼� ���
        if (!(FlyKickRange <= enemyMemory.distanceToPlayer))
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

            //animator.SetInteger(_animIDAttackIndex, ATTACK_PUNCH);
            //animator.SetTrigger(_animIDAttackTrigger);
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
        if (statComp.stats.IsDead) return;

        isAttacking = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            animator.ResetTrigger(_animIDAttackTrigger);
            animator.SetInteger(_animIDAttackIndex, 0);
        }

    }

    /****************************** Agent And �˻� Logic  ************************************/

    private bool IsFacingTarget(Vector3 target)
    {
        Vector3 directionToTarget = (target - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, directionToTarget);
        return dot > 0.90f; // 0.90 �̻��̸� ���� ������ ���� �ִٰ� �Ǵ�
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
        stats.currentHP -= damage;
        stats.currentHP = Mathf.Max((float)stats.currentHP, 0);

        //if (!isAttacking)
        //{
        //    animator.SetBool(_animIDHit, true);
        //}
        CheckDie();

        statComp.UpdateHPUI();
    }
}
