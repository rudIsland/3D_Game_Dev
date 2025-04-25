
using System;
using UnityEngine;

/*
 * �÷��̾��� ���� ����
 */
public class PlayerStateMachine : CharacterBase
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
    public bool Attacking = false;
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

    //���� ����
    public Animator animator;
    public int stateNum;

    [Header("�����")]
    //�����
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("�ִϸ��̼�")]
    public readonly int _animIDBlendIndex = Animator.StringToHash("BlendIndex"); //state Number
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

    //���� ����
    public State currentState = null;

    //����
    private PlayerStats stat = new PlayerStats();
    public override CharacterStats Stat => stat;
    public PlayerStats playerStat => stat;


    public Targeter targeter;

    /************************** End **************************/

    private void Awake()
    {
        _hasAnimator = TryGetComponent(out animator);
    }
    private void Start()
    {
        weapon.gameObject.SetActive(false);

        SwitchState(new PlayerFreeLookState(this));
    }
    private void Update()
    {
        currentState.Tick(Time.deltaTime);
    }


    private void OnEnable()
    {
        inputReader.jumpPressed += OnJumpPressed;
    }

    private void OnDisable()
    {
        inputReader.jumpPressed -= OnJumpPressed;
    }

    //���¸� �ٲٴ� �޼ҵ�
    public void SwitchState(State newState)
    {
        currentState?.Exit();
        currentState = newState;
        switch (currentState)
        {
            case PlayerFreeLookState:
                stateNum = 0;
                animator.SetInteger(_animIDBlendIndex, stateNum);
                break;

            case PlayerTargetLookState:
                stateNum = 1;
                animator.SetInteger(_animIDBlendIndex, stateNum);
                break;

            default:
                stateNum = 0;
                animator.SetInteger(_animIDBlendIndex, stateNum);
                break;
        }
        currentState.Enter();
    }

    public void UpStemina()
    {
        playerStat.TickRegen(Time.deltaTime);
        GameManager.Instance.Resource.UpdateStaminaUI();
    }

    public override void ApplyDamage(double damage)
    {
        stat.currentHP -= damage;
        stat.currentHP = Mathf.Max((float)stat.currentHP, 0);

        animator.SetBool(_animIDHit, true);
        CheckDie();

        GameManager.Instance.Resource.UpdateHPUI(); // ü�� UI ����
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
            Attacking = true;
        }
    }

    protected void EndAttack()
    {
        inputReader.isAttack = false;
        OFFWeapon();
        animator.ResetTrigger(_animIDAttack); // Ʈ���� �ʱ�ȭ!
        Attacking = false;
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
