using UnityEngine;


/*
 * 플레이어의 현재 상태
 */
public class PlayerStateMachine : BaseStateMachine
{
    public CharacterController characterController;
    public PlayerInputReader inputReader;

    [Header("플레이어 움직임")]
    public float moveSpeed = 2.0f;
    public float sprintSpeed = 5.333f;
    public float rotateSpeed = 10.0f;
    public float animationDampTime = 0.2f;

    //점프
    public float jumpHeight = 2.0f; // 점프 높이
    public float jumpTimeout = 0.50f;
    public float fallTimeout = 0.15f;

    public float GroundedOffset = -0.14f; //지면까지 offset
    public float GroundedRadius = 0.28f; //

    public float gravity = -9.81f; // 중력 값
    public float verticalVelocity; // 현재 수직 속도

    public bool inputJump = false; //점프입력
    public bool Grounded = true; //땅에 붙어있는지 여부
    public LayerMask GroundLayers; //레이어마스크

    // timeout deltatime
    private float _jumpTimeoutDelta { get; set; }
    private float _fallTimeoutDelta { get; set; }

    //오디오
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    //애니메이션
    public readonly int _animIDGrounded = Animator.StringToHash("Grounded"); //땅에 있는지
    public readonly int _animIDSpeed = Animator.StringToHash("Speed"); //속도
    public readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed"); //움직임속도
    public readonly int _animIDJump = Animator.StringToHash("Jump"); //점프
    public readonly int _animIDFreeFall = Animator.StringToHash("FreeFall");

    private void Start()
    {
        SwitchState(new PlayerFreeLookState(this));

        // reset our timeouts on start
        _jumpTimeoutDelta = jumpTimeout;
        _fallTimeoutDelta = fallTimeout;
    }
    private void OnEnable()
    {
        // 입력 이벤트 구독
        if (inputReader != null)
        {
            inputReader.jumpPressed += OnJumpPressed;
        }
    }

    private void OnDisable()
    {
        // 입력 이벤트 구독 해제
        if (inputReader != null)
        {
            inputReader.jumpPressed -= OnJumpPressed;
        }
    }

    public void OnJumpPressed()
    {
        // 현재 상태에서 점프 처리
        if (currentState is PlayerBaseState baseState)
        {
            baseState.Jump();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y - GroundedOffset,
            transform.position.z
        );

        Gizmos.color = Grounded ? Color.green : Color.red;
        Gizmos.DrawSphere(spherePosition, GroundedRadius);
    }
}
