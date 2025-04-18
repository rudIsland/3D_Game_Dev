using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mutant : Enemy
{
    public Animator animator;
    public readonly int _animIDWalk = Animator.StringToHash("WalkRange"); //Ž��
    public readonly int _animIDRunning = Animator.StringToHash("RunningRange"); //Ž��
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //����
    public readonly int _animIDPunchRange = Animator.StringToHash("PunchRange"); //����
    public readonly int _animIDJumpAttack = Animator.StringToHash("JumpAttackRange"); //��������
    public readonly int _animIDSwipAttack = Animator.StringToHash("SwipRange"); //��������
    public readonly int _animIDHit = Animator.StringToHash("IsHit"); //�±�
    public readonly int _animIDDead = Animator.StringToHash("IsDead"); //����
    

    [Header("���ݹ���")]
    [SerializeField] private float JumpAttackRange = 8f;
    [SerializeField] private float PunchAttackRange = 1.0f;
    [SerializeField] private float SwipAttackRange = 1.5f;
    [SerializeField] private float walkSpeed = 1.8f;
    [SerializeField] private float runningSpeed = 2.3f;

    [Header("�������� ��Ÿ��")]
    [SerializeField] private float jumpAttackCooldown = 10f;
    private float jumpAttackTimer = 0f;

    private bool canJumpAttack = false;
    private float jumpRangeCheckTimer = 0f;
    private readonly float jumpRangeCheckInterval = 5f;

    [Header("Ž������")]
    [SerializeField] private float RunningArange = 10f;
    [SerializeField] private float WalkArange = 5f;

    [SerializeField] private bool isRunning = false;
    [SerializeField] private bool isWalk = false;

    [Header("Ž������")]
    public EnemyWeapon leftWeapon;
    public EnemyWeapon rightWeapon;

    protected override void SetupStats()
    {
        detectRange = 15f; //Ž������
        attackRange = 1.0f; //���ݹ���
        moveSpeed = runningSpeed; //�̵��ӵ�
        angularSpeed = 180f; //ȸ���ӵ�
        statComp.stats.currentHP = statComp.stats.maxHP; //���� ü�¼���
        level.SetLevel(3); //��������

        GetComponentInChildren<EnemyGUI>()?.UpdateLevel(); //GUI��������
    }

    protected override void Start()
    {
        base.Start();
        OnDeath += HandleDeath;
    }

    protected override void Update()
    {
        if (statComp.stats.IsDead) return;

        UpdateDistanceToPlayer();
        UpdateDetectionStatus();
        UpdateAnimatorStates();

        if (!isRunning && !isWalk)
        {
            HandleUndetectedState();
            return;
        }

        HandleDetectedState();
        behaviorTree?.Evaluate();
    }

    //Ž������ üũ
    protected override void UpdateDetectionStatus()
    {
        isRunning = enemyMemory.distanceToPlayer <= RunningArange && !GameManager.Instance.playerStateMachine.isDead;
        isWalk = enemyMemory.distanceToPlayer <= WalkArange && !GameManager.Instance.playerStateMachine.isDead;
    }


    protected override void SetupTree()
    {
        // Ž�� Ʈ��
        ENode detectTree = new SelectorNode(new List<ENode> {
            new ActionNode(CheckRunningRange),
            new ActionNode(CheckWalkRange)
        });

        // �̵� Ʈ��
        ENode moveTree = new SelectorNode(new List<ENode> {
            new ActionNode(MoveToPlayer)
        });

        // ���� Ʈ��
        ENode attackTree = new SequenceNode(new List<ENode> {
            new SelectorNode(new List<ENode>{
                new ActionNode(SwipAttack),
                new ActionNode(PunchAttack),
                //new ActionNode(JumpAttack)
            })
        });

        // ��ü Ʈ�� ����
        behaviorTree = new SequenceNode(new List<ENode> {
            detectTree, moveTree, attackTree
        });
    }

    // ------------------ �ൿ ��� ------------------

    //Ž��
    private ESTATE CheckWalkRange()
    {
        float dist = Vector3.Distance(transform.position, enemyMemory.player.position);
        bool isInRange = dist <= WalkArange;
        animator.SetBool(_animIDWalk, isInRange);
        return isInRange ? ESTATE.SUCCESS : ESTATE.FAILED;
    }

    private ESTATE CheckRunningRange()
    {
        float dist = Vector3.Distance(transform.position, enemyMemory.player.position);
        bool isInRange = dist <= RunningArange;
        animator.SetBool(_animIDRunning, isInRange);
        return isInRange ? ESTATE.SUCCESS : ESTATE.FAILED;
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

        if (IsInPunchRange() || IsInSwipRange())
        {
            Debug.Log("[�̵�] ���ݹ��� ���� �� �̵� ����");
            SetAgentStop(true);
            return ESTATE.SUCCESS;
        }

        if (!isAttacking)
        {
            Debug.Log("[�̵�] �÷��̾�� �̵� ��");
            SetAgentStop(false);
            agent.SetDestination(enemyMemory.player.position);
        }

        return ESTATE.RUN;
    }

    //����

    private bool IsInPunchRange()
    {
        return Vector3.Distance(transform.position, enemyMemory.player.position) <= PunchAttackRange;
    }

    private bool IsInJumpRange()
    {
        float dist = Vector3.Distance(transform.position, enemyMemory.player.position);
        return dist <= JumpAttackRange;
    }

    private bool IsInSwipRange()
    {
        float dist = Vector3.Distance(transform.position, enemyMemory.player.position);
        return dist <= SwipAttackRange; // ���ĵ� ���

    }


    // ����
    private ESTATE PunchAttack()
    {
        if (!IsInPunchRange()) 
            return ESTATE.FAILED;


        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            isAttacking = true;
            agent.isStopped = true;
            animator.SetBool(_animIDAttack, true);
            animator.SetBool(_animIDPunchRange, true);
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE JumpAttack()
    {
        if (jumpAttackTimer > 0f)
        {
            Debug.Log("[����] �������� �Ұ� (��Ÿ��)");
            return ESTATE.FAILED;
        }

        if (!IsInJumpRange())
        {
            Debug.Log("[����] �������� �Ÿ� �ƴ�");
            return ESTATE.FAILED;
        }

        if (!RollChance(50f))
        {
            Debug.Log("[����] �������� Ȯ�� ����");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[����] �������� ȸ�� ��");
            if (!isAttacking)
                RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[����] �������� ����!");
            isAttacking = true;
            agent.isStopped = true;
            jumpAttackTimer = jumpAttackCooldown;
            animator.SetBool(_animIDJumpAttack, true);
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE SwipAttack()
    {
        if (!IsInSwipRange()) return ESTATE.FAILED;

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            isAttacking = true;
            agent.isStopped = true;
            animator.SetBool(_animIDSwipAttack, true);
        }

        return ESTATE.SUCCESS;
    }




    /**********************************************************************************/


    // ������ �ޱ�
    public override void ApplyDamage(double damage)
    {
        stats.currentHP -= damage;
        stats.currentHP = Mathf.Max((float)stats.currentHP, 0);
        animator.SetBool(_animIDHit, true);

        CheckDie();

        statComp.UpdateHPUI();
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
        if ((IsInPunchRange() || IsInSwipRange() || canJumpAttack) && !isAttacking)
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
    }

    private void NormalAttackingStart()
    {
        isAttacking = true;
        agent.isStopped = true;
        SetAgentRotation(false); // �� ȸ���� ����
        animator.SetBool(_animIDAttack, true);
    }

    private void NormalAttackingEnd()
    {
        if (statComp.stats.IsDead) return;

        isAttacking = false;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            SetAgentRotation(true); // �� ȸ�� �ٽ� ���
        }

        animator.SetBool(_animIDAttack, false);
    }


    private void OffHit()
    {
        isAttacking = false;
        animator.SetBool(_animIDAttack, isAttacking);
        animator.SetBool(_animIDHit, isAttacking);
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
        return Random.Range(0f, 100f) <= percent;
    }
}
