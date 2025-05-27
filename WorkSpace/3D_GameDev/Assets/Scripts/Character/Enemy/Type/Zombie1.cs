using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie1 : Enemy
{

    public readonly int _animIDFind = Animator.StringToHash("IsFind"); //탐지
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //공격
    public readonly int _animIDHit = Animator.StringToHash("IsHit"); //맞기
    public readonly int _animIDAttackRange = Animator.StringToHash("InAttackRange"); //공격
    public readonly int _animIDDead = Animator.StringToHash("IsDead"); //죽음

    public EnemyWeapon weapon;

    protected override void SetupStats()
    {
        detectRange = 15f; //탐지범위
        attackRange = 1.0f; //공격범위
        moveSpeed = 2.3f; //이동속도
        angularSpeed = 180f; //회전속도
        enemyStat.currentHP = enemyStat.maxHP; //현재 체력설정

        //레벨
        level.SetLevel(3);
        deathEXP = 130;

        GetComponentInChildren<EnemyGUI>()?.UpdateLevel(); //GUI레벨설정
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
        // 탐지 트리
        ENode detectTree = new SelectorNode(new List<ENode> {
            new ActionNode(CheckDetectRange)
        });

        // 이동 트리
        ENode moveTree = new SelectorNode(new List<ENode> {
            new ActionNode(MoveToPlayer)
        });

        // 공격 트리
        ENode attackTree = new SequenceNode(new List<ENode> {
            new ActionNode(CheckAttackRange),
            new ActionNode(NormalAttack)
        });

        // 전체 트리 구성
        behaviorTree = new SequenceNode(new List<ENode> {
            detectTree, moveTree, attackTree
        });
    }

    // ------------------ 행동 노드 ------------------

    //탐지
    private ESTATE CheckDetectRange()
    {
        bool detected = enemyMemory.isPlayerDetected;

        // 탐지 애니메이션 상태 업데이트
        animator.SetBool(_animIDFind, detected);

        return detected ? ESTATE.SUCCESS : ESTATE.FAILED;
    }


    //이동
    private ESTATE MoveToPlayer()
    {
        if (!enemyMemory.isPlayerDetected) //탐지실패 -> 이동정지
        {
            SetAgentStop(true);
            return ESTATE.FAILED;
        }

        if (enemyMemory.isInAttackRange) //공격중
        {
            SetAgentStop(true);
            return ESTATE.SUCCESS;
        }

        animator.SetBool(_animIDAttackRange, false);

        if (!isAttacking) //공격중이 아닐때 
        {
            SetAgentStop(false);
            agent.SetDestination(enemyMemory.player.position);
        }

        return ESTATE.RUN;
    }

    //공격
    private ESTATE CheckAttackRange()
    {
        bool inRange = enemyMemory.isInAttackRange;
        animator.SetBool(_animIDAttackRange, inRange);
        return inRange ? ESTATE.SUCCESS : ESTATE.FAILED;
    }



    // 공격
    private ESTATE NormalAttack()
    {
        //공격범위 안에 있는지 체크
        if (!enemyMemory.isInAttackRange)
        {
            animator.SetBool(_animIDAttackRange, false);
            return ESTATE.FAILED;
        }

        // 타겟 바라보고 있는지 체크
        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking)
                RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        //공격
        if (!isAttacking)
        {
            NormalAttackingStart();
        }

        return ESTATE.SUCCESS;
    }

    // 데미지 받기
    public override void ApplyDamage(double damage)
    {
        enemyStat.currentHP -= damage;
        enemyStat.currentHP = Mathf.Max((float)enemyStat.currentHP, 0);
        animator.SetBool(_animIDHit, true);

        CheckDie();

        UpdateHPUI();
    }


    // --- 유틸 함수 ---

    private void UpdateAnimatorStates()
    {
        // 탐지 실패 시 이동/회전 멈추고 애니메이션 꺼줌
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
            // 공격 범위 밖: 이동/회전 가능
            SetAgentStop(false);
            SetAgentRotation(true);
        }
        else if (!isAttacking)
        {
            // 공격 범위 안, 공격 전: 회전만 허용
            SetAgentStop(true);
            SetAgentRotation(false);
            RotateTowardsPlayer();
        }
        else
        {
            // 공격 중: 이동, 회전 금지
            SetAgentStop(true);
            SetAgentRotation(false);
        }
    }


    // Check Facing to Target(Player)
    private bool IsFacingTarget(Vector3 target)
    {
        Vector3 directionToTarget = (target - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, directionToTarget);
        return dot > 0.90f; // 0.90 이상이면 거의 정면을 보고 있다고 판단
    }


    /***************** Animation Event Function *******************/

    protected override void HandleDeath()
    {
        Debug.Log("적 죽는중...");
        animator.applyRootMotion = true;
        animator.SetTrigger(_animIDDead);

        //NavMeshAgent를 비활성화하기 전에 정지 먼저 설정
        SetAgentStop(true);

        agent.enabled = false; // 이제 안전하게 비활성화
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
