using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieMoveState : EnemyState
{
    private float timer = 0f;
    private float waitTime = 3f;   // 3초 후 전이

    public ZombieMoveState(EnemyStateMachine stateMachine) : base(stateMachine)
    { }

    public override void Enter()
    {
        stateMachine.enemy.animator.SetBool("IsFind", true);
        Debug.Log("Zombie Move State진입");
    }
    public override void Tick(float deltaTime)
    {
        timer += deltaTime;

        if (timer >= waitTime)
        {
            stateMachine.SwitchState(new ZombieAttackState(stateMachine));
        }
    }
    public override void Exit() { }
}
