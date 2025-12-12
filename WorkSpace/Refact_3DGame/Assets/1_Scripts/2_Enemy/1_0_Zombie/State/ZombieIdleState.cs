using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieIdleState : EnemyState
{
    private float timer = 0f;
    private float waitTime = 3f;   // 3초 후 전이

    public ZombieIdleState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter() {
        Debug.Log("Zombie Idle State진입");
    }
    public override void Tick(float deltaTime) {
        timer += deltaTime;

        if (timer >= waitTime)
        {
            stateMachine.SwitchState(new ZombieMoveState(stateMachine));
        }
    }

    public override void Exit() {

    }


}
