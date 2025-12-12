using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieDeadState : EnemyState
{
   

    public ZombieDeadState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Zombie Dead StateÁøÀÔ");

    }
    public override void Tick(float deltaTime)
    {
     
    }

    public override void Exit()
    {

    }
}
