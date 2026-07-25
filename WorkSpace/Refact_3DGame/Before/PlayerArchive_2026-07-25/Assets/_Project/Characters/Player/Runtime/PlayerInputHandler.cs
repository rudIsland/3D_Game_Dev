using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset playerInputActions;

    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction runAction;
    private bool hasJumpInput;

    public Vector2 MoveInput => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public Vector2 LookInput => lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public bool IsRunning => runAction?.IsPressed() ?? false;

    private void Awake()
    {
        if (playerInputActions == null)
        {
            Debug.LogError("PlayerInputHandler에 입력 액션이 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        playerActionMap = playerInputActions.FindActionMap("Player", true);
        moveAction = playerActionMap.FindAction("Move", true);
        lookAction = playerActionMap.FindAction("Look", true);
        jumpAction = playerActionMap.FindAction("Jump", true);
        runAction = playerActionMap.FindAction("Sprint", true);
    }

    private void OnEnable()
    {
        if (playerActionMap == null)
        {
            return;
        }

        jumpAction.started += SaveJumpInput;
        playerActionMap.Enable();
    }

    private void OnDisable()
    {
        if (playerActionMap == null)
        {
            return;
        }

        jumpAction.started -= SaveJumpInput;
        playerActionMap.Disable();
        hasJumpInput = false;
    }

    public bool TakeJumpInput()
    {
        if (!hasJumpInput)
        {
            return false;
        }

        hasJumpInput = false;
        return true;
    }

    private void SaveJumpInput(InputAction.CallbackContext context)
    {
        hasJumpInput = true;
    }
}
