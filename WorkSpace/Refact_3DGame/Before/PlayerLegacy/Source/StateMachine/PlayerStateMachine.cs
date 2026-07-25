using System;
using UnityEngine;

/*
 * 플레이어의 현재 상태
 * * 플레이어의 상태머신과 체력, 스태미나, 경험치, 레벨업 시스템 등이 실질적으로 동작하는 클래스
 * */
public class PlayerStateMachine : MonoBehaviour, IDamageable
{
    public CharacterController characterController;
    public PlayerInputReader inputReader;
    public PlayerWeapon weapon;

    [Header("스탯 프리셋(SO)")]
    [SerializeField] private PlayerStatPreset statPreset;
    [SerializeField] private PlayerSavedData saveData;     // 실시간/저장 데이터

    // --- 데이터 통로 (Properties) ---
    // 외부(GUI)에서는 예전처럼 player.maxHP로 부르지만, 실제로는 saveData에서 가져옵니다.
    public float maxHP => saveData.maxHP;
    public float attack => saveData.attack;
    public float defense => saveData.defense;

    public float maxStamina => statPreset.maxStamina;
    public float staminaRegenDelay => statPreset.staminaRegenDelay;
    public float staminaRegenRate => statPreset.staminaRegenPerSecond;

    public int currentLevel => saveData.level;
    public int currentExp => saveData.exp;
    public int nextLevelMaxExp => saveData.nextLevelMaxExp;
    public int statPoints => saveData.statPoints;



    public float currentHP
    {
        get => saveData.currentHP; // 원본 데이터에서 읽음
        set
        {
            // 1. 원본 데이터 수정 (saveData.maxHP 기준으로 클램프)
            saveData.currentHP = Mathf.Clamp(value, 0, saveData.maxHP);

            // 2. UI 이벤트 알림 (수정된 원본 값을 보냄)
            OnHPChanged?.Invoke(saveData.currentHP, saveData.maxHP);
        }
    }

    public float currentStamina
    {
        get => saveData.currentStamina;
        set
        {
            // 1. 원본 데이터 수정 (statPreset.maxStamina 기준으로 클램프)
            saveData.currentStamina = Mathf.Clamp(value, 0, statPreset.maxStamina);

            // 2. UI 이벤트 알림 (변경된 원본 값을 보냄)
            OnStaminaChanged?.Invoke(saveData.currentStamina, statPreset.maxStamina);
        }
    }
 
  
    public float sprintStaminaCost => statPreset.sprintStaminaCost;     
    public float jumpStaminaCost => statPreset.jumpStaminaCost; 
    private float lastStaminaUseTime;

    // -------------------------------------------------------

    [Header("플레이어 움직임")]
    public float moveSpeed = 2.5f;
    public float sprintSpeed = 5.333f;
    public float rotateSpeed = 10.0f;
    public float animationDampTime = 0.2f;
    public bool isJumping = false;
    public bool Attacking = false;
    public bool isHitting = false;
    public bool isExhausted = false;

    public Vector3 horizontalDir;
    public Vector3 verticallDir;

    public float GroundedOffset = 0.15f;
    public float GroundedRadius = 0.30f;

    [Header("Jump / Fall Timeout")]
    public float jumpHeight = 1.4f;
    public float JumpTimeout = 0.13f;
    public float FallTimeout = 0.15f;

    [HideInInspector] public float jumpTimeoutDelta;
    [HideInInspector] public float fallTimeoutDelta;

    public float gravity = -22f;
    public float verticalVelocity;
    public float terminalVelocity = -50.0f;

    public bool Grounded;
    public LayerMask GroundLayers;
    public Camera playerCamera;

    public Animator animator;
    public int stateNum;
    [Header("머터리얼")]
    public Material originalMaterial; // Inspector에서 원본 머터리얼 배열 할당
    public SkinnedMeshRenderer[] playerSkin;
    public Material deadMtl;

    [Header("오디오")]
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("애니메이션")]
    public readonly int _animIDBlendIndex = Animator.StringToHash("BlendIndex");
    public bool _hasAnimator;
    public float SpeedChangeRate = 10f;
    public float _ani_SpeedValue;
    public readonly int _animIDGrounded = Animator.StringToHash("Grounded");
    public readonly int _animIDSpeed = Animator.StringToHash("Speed");
    public readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    public readonly int _animIDJump = Animator.StringToHash("Jump");
    public readonly int _animIDFreeFall = Animator.StringToHash("FreeFall");
    public readonly int _animIDHit = Animator.StringToHash("Hit");
    public readonly int _animIDAttack = Animator.StringToHash("Attack");
    public readonly int _animIDDead = Animator.StringToHash("Dead");

    public PlayerFreeLookState _FreeLookState;
    public PlayerTargetLookState _TargetLookState;
    public PlayerHitState _HitState;
    public PlayerDeadState _DeadState;

    public State currentState = null;

    bool isDead = false;

    public event Action OnDeath;
    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnHPChanged;
    public event Action<int, int> OnLevelChanged;
    public event Action<int, int> OnExpChanged;
    public event Action OnStatChanged;

    public int killCount => saveData.killCount;

    public PlayerTargeter targeter;

    private void Awake()
    {
        _hasAnimator = TryGetComponent(out animator);

        // [해결 1] 상태 생성을 가장 먼저 해야 합니다! 
        // 그래야 아래에서 Initialize_Stats를 호출할 때 null 에러가 안 납니다.
        Initialize_States();

        // 나머지 초기화
        weapon.gameObject.SetActive(false);
        JumpTimeout = 0f;

        bool hasSaveFile = SaveSystem.Load(saveData);

        if (!hasSaveFile)
        {
            Initialize_Stats();
        }
        else
        {
            UpdateNextLevelExp();

            // 데이터 로드 후 첫 상태 설정 (여기서도 SwitchState가 필요할 수 있음)
            SwitchState(_FreeLookState);

            OnLevelChanged?.Invoke(saveData.level, saveData.nextLevelMaxExp);
            OnExpChanged?.Invoke(saveData.exp, saveData.nextLevelMaxExp);
            OnHPChanged?.Invoke(saveData.currentHP, saveData.maxHP);
            OnStaminaChanged?.Invoke(saveData.currentStamina, statPreset.maxStamina);
            OnStatChanged?.Invoke();
        }
    }

    private void Initialize_States()
    {
        _FreeLookState = new PlayerFreeLookState(this);
        _TargetLookState = new PlayerTargetLookState(this);
        _HitState = new PlayerHitState(this);
        _DeadState = new PlayerDeadState(this);
    }

    private void Start()
    {
    
    }


    private void Update()
    {
        currentState.Tick(Time.deltaTime);
        HandleStaminaRegen();
        CheckExhaustion();
    }

    public void Initialize_Stats()
    {
        // 1. 캐릭터 컨트롤러 다시 켜기 (가장 중요: Move 에러 방지)
        if (characterController != null)
        {
            characterController.enabled = true;
        }



        // 3. 장부 데이터(SO) 리셋
        saveData.ResetFromPreset(statPreset);
        saveData.killCount = 0; // 명시적 초기화

        // 4. 실시간 값 할당
        currentHP = saveData.maxHP;
        currentStamina = statPreset.maxStamina;
        isDead = false;

        UpdateNextLevelExp();
        SyncUIAll();

        // 5. 머터리얼 원본으로 되돌리기
        ResetMaterials();

        // 6. 상태를 기본 상태(FreeLook)로 강제 전환
        SwitchState(_FreeLookState);

        Debug.Log("<color=green>[플레이어 모든 상태 리셋 완료]</color>");
    }

    private void ResetMaterials()
    {
        if (originalMaterial == null || playerSkin == null) return;

        for (int i = 0; i < playerSkin.Length; i++)
        {
            if (playerSkin[i] == null) continue;

            // [핵심] 각 파츠(메쉬)가 가진 머터리얼 슬롯 개수를 확인합니다.
            int slotCount = playerSkin[i].sharedMaterials.Length;

            // 슬롯 개수에 맞는 새로운 머터리얼 배열을 생성합니다.
            Material[] newMaterials = new Material[slotCount];

            // 모든 슬롯에 원본 머터리얼(1개)을 채워넣습니다.
            for (int j = 0; j < slotCount; j++)
            {
                newMaterials[j] = originalMaterial;
            }

            // 해당 파츠에 완성된 배열을 적용합니다.
            playerSkin[i].materials = newMaterials;
        }
    }

    private void OnEnable()
    {
        OnDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        OnDeath -= HandlePlayerDeath;
    }

    private void CheckExhaustion()
    {
        if (currentStamina <= 0)
        {
            isExhausted = true;
        }

        // sprintStaminaCost 대신 statPreset 값 직접 참조
        if (isExhausted && currentStamina >= statPreset.sprintStaminaCost)
        {
            isExhausted = false;
        }
    }

    public void SwitchState(State newState)
    {
        if (currentState == newState) return;

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

    private void UpdateNextLevelExp()
    {
        if (statPreset.levelTemplate != null)
        {
            saveData.nextLevelMaxExp = statPreset.levelTemplate.GetRequiredExp(saveData.level);
            // currentLevel, nextLevelMaxExp 변수가 주석 처리되었으므로 동기화 로직은 생략하거나 saveData를 직접 사용하게 됨
        }
    }

    public void AddExperience(int amount)
    {
        // 1. 경험치 추가
        saveData.exp += amount;

        // [추가] 경험치 획득 로그 표시
        // 획득량(amount)과 획득 후 현재 경험치(saveData.exp), 그리고 다음 레벨까지 필요한 양을 출력합니다.
        Debug.Log($"<color=cyan>[경험치 획득]</color> 획득량: {amount} | 현재 경험치: {saveData.exp} / {saveData.nextLevelMaxExp}");

        // 2. 레벨업 체크 (무한 루프 방지 포함)
        int safetyNet = 0;
        while (saveData.exp >= saveData.nextLevelMaxExp && safetyNet < 100)
        {
            LevelUp();
            safetyNet++;
        }

        // 3. UI 갱신 이벤트 호출
        OnExpChanged?.Invoke(saveData.exp, saveData.nextLevelMaxExp);
    }

    private void LevelUp()
    {
        saveData.exp -= saveData.nextLevelMaxExp;
        saveData.level++;
        saveData.statPoints += 3;

        UpdateNextLevelExp();

        saveData.currentHP = saveData.maxHP;
        OnHPChanged?.Invoke(saveData.currentHP, saveData.maxHP);

        OnLevelChanged?.Invoke(saveData.level, saveData.nextLevelMaxExp);
        OnStatChanged?.Invoke();

        // [추가] 레벨업 시 데이터 매니저를 통해 현재 씬 번호와 함께 저장
        if (DataManager.Instance != null)
        {
            DataManager.Instance.SaveGame();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLevelUpPanel();
        }

        Debug.Log($"<color=yellow>레벨 업!</color> 현재 레벨: {saveData.level}, 보유 포인트: {saveData.statPoints}");
    }

    public void IncreaseMaxHP()
    {
        if (saveData.statPoints <= 0) return;

        saveData.statPoints--;
        saveData.maxHP += 20f;
        saveData.currentHP += 20f;

        OnHPChanged?.Invoke(saveData.currentHP, saveData.maxHP);
        OnStatChanged?.Invoke();

        // [추가] 스탯 변경 시 자동 저장
        DataManager.Instance.SaveGame();
    }

    public void IncreaseAttack()
    {
        if (saveData.statPoints <= 0) return;

        saveData.statPoints--;
        saveData.attack += 2f;

        OnStatChanged?.Invoke();

        // [추가] 스탯 변경 시 자동 저장
        DataManager.Instance.SaveGame();
    }

    public void IncreaseDefense()
    {
        if (saveData.statPoints <= 0) return;

        saveData.statPoints--;
        saveData.defense += 1f;

        OnStatChanged?.Invoke();

        // [추가] 스탯 변경 시 자동 저장
        DataManager.Instance.SaveGame();
    }

    public void UseStamina(float amount)
    {
        currentStamina -= amount;
        lastStaminaUseTime = Time.time;
    }

    private void HandleStaminaRegen()
    {
        // staminaRegenDelay, staminaRegenRate 대신 statPreset 값 직접 참조
        if (Time.time - lastStaminaUseTime >= statPreset.staminaRegenDelay && currentStamina < statPreset.maxStamina)
        {
            currentStamina += statPreset.staminaRegenPerSecond * Time.deltaTime;
        }
    }

    public void HandlePlayerDeath()
    {
        Debug.Log("플레이어 사망");
        isDead = true;
        SwitchState(_DeadState);

    }

    public void OnJumpPressed()
    {
        if (!Grounded) return;

        if (JumpTimeout > 0f) return;

        isJumping = true;
    }

    public void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(
                LandingAudioClip,
                transform.position,
                FootstepAudioVolume
            );
        }

        animator.SetBool(_animIDJump, false);
        animator.SetBool(_animIDFreeFall, false);

        JumpTimeout = 2.0f;
    }

    private void EndHit()
    {
        isHitting = false;
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
        animator.ResetTrigger(_animIDAttack);
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

    public void TakeDamage(DamageInfo damageInfo)
    {
        currentHP -= damageInfo.amount;
        currentHP = Mathf.Max(currentHP, 0);

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            SwitchState(_HitState);
        }
    }

    public void OnEnemyKilled()
    {
        // 1. 데이터 장부의 처치 수 증가
        saveData.killCount++;

        Debug.Log($"<color=orange>[처치 기록]</color> 총 처치 수: {saveData.killCount}");

        // 2. 처치할 때마다 즉시 저장 (데이터 유실 방지)
        if (DataManager.Instance != null)
        {
            DataManager.Instance.SaveGame();
        }
    }


    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        OnDeath?.Invoke();
        SwitchState(_DeadState);
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


    // 모든 UI 이벤트를 한 번에 쏴주는 헬퍼 함수
    private void SyncUIAll()
    {
        OnHPChanged?.Invoke(saveData.currentHP, saveData.maxHP);
        OnStaminaChanged?.Invoke(saveData.currentStamina, statPreset.maxStamina);
        OnLevelChanged?.Invoke(saveData.level, saveData.nextLevelMaxExp);
        OnExpChanged?.Invoke(saveData.exp, saveData.nextLevelMaxExp);
        OnStatChanged?.Invoke(); // 스탯창(공격력, 방어력 등) 갱신
    }
}