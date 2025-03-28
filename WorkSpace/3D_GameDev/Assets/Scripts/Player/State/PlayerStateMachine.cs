using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


/*
 * �÷��̾��� ���� ����
 */
public class PlayerStateMachine : BaseStateMachine
{
    public CharacterController characterController;
    public PlayerInputReader inputReader;

    [Header("�÷��̾� ������")]
    public float moveSpeed = 2.0f; //�⺻ ������ �ӵ�
    public float sprintSpeed = 5.333f; //�޸��� �ӵ�
    public float rotateSpeed = 10.0f; //ȸ���ӵ�
    public float animationDampTime = 0.2f; //�ִϸ��̼� ���޽ð� ª������ ������ ����
    public bool jump = false; //��������

    //����
    public float jumpHeight = 1.5f; // ���� ����

    public float GroundedOffset = 0.15f; //������� offset
    public float GroundedRadius = 0.20f; //���� �� ����

    public float JumpTimeout = 2.0f; //���� �� ���� ���� ��Ÿ��

    public float gravity = -15f; // �߷� ��
    public float verticalVelocity; // ���� ���� �ӵ�
    public float terminalVelocity = 53.0f; //���� �ִ� �ӵ�

    public bool Grounded = true; //���� �پ��ִ��� ����
    public LayerMask GroundLayers; //���̾��ũ
    public Camera playerCamera;

    [Header("�����")]
    //�����
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("�ִϸ��̼�")]
    //�ִϸ��̼�
    public bool _hasAnimator;
    public float SpeedChangeRate = 10f; //Speed�Ķ���� �ٲ�� �ӵ� ������ų ��
    public float _ani_SpeedValue; //Speed �Ķ���� ������ų ��
    public readonly int _animIDGrounded = Animator.StringToHash("Grounded"); //���� �ִ���
    public readonly int _animIDSpeed = Animator.StringToHash("Speed"); //�ӵ�
    public readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed"); //�����Ӽӵ�
    public readonly int _animIDJump = Animator.StringToHash("Jump"); //����
    public readonly int _animIDFreeFall = Animator.StringToHash("FreeFall");

    public Targeter targeter;

    public PlayerFreeLookState FreeLookState { get; private set; }
    public PlayerTargetLookState TargetLookState { get; private set; }

    private void Awake()
    {
        // ���� �ν��Ͻ� �̸� ����
        FreeLookState = new PlayerFreeLookState(this);
        TargetLookState = new PlayerTargetLookState(this);
    }

    private void Start()
    {
        SwitchState(FreeLookState);

        _hasAnimator = TryGetComponent(out animator);
    }
    private void OnEnable()
    {
        inputReader.jumpPressed += OnJumpPressed;
        inputReader.TargetPressed += OnTargetPressed;
    }

    private void OnDisable()
    {
        inputReader.jumpPressed -= OnJumpPressed;
        inputReader.TargetPressed -= OnTargetPressed;
    }

    public void OnJumpPressed()
    {
        // ���� ���¿��� ���� ó��
        if (Grounded)
        {
            if (currentState is PlayerBaseState baseState)
            {
                baseState.Jump();
            }
        }
    }

    public void OnTargetPressed()
    {
        if (currentState is PlayerBaseState baseState)
        {
            baseState.onPressedTarget();
        }
    }

    public void OnLand(AnimationEvent animationEvent)
    {
        // �ʿ�� ���� ȿ���� ���
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            // ���� ȿ������ �߰������� ������ ��� ���
            AudioSource.PlayClipAtPoint(
                LandingAudioClip,
                transform.position,
                FootstepAudioVolume
            );
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (targeter != null && targeter.SphereCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, targeter.SphereCollider.radius);
        }
    }
#endif
}
