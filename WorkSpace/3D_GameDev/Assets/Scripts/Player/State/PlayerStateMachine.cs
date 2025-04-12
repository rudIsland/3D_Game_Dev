using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;


/*
 * �÷��̾��� ���� ����
 */
public class PlayerStateMachine : BaseStateMachine
{
    public CharacterController characterController;
    public PlayerInputReader inputReader;
    public PlayerWeapon weapon;

    [Header("�÷��̾� ������")]
    public float moveSpeed = 2.5f; //�⺻ ������ �ӵ�
    public float sprintSpeed = 5.333f; //�޸��� �ӵ�
    public float rotateSpeed = 10.0f; //ȸ���ӵ�
    public float animationDampTime = 0.2f; //�ִϸ��̼� ���޽ð� ª������ ������ ����
    public bool jump = false; //��������
    public bool isDead  {get; private set;}= false;

    //����
    public float jumpHeight = 1.0f; // ���� ����

    public float GroundedOffset = 0.15f; //������� offset
    public float GroundedRadius = 0.30f; //���� �� ����

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
    public readonly int _animIDHit = Animator.StringToHash("Hit");
    public readonly int _animIDAttack = Animator.StringToHash("Attack");
    public readonly int _animIDDead = Animator.StringToHash("Dead");

    public Targeter targeter;

    public PlayerStatComponent stats;

    private void Awake()
    {
        weapon.gameObject.SetActive(false);
        stats = GetComponent<PlayerStatComponent>();
    }

    private void Start()
    {
        SwitchState(new PlayerFreeLookState(this));
        _hasAnimator = TryGetComponent(out animator);
    }
    private void OnEnable()
    {
        inputReader.jumpPressed += OnJumpPressed;
    }

    private void OnDisable()
    {
        inputReader.jumpPressed -= OnJumpPressed;
    }

    public void TakeDamage(double damage)
    {
        stats.TakeDamage(damage);
    }

    public void HandlePlayerDeath()
    {
        Debug.Log("�÷��̾� ���");
        // ���� ���� ó��
        isDead = true;
        SwitchState(new PlayerDeadState(this));

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

    //-----------------------------Animation Logic
    private void OffHit()
    {
        animator.SetBool(_animIDHit, false);
    }

    public void StartAttack()
    {
        if (inputReader.isAttack)
        {
            animator.SetTrigger(_animIDAttack);
            inputReader.isAttack = false;
        }
    }

    protected void EndAttack()
    {
        inputReader.isAttack = false;
        OFFWeapon();
        animator.ResetTrigger(_animIDAttack); // Ʈ���� �ʱ�ȭ!
    }

    private void ONWeapon()
    {
        weapon.gameObject.SetActive(true);
    }
    private void OFFWeapon()
    {
        weapon.gameObject.SetActive(false);
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
