//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;


////�÷��̾�
//public class BaseStateMachine : CharacterBase
//{
//    //���� ����
//    public Animator animator;
//    public int stateNum;

//    [Header("�����")]
//    //�����
//    public AudioClip LandingAudioClip;
//    public AudioClip[] FootstepAudioClips;
//    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

//    [Header("�ִϸ��̼�")]
//    public readonly int _animIDBlendIndex = Animator.StringToHash("BlendIndex"); //state Number
//    //�ִϸ��̼�
//    public bool _hasAnimator;
//    public float SpeedChangeRate = 10f; //Speed�Ķ���� �ٲ�� �ӵ� ������ų ��
//    public float _ani_SpeedValue; //Speed �Ķ���� ������ų ��
//    public readonly int _animIDGrounded = Animator.StringToHash("Grounded"); //���� �ִ���
//    public readonly int _animIDSpeed = Animator.StringToHash("Speed"); //�ӵ�
//    public readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed"); //�����Ӽӵ�
//    public readonly int _animIDJump = Animator.StringToHash("Jump"); //����
//    public readonly int _animIDFreeFall = Animator.StringToHash("FreeFall");
//    public readonly int _animIDHit = Animator.StringToHash("Hit");
//    public readonly int _animIDAttack = Animator.StringToHash("Attack");
//    public readonly int _animIDDead = Animator.StringToHash("Dead");

//    //���� ����
//    public State currentState = null;

//    public override CharacterStatsComponent statComp => GetComponent<PlayerStatComponent>();
//    public PlayerStatComponent PlayerStats => statComp as PlayerStatComponent;

//    //���¸� �ٲٴ� �޼ҵ�
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
