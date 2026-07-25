using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("필수 연결")]
    [SerializeField] private PlayerInputHandler playerInput;
    [FormerlySerializedAs("cameraRoot")]
    [SerializeField] private Transform viewCamera;
    [SerializeField] private Animator playerAnimator;

    [Header("이동")]
    [SerializeField] private float walkSpeed = 2.8f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float speedUp = 14f;
    [SerializeField] private float slowDown = 18f;
    [SerializeField] private float turnSmoothTime = 0.08f;

    [Header("점프")]
    [SerializeField] private float jumpHeight = 1.6f;
    [SerializeField] private float gravity = -22f;
    [SerializeField] private float groundedPull = -2f;
    [SerializeField] private float jumpInputWait = 0.15f;
    [SerializeField] private float groundLeaveWait = 0.12f;

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int MotionSpeedId = Animator.StringToHash("MotionSpeed");
    private static readonly int GroundedId = Animator.StringToHash("Grounded");
    private static readonly int JumpId = Animator.StringToHash("Jump");
    private static readonly int FreeFallId = Animator.StringToHash("FreeFall");

    private CharacterController characterController;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float turnVelocity;
    private float jumpInputTimer;
    private float groundLeaveTimer;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInputHandler>();
        }
    }

    private void Update()
    {
        SaveJumpInput();

        bool isGrounded = characterController.isGrounded;
        UpdateGroundWait(isGrounded);
        StartJumpIfPossible();
        ApplyGravity(isGrounded);

        Vector3 wantedDirection = GetCameraMoveDirection();
        UpdateHorizontalVelocity(wantedDirection);
        TurnToMoveDirection(wantedDirection);

        Vector3 fullVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
        characterController.Move(fullVelocity * Time.deltaTime);

        UpdateAnimator(isGrounded);
    }

    private void SaveJumpInput()
    {
        if (playerInput != null && playerInput.TakeJumpInput())
        {
            jumpInputTimer = jumpInputWait;
        }
        else
        {
            jumpInputTimer -= Time.deltaTime;
        }
    }

    private void UpdateGroundWait(bool isGrounded)
    {
        if (isGrounded)
        {
            groundLeaveTimer = groundLeaveWait;
            return;
        }

        groundLeaveTimer -= Time.deltaTime;
    }

    private void StartJumpIfPossible()
    {
        if (jumpInputTimer <= 0f || groundLeaveTimer <= 0f)
        {
            return;
        }

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        jumpInputTimer = 0f;
        groundLeaveTimer = 0f;
    }

    private void ApplyGravity(bool isGrounded)
    {
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedPull;
            return;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private Vector3 GetCameraMoveDirection()
    {
        if (playerInput == null)
        {
            return Vector3.zero;
        }

        Vector2 moveInput = Vector2.ClampMagnitude(playerInput.MoveInput, 1f);
        if (moveInput.sqrMagnitude < 0.01f)
        {
            return Vector3.zero;
        }

        Transform view = viewCamera != null ? viewCamera : Camera.main?.transform;
        if (view == null)
        {
            return new Vector3(moveInput.x, 0f, moveInput.y);
        }

        Vector3 forward = view.forward;
        Vector3 right = view.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (forward * moveInput.y + right * moveInput.x).normalized;
    }

    private void UpdateHorizontalVelocity(Vector3 wantedDirection)
    {
        float wantedSpeed = 0f;
        if (wantedDirection.sqrMagnitude > 0.01f)
        {
            wantedSpeed = playerInput != null && playerInput.IsRunning ? runSpeed : walkSpeed;
        }

        Vector3 wantedVelocity = wantedDirection * wantedSpeed;
        float changeSpeed = wantedSpeed > horizontalVelocity.magnitude ? speedUp : slowDown;
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            wantedVelocity,
            changeSpeed * Time.deltaTime);
    }

    private void TurnToMoveDirection(Vector3 wantedDirection)
    {
        if (wantedDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        float wantedAngle = Mathf.Atan2(wantedDirection.x, wantedDirection.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            wantedAngle,
            ref turnVelocity,
            turnSmoothTime);

        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
    }

    private void UpdateAnimator(bool isGrounded)
    {
        if (playerAnimator == null)
        {
            return;
        }

        float moveAmount = horizontalVelocity.magnitude;
        playerAnimator.SetFloat(SpeedId, moveAmount, 0.08f, Time.deltaTime);
        playerAnimator.SetFloat(MotionSpeedId, moveAmount > 0.05f ? 1f : 0f);
        playerAnimator.SetBool(GroundedId, isGrounded);
        playerAnimator.SetBool(JumpId, !isGrounded && verticalVelocity > 0.1f);
        playerAnimator.SetBool(FreeFallId, !isGrounded && verticalVelocity < -0.1f);
    }
}
