using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie1 : Enemy
{

    public Animator animator;
    public readonly int _animIDFind = Animator.StringToHash("IsFind"); //탐지
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //공격
    public readonly int _animIDHit = Animator.StringToHash("IsHit"); //맞기
    public readonly int _animIDAttackRange = Animator.StringToHash("InAttackRange"); //공격

    protected override void SetupStats()
    {
        detectRange = 15f; //탐지범위
        attackRange = 1.0f; //공격범위
        moveSpeed = 2.3f; //이동속도
        angularSpeed = 180f; //회전속도
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
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
            new SelectorNode(new List<ENode>{
                new ActionNode(NormalAttack)
            })
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
        //not Find, not Move
        if (!enemyMemory.isPlayerDetected)
        {
            SetAgentStop(true);
            return ESTATE.FAILED;
        }

        if (enemyMemory.isInAttackRange)
        {
            SetAgentStop(true);
            return ESTATE.SUCCESS;
        }

        animator.SetBool(_animIDAttackRange, false);

        if (!isAttacking)
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
            RotateTowardsPlayer();
            return ESTATE.RUN; // 회전 중, 공격 X
        }
        
        //공격
        if (!isAttacking)
        {
            NormalAttackingStart();
        }

        return ESTATE.SUCCESS;
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
        SetAgentStop(false);

        if (enemyMemory.isInAttackRange && !isAttacking)
        {
            SetAgentRotation(false);
            RotateTowardsPlayer();
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
        return dot > 0.90f; // 0.90 이상이면 거의 정면을 보고 있다고 판단
    }


    /***************** In Animation Event Function *******************/
    private void NormalAttackingStart()
    {
        isAttacking = true;
        agent.isStopped = true; // agent Stop
        animator.SetBool(_animIDAttack, true);
    }

    private void NormalAttackingEnd()
    {
        isAttacking = false;
        agent.isStopped = false;
        animator.SetBool(_animIDAttack, false);
    }


    private void OnWeapon()
    {
        weapon.gameObject.SetActive(true);
    }
    private void OffWeapon()
    {
        weapon.gameObject.SetActive(false);
    }

    private void OffHit()
    {
        isAttacking = false;
        animator.SetBool(_animIDAttack, isAttacking);
        animator.SetBool(_animIDHit, isAttacking);
    }
}
