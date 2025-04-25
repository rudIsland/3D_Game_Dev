//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;


////플레이어
//public class BaseStateMachine : CharacterBase
//{
//    //공통 변수
//    public Animator animator;
//    public int stateNum;

//    [Header("오디오")]
//    //오디오
//    public AudioClip LandingAudioClip;
//    public AudioClip[] FootstepAudioClips;
//    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

//    [Header("애니메이션")]
//    public readonly int _animIDBlendIndex = Animator.StringToHash("BlendIndex"); //state Number
//    //애니메이션
//    public bool _hasAnimator;
//    public float SpeedChangeRate = 10f; //Speed파라미터 바뀌는 속도 증가시킬 값
//    public float _ani_SpeedValue; //Speed 파라미터 설정시킬 값
//    public readonly int _animIDGrounded = Animator.StringToHash("Grounded"); //땅에 있는지
//    public readonly int _animIDSpeed = Animator.StringToHash("Speed"); //속도
//    public readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed"); //움직임속도
//    public readonly int _animIDJump = Animator.StringToHash("Jump"); //점프
//    public readonly int _animIDFreeFall = Animator.StringToHash("FreeFall");
//    public readonly int _animIDHit = Animator.StringToHash("Hit");
//    public readonly int _animIDAttack = Animator.StringToHash("Attack");
//    public readonly int _animIDDead = Animator.StringToHash("Dead");

//    //현재 상태
//    public State currentState = null;

//    public override CharacterStatsComponent statComp => GetComponent<PlayerStatComponent>();
//    public PlayerStatComponent PlayerStats => statComp as PlayerStatComponent;

//    //상태를 바꾸는 메소드
//    public void SwitchState(State newState)
//    {
//        currentState?.Exit();
//        currentState = newState;
//        switch (currentState)
//        {
//            case PlayerFreeLookState:
//                stateNum = 0;
//                animator.SetInteger(_animIDBlendIndex, stateNum);
//                break;

//            case PlayerTargetLookState:
//                stateNum = 1;
//                animator.SetInteger(_animIDBlendIndex, stateNum);
//                break;

//            default:
//                stateNum = 0;
//                animator.SetInteger(_animIDBlendIndex, stateNum);
//                break;
//        }
//        currentState.Enter();
//    }

//    private void Update()
//    {
//        currentState.Tick(Time.deltaTime);
//    }

//    public override void ApplyDamage(double damage)  { }


//}
