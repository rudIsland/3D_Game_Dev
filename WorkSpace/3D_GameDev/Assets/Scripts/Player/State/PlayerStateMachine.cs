using UnityEngine;


/*
 * �÷��̾��� ���� ����
 */
public class PlayerStateMachine : BaseStateMachine
{
    public CharacterController characterController;
    public PlayerInputReader inputReader;

    [Header("�÷��̾� ������")]
    public float moveSpeed = 2.0f;
    public float sprintSpeed = 5.333f;
    public float rotateSpeed = 10.0f;
    public float animationDampTime = 0.2f;

    //����
    public float jumpHeight = 2.0f; // ���� ����
    public float jumpTimeout = 0.50f;
    public float fallTimeout = 0.15f;

    public float GroundedOffset = -0.14f; //������� offset
    public float GroundedRadius = 0.28f; //

    public float gravity = -9.81f; // �߷� ��
    public float verticalVelocity; // ���� ���� �ӵ�

    public bool inputJump = false; //�����Է�
    public bool Grounded = true; //���� �پ��ִ��� ����
    public LayerMask GroundLayers; //���̾��ũ

    // timeout deltatime
    private float _jumpTimeoutDelta { get; set; }
    private float _fallTimeoutDelta { get; set; }

    //�����
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    //�ִϸ��̼�
    public readonly int _animIDGrounded = Animator.StringToHash("Grounded"); //���� �ִ���
    public readonly int _animIDSpeed = Animator.StringToHash("Speed"); //�ӵ�
    public readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed"); //�����Ӽӵ�
    public readonly int _animIDJump = Animator.StringToHash("Jump"); //����
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
        // �Է� �̺�Ʈ ����
        if (inputReader != null)
        {
            inputReader.jumpPressed += OnJumpPressed;
        }
    }

    private void OnDisable()
    {
        // �Է� �̺�Ʈ ���� ����
        if (inputReader != null)
        {
            inputReader.jumpPressed -= OnJumpPressed;
        }
    }

    public void OnJumpPressed()
    {
        // ���� ���¿��� ���� ó��
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
