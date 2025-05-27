using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie1 : Enemy
{

    public readonly int _animIDFind = Animator.StringToHash("IsFind"); //Ž��
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //����
    public readonly int _animIDHit = Animator.StringToHash("IsHit"); //�±�
    public readonly int _animIDAttackRange = Animator.StringToHash("InAttackRange"); //����
    public readonly int _animIDDead = Animator.StringToHash("IsDead"); //����

    public EnemyWeapon weapon;

    protected override void SetupStats()
    {
        detectRange = 15f; //Ž������
        attackRange = 1.0f; //���ݹ���
        moveSpeed = 2.3f; //�̵��ӵ�
        angularSpeed = 180f; //ȸ���ӵ�
        enemyStat.currentHP = enemyStat.maxHP; //���� ü�¼���

        //����
        level.SetLevel(3);
        deathEXP = 130;

        GetComponentInChildren<EnemyGUI>()?.UpdateLevel(); //GUI��������
    }

    protected override void Start()
    {
        base.Start();
        OnDeath += HandleDeath;

        weapon.gameObject.SetActive(false);
    }

    protected override void Update()
    {   
        if (enemyStat.IsDead) return;

        if (Player.Instance.playerStateMachine.currentState is PlayerDeadState)
        {
            ChangeDefaultMtl();
            return;
        }

        UpdateDistanceToPlayer();
        UpdateDetectionStatus();

        UpdateAnimatorStates();

        if (!enemyMemory.isPlayerDetected)
        {
            HandleUndetectedState();
            return;
        }

        HandleDetectedState();
        behaviorTree?.Evaluate();
    }

    protected override void SetupTree()
    {
        // Ž�� Ʈ��
        ENode detectTree = new SelectorNode(new List<ENode> {
            new ActionNode(CheckDetectRange)
        });

        // �̵� Ʈ��
        ENode moveTree = new SelectorNode(new List<ENode> {
            new ActionNode(MoveToPlayer)
        });

        // ���� Ʈ��
        ENode attackTree = new SequenceNode(new List<ENode> {
            new ActionNode(CheckAttackRange),
            new ActionNode(NormalAttack)
        });

        // ��ü Ʈ�� ����
        behaviorTree = new SequenceNode(new List<ENode> {
            detectTree, moveTree, attackTree
        });
    }

    // ------------------ �ൿ ��� ------------------

    //Ž��
    private ESTATE CheckDetectRange()
    {
        bool detected = enemyMemory.isPlayerDetected;

        // Ž�� �ִϸ��̼� ���� ������Ʈ
        animator.SetBool(_animIDFind, detected);

        return detected ? ESTATE.SUCCESS : ESTATE.FAILED;
    }


    //�̵�
    private ESTATE MoveToPlayer()
    {
        if (!enemyMemory.isPlayerDetected) //Ž������ -> �̵�����
        {
            SetAgentStop(true);
            return ESTATE.FAILED;
        }

        if (enemyMemory.isInAttackRange) //������
        {
            SetAgentStop(true);
            return ESTATE.SUCCESS;
        }

        animator.SetBool(_animIDAttackRange, false);

        if (!isAttacking) //�������� �ƴҶ� 
        {
            SetAgentStop(false);
            agent.SetDestination(enemyMemory.player.position);
        }

        return ESTATE.RUN;
    }

    //����
    private ESTATE CheckAttackRange()
    {
        bool inRange = enemyMemory.isInAttackRange;
        animator.SetBool(_animIDAttackRange, inRange);
        return inRange ? ESTATE.SUCCESS : ESTATE.FAILED;
    }



    // ����
    private ESTATE NormalAttack()
    {
        //���ݹ��� �ȿ� �ִ��� üũ
        if (!enemyMemory.isInAttackRange)
        {
            animator.SetBool(_animIDAttackRange, false);
            return ESTATE.FAILED;
        }

        // Ÿ�� �ٶ󺸰� �ִ��� üũ
        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking)
                RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        //����
        if (!isAttacking)
        {
            NormalAttackingStart();
        }

        return ESTATE.SUCCESS;
    }

    // ������ �ޱ�
    public override void ApplyDamage(double damage)
    {
        enemyStat.currentHP -= damage;
        enemyStat.currentHP = Mathf.Max((float)enemyStat.currentHP, 0);
        animator.SetBool(_animIDHit, true);

        CheckDie();

        UpdateHPUI();
    }


    // --- ��ƿ �Լ� ---

    private void UpdateAnimatorStates()
    {
        // Ž�� ���� �� �̵�/ȸ�� ���߰� �ִϸ��̼� ����
        if (!enemyMemory.isPlayerDetected)
        {
            if (!agent.isStopped)
                agent.isStopped = true;

            if (agent.updateRotation)
                agent.updateRotation = false;

            animator.SetBool(_animIDFind, false);
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
        if (!enemyMemory.isInAttackRange)
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

        agent.enabled = false; // ���� �����ϰ� ��Ȱ��ȭ
        GetComponent<Collider>().enabled = false;
        GetComponent<Target>().enabled = false;

        base.HandleDeath();

    }

    private void NormalAttackingStart()
    {
        isAttacking = true;
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
        }
        animator.SetBool(_animIDAttack, true);
    }

    private void NormalAttackingEnd()
    {
        if (enemyStat.IsDead) return;

        isAttacking = false;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        animator.SetBool(_animIDAttack, false);
    }

    private void OffHit()
    {
        isAttacking = false;
        animator.SetBool(_animIDAttack, isAttacking);
        animator.SetBool(_animIDHit, isAttacking);
    }

    public void OnWeapon()
    {
        weapon.gameObject.SetActive(true);
    }
    public void OffWeapon()
    {
        weapon.gameObject.SetActive(false);
    }
}
