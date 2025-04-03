using System.Collections.Generic;
using UnityEngine;

public class Zombie1 : Enemy
{

    public Animator animator;
    public readonly int _animIDFind = Animator.StringToHash("IsFind"); //탐지
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //공격
    public readonly int _animIDAttackRange = Animator.StringToHash("InAttackRange"); //공격

    //public bool isAttacking = false;


    protected override void SetupStats()
    {
        detectRange = 6f; //탐지범위
        attackRange = 1.2f; //공격범위
        moveSpeed = 1.5f; //이동속도
    }

    protected override void SetupTree()
    {
        // 탐지 트리
        ENode detectTree = new SelectorNode(new List<ENode>
        {
            new ActionNode(CheckDetectRange)
        });

        // 이동 트리
        ENode moveTree = new SelectorNode(new List<ENode>
        {
            new ActionNode(MoveToPlayer)
        });

        // 공격 트리
        ENode attackTree = new SequenceNode(new List<ENode>
        {
            new ActionNode(CheckAttackRange),
            new SelectorNode(new List<ENode>
            {
                new ActionNode(NormalAttack)
            })
        });

        // 전체 트리 구성
        behaviorTree = new SequenceNode(new List<ENode>
        {
            detectTree,
            moveTree,
            attackTree
        });
    }

    // ------------------ 행동 노드 ------------------

    //탐지
    private ESTATE CheckDetectRange()
    {
        if (enemyMemory.isPlayerDetected)
        {
            //Debug.Log("플레이어 탐지됨!");
            animator.SetBool(_animIDFind, true);
            return ESTATE.SUCCESS;
        }
        //Debug.Log("플레이어 감지 실패");
        animator.SetBool(_animIDFind, false);
        return ESTATE.FAILED;
    }


    //이동
    private ESTATE MoveToPlayer()
    {
        if (enemyMemory.isInAttackRange)
        {
            return ESTATE.SUCCESS;
        }
        else 
        {
            OutOfAttackRange();
        }

        //Target Setting
        Vector3 target = new Vector3(enemyMemory.player.position.x, transform.position.y, enemyMemory.player.position.z);
        float distanceToPlayer = Vector3.Distance(transform.position, target); //distance

        //Move but, if isAttacking is true.. not MoveTowards
        if (!isAttacking)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        }

        // Target to Rotate
        RotateTowardsTarget(target);

        // If in AttackRagne and FacingTargeting is Success
        if (distanceToPlayer <= attackRange && IsFacingTarget(target))
        {
            return ESTATE.SUCCESS; // 공격 가능
        }

        //Debug.Log("이동 중...");

        return ESTATE.RUN;
    }



    //공격
    private ESTATE CheckAttackRange() //if true -> AttackActionNode gogo
    {

        // 회전 완료 여부 체크
        Vector3 target = new Vector3(enemyMemory.player.position.x, transform.position.y, enemyMemory.player.position.z);
        if (!IsFacingTarget(target))
        {
            RotateTowardsTarget(target); // 공격 전까지 바라보도록 함
            return ESTATE.RUN; // 공격하지 않고 회전 유지
        }

        Debug.Log("공격 범위 안에 있음");
        animator.SetBool(_animIDAttackRange, true);
        return ESTATE.SUCCESS;
    }

    // 목표 방향으로 회전 (부드러운 회전)
    private void RotateTowardsTarget(Vector3 target)
    {
        if (!isAttacking)
        {
            Vector3 direction = (target - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }

    // 플레이어를 향하고 있는지 확인
    private bool IsFacingTarget(Vector3 target)
    {
        Vector3 directionToTarget = (target - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, directionToTarget);
        return dot > 0.95f; // 0.95 이상이면 거의 정면을 보고 있다고 판단
    }


    // 공격
    private ESTATE NormalAttack()
    {
        if (!enemyMemory.isInAttackRange)
        {
            Debug.Log("공격 범위 벗어남!");
            OutOfAttackRange();
            return ESTATE.FAILED;
        }

        if (!isAttacking)
        {
            isAttacking = true;
            NormalAttackingStart();
            Debug.Log("공격 시작!");
        }

        return ESTATE.SUCCESS;
    }


    //Animation Event Function
    private void NormalAttackingStart() //Animation Event and Code Function
    {
        isAttacking = true;
        animator.SetBool(_animIDAttack, isAttacking);
    }
    private void NormalAttackingEnd()
    {
        isAttacking = false;
        animator.SetBool(_animIDAttack, isAttacking);
    }

    private void OnWeapon()
    {
        weapon.gameObject.SetActive(true);
    }
    private void OffWeapon()
    {
        weapon.gameObject.SetActive(false);
    }

    private void OutOfAttackRange()
    {
        animator.SetBool(_animIDAttackRange, false);
    }

}
