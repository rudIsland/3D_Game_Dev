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
    private void OnEnable()
    {
        inputReader.jumpPressed += OnJumpPressed;
    }

    private void OnDisable()
    {
        inputReader.jumpPressed -= OnJumpPressed;
    }

    public override void ApplyDamage(double damage)
    {
        stats.currentHP -= damage;
        stats.currentHP = Mathf.Max((float)stats.currentHP, 0);

        animator.SetBool(_animIDHit, true);

        CheckDie();

        statComp.UpdateResource();
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
