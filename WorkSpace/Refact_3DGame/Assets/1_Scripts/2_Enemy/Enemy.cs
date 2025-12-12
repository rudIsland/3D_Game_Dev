using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Collections;
using System;
using System.Linq;
using UnityEngine.InputSystem.Processors;

public abstract class Enemy : MonoBehaviour
{
    public Animator animator;

    [SerializeField] protected float detectRange ;
    [SerializeField] protected float attackRange;
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float angularSpeed;

    [SerializeField]  protected double maxHP;
    [SerializeField]  protected double currentHP;
    [SerializeField] protected double attackPower;
    [SerializeField] protected double defencePower;

    public bool isTarget; //현재 대상이 플레이어의 타겟인지 확인

    /*
    public NavMeshAgent agent;

    public bool isAttacking = false;

    [Header("공통 체력UI")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    public static event Action<float> OnEnemyKilled; // 경험치 전달 이벤트

    [Header("머터리얼")]
    public Material[] defaultMtl;
    public Material[] detectedMtl;
    public Material[] TargetMtl;
    public Material[] DeadMtl;

    public bool isTarget; //현재 대상이 플레이어의 타겟인지 확인


    public Material[] currentMtl;
    */

    protected EnemyStateMachine stateMachine;

    protected bool isDead = false;

    protected virtual void Awake()
    {
        //agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        stateMachine = GetComponent<EnemyStateMachine>();
    }

    /*protected virtual void Start()
    {
        agent.speed = moveSpeed; //움직임 속도
        agent.stoppingDistance = attackRange; //공격 범위
        agent.angularSpeed = angularSpeed;  //회전 속도
        agent.updateRotation = true;   // 자동 회전 활성화
        agent.updateUpAxis = true;     // 기본 Y축 회전

    }*/

    protected virtual void Update()
    {
        //if (Player.Instance.playerStateMachine.currentState is PlayerDeadState) return;
        //UpdateDistanceToPlayer();
        //UpdateDetectionStatus();

        
    }
    public abstract void SetupStateMachine(EnemyStateMachine fsm);


    protected virtual void HandleDeath()
    {
        Debug.Log("적 사망");
        //플레이어에게 경험치 넣기
        // 경험치 이벤트 발생

        //사라지기 시작
        StartCoroutine(DissolveCoroutine());

        StartCoroutine(DestroyEnemy());
    }

    private IEnumerator DissolveCoroutine()
    {
        float t = 0f;
        float duration = 5f; // 몇 초 동안 서서히 사라질지

        while (t < duration)
        {
            t += Time.deltaTime;
            float dissolve = Mathf.Clamp01(t / duration);

            foreach (var mat in GetComponentInChildren<SkinnedMeshRenderer>().materials)
            {
                if (mat.HasProperty("_NoiseAmount"))
                    mat.SetFloat("_NoiseAmount", dissolve);
            }

            yield return null;
        }

        // 완전히 사라짐
        foreach (var mat in GetComponentInChildren<SkinnedMeshRenderer>().materials)
        {
            mat.SetFloat("_NoiseAmount", 1f);
        }
    }

    private IEnumerator DestroyEnemy()
    {
        yield return new WaitForSeconds(5f);

        Destroy(gameObject);
    }


    /*protected virtual void RotateTowardsPlayer()
    {
        Vector3 direction = (enemyMemory.player.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }


    //플레이어와의 거리계산
    protected void UpdateDistanceToPlayer()
    {
        Vector3 toPlayer = enemyMemory.player.position - transform.position;
        toPlayer.y = 0f; // 평면 거리
        enemyMemory.distanceToPlayer = toPlayer.magnitude;
    }

    //탐지범위 체크
    protected virtual void UpdateDetectionStatus()
    {
        // if (!GameManager.Instance.player.isDead) return; 
        enemyMemory.isPlayerDetected = enemyMemory.distanceToPlayer <= detectRange;
        enemyMemory.isInAttackRange = enemyMemory.distanceToPlayer <= attackRange;

        //탐지에 대해 머터리얼 변화
        if (enemyMemory.isPlayerDetected) ChangeDetectedMtl();
        else ChangeDefaultMtl();
    }*/



}
