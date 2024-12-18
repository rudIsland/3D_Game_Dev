using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

/*
 * 플레이어의 현재 상태
 */
public class PlayerStateMachine : BaseStateMachine
{
    public CharacterController characterController;
    public Rigidbody rigid;

    [Header("플레이어 움직임")]
    public VariableJoystick joystick;
    public float moveSpeed = 5f;
    public float rotateSpeed = 0.2f;

    

    
   

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
      
    }

   
    private void Start()
    {
        SwitchState(new PlayerFreeLookState(this));
    }


}
