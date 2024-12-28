using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//플레이어 또는 적의 상태 정보
public class BaseStateMachine : MonoBehaviour
{
    //공통 변수
    public Animator animator;

    //현재 상태
    public State currentState = null;


    //상태를 바꾸는 메소드
    public void SwitchState(State newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void Update()
    {
        currentState.Tick(Time.deltaTime);
    }

}
