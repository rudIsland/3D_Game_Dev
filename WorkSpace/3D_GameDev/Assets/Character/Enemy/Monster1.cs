using System.Collections.Generic;
using UnityEngine;

public class Monster1 : Enemy
{
    protected override void SetupStats()
    {
        detectRange = 8f;
        attackRange = 1.8f;
        moveSpeed = 2.4f;
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
            Debug.Log("플레이어 탐지됨!");
            return ESTATE.SUCCESS;
        }
        Debug.Log("플레이어 감지 실패");
        return ESTATE.FAILED;
    }


    //이동
    private ESTATE MoveToPlayer()
    {
        if (enemyMemory.isInAttackRange)
            return ESTATE.SUCCESS;

        Vector3 target = new Vector3(enemyMemory.player.position.x, transform.position.y, enemyMemory.player.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        Debug.Log("이동 중...");
        return ESTATE.RUN;
    }


    //공격
    private ESTATE CheckAttackRange()
    {
        if (!enemyMemory.isInAttackRange)
        {
            Debug.Log("공격 거리 아님");
            return ESTATE.FAILED;
        }

        Debug.Log("공격 범위 안에 있음!");
        return ESTATE.SUCCESS;
    }

    private ESTATE NormalAttack()
    {
        if (!enemyMemory.isInAttackRange)
            return ESTATE.FAILED;

        Debug.Log("플레이어를 공격!");
        return ESTATE.SUCCESS;
    }
}
