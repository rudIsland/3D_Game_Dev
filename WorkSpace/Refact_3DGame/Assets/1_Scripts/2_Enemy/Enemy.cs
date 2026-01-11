
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : MonoBehaviour, IDamageable
{
    public Animator animator;

    [SerializeField] protected EnemyStatPreset stat; // 상속받은 MutantStatPreset도 여기 담깁니다.

    [SerializeField] protected float _DetectRange;  //탐지범위
    [SerializeField] protected float _AttackRange;  //공격범휘
    [SerializeField] protected float _MoveSpeed;    //움직임 속도
    [SerializeField] protected float _AngularSpeed; //회전속도

    [SerializeField] protected float _MaxHP;        //최대 체력
    [SerializeField] protected float _CurrentHP;    //현재 체력
    [SerializeField] protected float _AttackPower;  //공격력
    [SerializeField] protected float _DefencePower; //방어력
    [SerializeField] protected int _DeathEXP;       //죽음시 플레이어에게 줄 경험치

    // 외부 접근용 (읽기 전용)
    public float detectRange => _DetectRange;
    public float attackRange => _AttackRange;
    public float moveSpeed => _MoveSpeed;
    public float angularSpeed => _AngularSpeed;

    public float maxHP => _MaxHP;
    public float currentHP => _CurrentHP;
    public float attackPower => _AttackPower;
    public float defense => _DefencePower;
    public int deathEXP => _DeathEXP;


    [Header("플레이어 탐지콜리더")]
    public PlayerDetector playerDetector;           //플레이어 탐색
    public bool isDetected = false;


    // ... 기존 변수들 ...
    [Header("UI Reference")]
    public EnemyGUI enemyGUI; // 인스펙터에서 EnemyGUI 오브젝트를 드래그 앤 드롭 하세요.
    [SerializeField] protected int _Level = 1;


    //이벤트
    public event Action<Enemy> OnEnemyDeath; // 경험치 전달 이벤트
    public event Action<Enemy> OnDeathEffectFinished; // 연출 종료 후 (풀 반납, 리스폰)

    [Header("머터리얼")]
    public Material[] mDefaultMtl;
    public Material[] mDetectedMtl;
    public Material[] mTargetMtl;
    public Material[] mDeadMtl;


    [Header("상태")]
    public bool isAttacking = false;    //공격여부
    public bool isHit = false;          //맞았는지 여부
    public bool isTarget; //현재 대상이 플레이어의 타겟인지 확인
    protected bool isDead = false;


    public readonly int _animIDDead = Animator.StringToHash("DeathTrigger"); //죽음
    public readonly int _animIDisHit = Animator.StringToHash("IsHit"); //피격
    
    [Header("머터리얼")]
    protected Material[] _runtimeMaterials;
    // 렌더러 캐싱용 변수 추가
    protected SkinnedMeshRenderer mainRenderer;
    protected EnemyStateMachine stateMachine;

    //Dissolve효과 프로퍼티를 해쉬 정수값으로 저장한다.
    private static readonly int NoiseAmountID = Shader.PropertyToID("_NoiseAmount");

    [Header("AI")]
    public NavMeshAgent navAgent;

    [Header("무기")]
    public EnemyWeapon[] weapon;
    public int currentHitIndex = 0; // 애니메이션 이벤트로 변경될 값

    protected virtual void Awake()
    {
        //컴포넌트 가져오기
        animator = GetComponent<Animator>();
        mainRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        stateMachine = GetComponent<EnemyStateMachine>();
        navAgent = GetComponent<NavMeshAgent>();

        //기본 머터리얼 적용
        mainRenderer.materials = mDefaultMtl;
    }

    // Enemy.cs의 일부 수정
    public virtual void OnEnable() // 풀에서 꺼낼 때 실행
    {
        InitializeEnemy();
    }

    public virtual void InitializeEnemy()
    {
        // 1. 플레이어의 현재 레벨 참조 (StageManager를 통해 접근)
        int playerLevel = 1;
        if (StageManager.Instance != null && StageManager.Instance.player != null)
        {
            playerLevel = StageManager.Instance.player.currentLevel;
        }

        // 2. 몬스터 레벨 결정 (플레이어 레벨과 동기화)
        _Level = playerLevel;

        // 3. [핵심] 레벨 스케일링 계산 (기본값 + 성장치 * (레벨-1))
        int levelFactor = _Level - 1;

        // 스탯 계산
        _MaxHP = stat.maxHP + (stat.hpGrowthPerLevel * levelFactor);
        _CurrentHP = _MaxHP;
        _AttackPower = stat.attack + (stat.atkGrowthPerLevel * levelFactor);
        _DefencePower = stat.defense + (stat.defGrowthPerLevel * levelFactor);

        // 공통 범위 스탯 할당 (프리셋의 기본값들)
        _DetectRange = stat._DetectRange;
        _AttackRange = stat._AttackRange;
        _MoveSpeed = stat._MoveSpeed;
        _AngularSpeed = stat._AngularSpeed;

        // 경험치 계산
        _DeathEXP = stat.baseDeathEXP + (stat.expGrowthPerLevel * levelFactor);

        // ---------------------------------------------------------
        // 4. 기존 초기화 로직 (물리/비주얼/상태)
        isDead = false;
        isHit = false;
        isTarget = false;
        isAttacking = false;
        isDetected = false;

        gameObject.layer = LayerMask.NameToLayer("Enemy");
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) col.enabled = true;

        if (navAgent != null)
        {
            navAgent.enabled = true;
            navAgent.ResetPath();
        }

        // UI 갱신 (레벨과 체력바 업데이트)
        if (enemyGUI != null)
        {
            enemyGUI.SetVisible(true);
            enemyGUI.SetLevel(_Level);
            enemyGUI.UpdateHPUI(_CurrentHP, _MaxHP);
        }

        // 비주얼 리셋
        if (mainRenderer != null)
        {
            mainRenderer.materials = mDefaultMtl;
            _runtimeMaterials = mainRenderer.materials;
            foreach (var mat in _runtimeMaterials)
            {
                if (mat.HasProperty(NoiseAmountID)) mat.SetFloat(NoiseAmountID, 0f);
            }
        }

        // 애니메이터 및 상태머신 리셋
        if (animator != null) { animator.Rebind(); animator.Update(0f); }
        if (stateMachine != null) stateMachine.SwitchState(stateMachine.idleState);
    }

    // 애니메이션 이벤트에서 호출할 함수
    public void SetHitIndex(int index)
    {
        currentHitIndex = index;
    }

    // 무기가 충돌했을 때 데미지를 요청하는 함수
    public float GetCurrentAttackDamage(int weaponIndex)
    {
        // 현재 상태가 공격 상태인지 확인
        if (stateMachine.currentState is IAttackState attackState)
        {
            return attackState.GetDamageFromData(weaponIndex, currentHitIndex);
        }
        return 0f;
    }

    public void ActiveWeapon(int index)
    {
        weapon[index].gameObject.SetActive(true);
    }
    public void DisActiveWeapon(int index)
    {
        weapon[index].gameObject.SetActive(false);
    }


    protected virtual void Update() {   }


    public abstract void SetupStateMachine(EnemyStateMachine fsm);


    // 디졸브가 끝난 시점에 호출할 함수
    public void ReportEffectFinished()
    {
        OnDeathEffectFinished?.Invoke(this);
    }
    public void CallDeathEvent()
    {
        // 1. 경험치 전달은 죽자마자 즉시
        OnEnemyDeath?.Invoke(this);

        Debug.Log($"<color=red>[사망 방송]</color> {gameObject.name}이(가) 죽으면서 {deathEXP} 경험치를 보냈습니다.");
    }

    public void StartDissolve()
    {
        StartCoroutine(DissolveCoroutine());
    }

    private IEnumerator DissolveCoroutine()
    {
        if (mainRenderer == null) yield break;

        mainRenderer.materials = mDeadMtl;

        _runtimeMaterials = mainRenderer.materials;

        float t = 0f;
        float duration = 5f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float dissolve = Mathf.Clamp01(t / duration);

            // 미리 가져온 mats 배열을 순회하며 값을 바꿉니다.
            foreach (var mat in _runtimeMaterials)
            {
                if (mat.HasProperty(NoiseAmountID))
                    mat.SetFloat(NoiseAmountID, dissolve);
            }

            yield return null;
        }

        // 완전히 사라짐 확실하게 처리
        foreach (var mat in _runtimeMaterials)
        {
            if (mat.HasProperty(NoiseAmountID))
                mat.SetFloat(NoiseAmountID, 1f);
        }

        // 모든 연산과 시각 효과가 끝난 "직후"에 이벤트를 호출해서 풀로 돌려보냅니다.
        gameObject.SetActive(false); //오브젝트 풀링 비활성화 처리
        // 연출이 끝났음을 보고
        ReportEffectFinished();
    }

    public void ChangeTargettMtl()
    {
        mainRenderer.materials = mTargetMtl;
    }

    public void ChangeDetectedMtl()
    {
        mainRenderer.materials = mDetectedMtl;
    }

    public void ChangeDefaultMtl()
    {
        if (mainRenderer == null) mainRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (mainRenderer == null) return;

        // 핵심: 지금 타겟팅 된 상태라면 기본 머터리얼로 돌리지 말고 타겟 머터리얼을 유지해라!
        if (isTarget)
        {
            mainRenderer.materials = mTargetMtl;
        }
        else if (isDetected)
        {
            mainRenderer.materials = mDetectedMtl;
        }
        else
        {
            mainRenderer.materials = mDefaultMtl;
        }
    }

    // 모든 적의 공통 피격 로직
    public virtual void TakeDamage(DamageInfo info)
    {
        if (isDead) return;

        _CurrentHP -= info.amount;

        if (enemyGUI != null)
        {
            enemyGUI.UpdateHPUI(_CurrentHP, _MaxHP);
        }
        

        if (_CurrentHP <= 0)
        {
            Die();
        }
        else
        {
            // 피격 상태로 전환 (모든 Enemy는 hitState를 가짐)
            stateMachine.SwitchState(stateMachine.hitState);
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        if (enemyGUI != null)
            enemyGUI.SetVisible(false); // UI 숨기기

        ReleaseTarget();
        stateMachine.SwitchState(stateMachine.deadState);
    }

    public void ReleaseTarget()
    {
        isTarget = false;
        ChangeDefaultMtl();
        // 죽은 뒤에는 레이캐스트에 걸리지 않도록 레이어 변경 (Ignore Raycast 레이어 권장)
        //gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }
}
