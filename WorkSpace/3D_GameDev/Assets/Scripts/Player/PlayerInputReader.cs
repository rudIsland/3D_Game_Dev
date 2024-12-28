using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour, PlayerControls.IPlayerActions
{

    private PlayerControls controls;

    public Vector2 lookInput; //바라보기 방향 벡터
    public Vector2 moveInput; //움직일 방향 벡터 -1 or 1
    public event Action jumpPressed;
    public bool onSprint = false;

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
        }
        else if (context.canceled)
        {
            moveInput = Vector2.zero; // 입력 중지 시 초기화
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onSprint = true;
        }
        else
        {
            onSprint = false;
        }
    }
}
