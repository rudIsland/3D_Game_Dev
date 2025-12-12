using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieAttackState : EnemyState
{
    public ZombieAttackState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.enemy.animator.SetTrigger("IsAttack");
        Debug.Log("Zombie Attack State¡¯¿‘");
    }
    public override void Tick(float deltaTime)
    {

    }
    public override void Exit() { }

}
