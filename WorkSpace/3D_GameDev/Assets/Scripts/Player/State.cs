using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 * 가만히 있는 상태(Idle)
 * 공격중인 상태(Attacking)
 * 적을 목표로 하고 있는 상태(Targeting)
 * 죽은 상태(Dead)
 */
public abstract class State
{
    public abstract void Enter(); //진입 함수

    public abstract void Tick(float deltaTime); //갱신 함수

    public abstract void Exit(); //종료 함수

}
