using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour, PlayerControls.IPlayerActions
{

    private PlayerControls controls;

    public Vector2 lookInput; //바라보기 방향 벡터
    public Vector2 moveInput; //움직일 방향 벡터 -1 or 1
    public bool isMove = false;
    public bool isJump = false;
    public event Action jumpPressed;
    public bool onSprint = false;
    public bool isAttack = false;
    public event Action TargetPressed;

    void Start()
    {
        // Input System 초기화
        controls = new PlayerControls();
        controls.Player.SetCallbacks(this);

        controls.Player.Enable();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            //Debug.Log("점프");
            jumpPressed?.Invoke();
            isJump = true;
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lookInput = context.ReadValue<Vector2>();
           // Debug.Log($"바라보기 입력: {lookInput}");
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            moveInput = context.ReadValue<Vector2>();
            isMove = true;
        }
        else if (context.canceled)
        {
            moveInput = Vector2.zero; // 입력 중지 시 초기화
            isMove = false;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if(context.performed && !isJump)
        {
            isAttack = true;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // 스프린트 활성화
            onSprint = true;
            Debug.Log("스프린트 시작");
        }
        else if (context.canceled)
        {
            // 스프린트 비활성화
            onSprint = false;
            Debug.Log("스프린트 종료");
        }
    }


    public void OnTarget(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TargetPressed?.Invoke();
        }
    }
}
