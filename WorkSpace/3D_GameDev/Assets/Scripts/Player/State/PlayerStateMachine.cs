using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


/*
 * 플레이어의 현재 상태
 */
public class PlayerStateMachine : BaseStateMachine
{
    public CharacterController characterController;
    public PlayerInputReader inputReader;
    public PlayerWeapon weapon;

    [Header("플레이어 움직임")]
    public float moveSpeed = 2.5f; //기본 움직임 속도
    public float sprintSpeed = 5.333f; //달리기 속도
    public float rotateSpeed = 10.0f; //회전속도
    public float animationDampTime = 0.2f; //애니메이션 도달시간 짧을수록 빠르게 도달
    public bool jump = false; //점프여부

    //점프
    public float jumpHeight = 1.0f; // 점프 높이

    public float GroundedOffset = 0.15f; //지면까지 offset
    public float GroundedRadius = 0.30f; //지면 원 지름

    public float JumpTimeout = 2.0f; //착지 후 점프 방지 쿨타임

    public float gravity = -15f; // 중력 값
    public float verticalVelocity; // 현재 수직 속도
    public float terminalVelocity = 53.0f; //낙하 최대 속도

    public bool Grounded = true; //땅에 붙어있는지 여부
    public LayerMask GroundLayers; //레이어마스크
    public Camera playerCamera;

    [Header("오디오")]
    //오디오
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("애니메이션")]
    //애니메이션
    public bool _hasAnimator;
    public float SpeedChangeRate = 10f; //Speed파라미터 바뀌는 속도 증가시킬 값
    public float _ani_SpeedValue; //Speed 파라미터 설정시킬 값
    public readonly int _animIDGrounded = Animator.StringToHash("Grounded"); //땅에 있는지
    public readonly int _animIDSpeed = Animator.StringToHash("Speed"); //속도
    public readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed"); //움직임속도
    public readonly int _animIDJump = Animator.StringToHash("Jump"); //점프
    public readonly int _animIDFreeFall = Animator.StringToHash("FreeFall");
    public readonly int _animIDHit = Animator.StringToHash("Hit");
    public readonly int _animIDAttack = Animator.StringToHash("Attack");
   

    public Targeter targeter;

    private void Awake()
    {
        weapon.gameObject.SetActive(false);
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

    public void OnJumpPressed()
    {
        // 현재 상태에서 점프 처리
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
        // 필요시 착지 효과음 재생
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            // 착지 효과음을 추가적으로 설정한 경우 재생
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
        animator.ResetTrigger(_animIDAttack); // 트리거 초기화!
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
