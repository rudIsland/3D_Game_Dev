using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mutant : Enemy
{
    private enum AttackType
    {
        Jump,
        Swip,
        Punch
    }

    public readonly int _animIDIdle = Animator.StringToHash("Idle"); //���ֱ�
    public readonly int _animIDHit = Animator.StringToHash("IsHit"); //Ž��
    public readonly int _animIDWalk = Animator.StringToHash("WalkRange"); //Ž��
    public readonly int _animIDRunning = Animator.StringToHash("RunningRange"); //Ž��
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //����
    public readonly int _animIDAttackIndex = Animator.StringToHash("AttackIndex"); //���ݹ�ȣ
    public readonly int _animIDDead = Animator.StringToHash("IsDead"); //����
    public readonly int _animIDAttackTrigger = Animator.StringToHash("AttackTrigger"); //���� Ʈ����

    private const int ATTACK_JUMP = 1;
    private const int ATTACK_PUNCH = 2;
    private const int ATTACK_SWIP = 3;

    [Header("���ݹ���")]
    [SerializeField] private float JumpAttackRange = 3f;
    [SerializeField] private float PunchAttackRange = 1.0f;
    [SerializeField] private float SwipAttackRange = 2f;
    [SerializeField] private bool isJumpAttackRange = false;
    [SerializeField] private bool isPunchAttackRange = false;
    [SerializeField] private bool isSwipAttackRange = false;

    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runningSpeed = 3f;

    [Header("���� Ȯ��")]
    [SerializeField] private float jumpAttackChance = 0.5f;  // 50% Ȯ��
    [SerializeField] private float SwipAttackChance = 0.6f; // 60% Ȯ��

    [Header("�������� ��Ÿ��")]
    [SerializeField] private float jumpAttackCooldown = 5f;
    private float jumpAttackTimer = 0f;

    private bool isJumpAttacking = false;

    [Header("Ž������")]
    [SerializeField] private float RunningArange = 10f;
    [SerializeField] private float WalkArange = 5f;

    [SerializeField] private bool isRunning = false;
    [SerializeField] private bool isWalk = false;

    [Header("����")]
    public EnemyWeapon leftWeapon;
    public EnemyWeapon rightWeapon;

    protected override void SetupStats()
    {
        detectRange = 10f; //Ž������
        attackRange = 1.0f; //���ݹ���
        moveSpeed = runningSpeed; //�̵��ӵ�
        angularSpeed = 180f; //ȸ���ӵ�

        level.SetLevel(3); //��������

        enemyStat.maxHP = 500;
        enemyStat.ATK = 25f;
        enemyStat.DEF = 15f;
        enemyStat.currentHP = enemyStat.maxHP; //���� ü�¼���
        deathEXP = 1500;

        GetComponentInChildren<EnemyGUI>()?.UpdateLevel(); //GUI��������
    }

    protected override void Start()
    {
        base.Start();
        OnDeath += HandleDeath;
        leftWeapon.gameObject.SetActive(false);
        rightWeapon.gameObject.SetActive(false);
    }

    protected override void Update()
    {
        if (enemyStat.IsDead) return;
        if (Player.Instance.playerStateMachine.currentState is PlayerDeadState)
        {
            ChangeDefaultMtl();
            return;
        }

        JumpAttackCoolTime();

        UpdateDistanceToPlayer(); //�÷��̾���� �Ÿ� ���
        UpdateDetectionStatus(); //�� Ž������ ���
        UpdateAnimatorStates(); //�ִϸ��̼� ���


        if (!isRunning && !isWalk) //�Ȱų� �ٴ� ���°� �ƴҰ��
        {
            HandleUndetectedState(); //agent�� ���� false
            return;
        }

        HandleDetectedState();
        behaviorTree?.Evaluate();
    }

    private void JumpAttackCoolTime()
    {
        jumpAttackTimer -= Time.deltaTime; //�ð� �帧��� �پ�鵵�� ����
    }


    //Ž������ üũ
    protected override void UpdateDetectionStatus()
    {
       // if (!GameManager.Instance.player.isDead) return; 
        isRunning = enemyMemory.distanceToPlayer <= RunningArange;
        isWalk = enemyMemory.distanceToPlayer <= WalkArange;
        isJumpAttackRange = enemyMemory.distanceToPlayer <= JumpAttackRange;
        isPunchAttackRange = enemyMemory.distanceToPlayer <= PunchAttackRange;
        isSwipAttackRange = enemyMemory.distanceToPlayer <= SwipAttackRange;

        if (isTarget) return; //Ÿ���϶� �ٲ����ʰ�

        // ���� ����� ��츸 ���͸��� �ٲ�
        if (!enemyMemory.isPlayerDetected)
        {
            ChangeDefaultMtl();
        }
    }


    protected override void SetupTree()
    {
        // Ž�� Ʈ��
        ENode detectTree = new SelectorNode(new List<ENode> {
            new ActionNode(CheckRunningRange),
            new ActionNode(CheckWalkRange)
        });

        ENode attackAndMoveTree = new SelectorNode(new List<ENode> {
            new ActionNode(SelectAndPerformAttack),
            new ActionNode(MoveToPlayer)
        });

        // ��ü Ʈ�� ����
        behaviorTree = new SequenceNode(new List<ENode> {
            detectTree,
            attackAndMoveTree
        });
    }


    // ------------------ �ൿ ��� ------------------

    //Ž��
    private ESTATE CheckWalkRange()
    {
        animator.SetBool(_animIDWalk, isWalk);
        return isWalk ? ESTATE.SUCCESS : ESTATE.FAILED;
    }

    private ESTATE CheckRunningRange()
    {
        animator.SetBool(_animIDRunning, isRunning);
        return isRunning ? ESTATE.SUCCESS : ESTATE.FAILED;
    }


    //�̵�
    private ESTATE MoveToPlayer()
    {
        if (!isRunning && !isWalk)
        {
            Debug.Log("[�̵�] Ž�� ���� �� �̵� ����");
            SetAgentStop(true);
            return ESTATE.FAILED;
        }

        // ���� �߿� �̵����� ����
        if (isAttacking)
        {
            Debug.Log("[�̵�] ���� �� �� �̵� ����");
            ChangeDetectedMtl();
            SetAgentStop(true);
            return ESTATE.SUCCESS;
        }

        // ���� ���� �� �̵�
        Debug.Log("[�̵�] �÷��̾�� �̵� ��");

        ChangeDetectedMtl();
        SetAgentStop(false);
        agent.SetDestination(enemyMemory.player.position);

        return ESTATE.RUN;
    }


    private ESTATE SelectAndPerformAttack()
    {
        float jumpWeight = 0f;
        float swipWeight = 1f;
        float punchWeight = 2f;

        // ���� ���� ���� + �Ÿ� ��� ����ġ ����
        if (isJumpAttackRange && jumpAttackTimer <= 0f)
        {
            float distance = enemyMemory.distanceToPlayer;

            if (distance <= 1.5f)
                jumpWeight = 1f;
            else if (distance <= 2.5f)
                jumpWeight = 5f;
            else
                jumpWeight = 15f;
        }

        if (isSwipAttackRange)
            swipWeight += 10f;

        if (isPunchAttackRange)
            punchWeight += 10f;

        float totalWeight = jumpWeight + swipWeight + punchWeight;
        if (totalWeight <= 0f) return ESTATE.FAILED;

        float roll = Random.Range(0f, totalWeight);

        if (roll <= jumpWeight)
            return JumpAttack();

        roll -= jumpWeight;
        if (roll <= swipWeight)
            return PunchAttack();

        return SwipAttack();
    }

    // ����
    private ESTATE PunchAttack()
    {
        if (!isPunchAttackRange)
        {
            Debug.Log("[����] ��ġ ���� ����: ���� �ƴ�");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[����] ��ġ ���� ȸ�� ��...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[����] ��ġ ���� ����");
            NormalAttackingStart(); //���� ���°ǵ� �߰��غ� �ʹ� �ǰ��� ������

            animator.SetInteger(_animIDAttackIndex, ATTACK_PUNCH);
            animator.SetTrigger(_animIDAttackTrigger);
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE JumpAttack()
    {
        if (!isJumpAttackRange || jumpAttackTimer > 0f || !RollChance(jumpAttackChance))
            return ESTATE.FAILED;

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[����] ���� ���� ȸ�� ��...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            isJumpAttacking = true;
            jumpAttackTimer = jumpAttackCooldown;

            NormalAttackingStart(); //���� ���°ǵ� �߰��غ� �ʹ� �ǰ��� ������
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;

            animator.SetInteger(_animIDAttackIndex, ATTACK_JUMP);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE SwipAttack()
    {
        if (!isSwipAttackRange)
        {
            Debug.Log("[����] ���� ���� ����: ���� �ƴ�");
            return ESTATE.FAILED;
        }

        if (!RollChance(SwipAttackChance))
        {
            Debug.Log("[����] ���� ���� ����: Ȯ�� ����");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[����] ���� ���� ȸ�� ��...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[����] ���� ���� ����");
            NormalAttackingStart(); //���� ���°ǵ� �߰��غ� �ʹ� �ǰ��� ������

            animator.SetInteger(_animIDAttackIndex, ATTACK_SWIP);
            animator.SetTrigger(_animIDAttackTrigger);
        }

        return ESTATE.SUCCESS;
    }




    /**********************************************************************************/


    // ������ �ޱ�
    public override void ApplyDamage(double damage)
    {
        enemyStat.currentHP -= damage;
        enemyStat.currentHP = Mathf.Max((float)enemyStat.currentHP, 0);

        if (!isAttacking)
        {
            animator.SetBool(_animIDHit,true);
        }

        CheckDie();

        UpdateHPUI();
    }


    // --- ��ƿ �Լ� ---

    private void UpdateAnimatorStates()
    {
        // Ž�� ���� �� ��� ����
        if (!isWalk && !isRunning)
        {
            SetAgentStop(true);
            SetAgentRotation(false);

            animator.SetBool(_animIDWalk, false);
            animator.SetBool(_animIDRunning, false);
            return;
        }

        // �ȱ� ���� �� �ȱ⸸ true
        if (isWalk)
        {
            animator.SetBool(_animIDWalk, true);
            animator.SetBool(_animIDRunning, false);
            agent.speed = walkSpeed;
            return;
        }

        // �޸��� ���� �� �޸��⸸ true
        if (isRunning)
        {
            animator.SetBool(_animIDRunning, true);
            animator.SetBool(_animIDWalk, false);
            agent.speed = runningSpeed;
            return;
        }
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

    private void HandleUndetectedState()
    {
        SetAgentStop(true);
        SetAgentRotation(false);
    }

    private void HandleDetectedState()
    {
        if ((isPunchAttackRange || isSwipAttackRange || jumpAttackTimer <= 0f) && !isAttacking)
        {
            SetAgentRotation(false);
            RotateTowardsPlayer();
        }
        else if (isAttacking)
        {
            SetAgentRotation(false);
        }
        else
        {
            SetAgentRotation(true);
        }
    }


    // Check Facing to Target(Player)
    private bool IsFacingTarget(Vector3 target)
    {
        Vector3 directionToTarget = (target - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, directionToTarget);
        return dot > 0.90f; // 0.90 �̻��̸� ���� ������ ���� �ִٰ� �Ǵ�
    }


    /***************** Animation Event Function *******************/

    protected override void HandleDeath()
    {
        Debug.Log("�� �״���...");
        animator.applyRootMotion = true;
        animator.SetTrigger(_animIDDead);

        //NavMeshAgent�� ��Ȱ��ȭ�ϱ� ���� ���� ���� ����
        SetAgentStop(true);
        agent.speed = moveSpeed; // �� �ӵ� ���� (����)

        agent.enabled = false; // ���� �����ϰ� ��Ȱ��ȭ
        GetComponent<Collider>().enabled = false;
        GetComponent<Target>().enabled = false;

        base.HandleDeath();
    }

    private void NormalAttackingStart()
    {
        isAttacking = true;
        animator.applyRootMotion = true;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        animator.SetBool(_animIDAttack, true);
    }

    private void NormalAttackingEnd()
    {
        if (enemyStat.IsDead) return;

        isAttacking = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;

            if (isJumpAttacking)
            {
                agent.updatePosition = true;
                agent.updateRotation = true;
                isJumpAttacking = false;
            }
        }

        animator.ResetTrigger(_animIDAttackTrigger);
        animator.SetInteger(_animIDAttackIndex, 0);
        animator.SetBool(_animIDAttack, false);
    }


    private void OffHit()
    {
        isAttacking = false;
        animator.SetBool(_animIDHit, false);
        animator.SetBool(_animIDAttack, isAttacking);
    }

    public void OnLeftWeapon()
    {
        leftWeapon.gameObject.SetActive(true);
    }
    public void OnRightWeapon()
    {
        rightWeapon.gameObject.SetActive(true);
    }
    public void OffWeapon()
    {
        leftWeapon.gameObject.SetActive(false);
        rightWeapon.gameObject.SetActive(false);
    }

    private bool RollChance(float percent)
    {
        return Random.value <= percent;
    }
}
