# HitDamageRoadMap - Unit 생명주기 기반 타격·피격 재구성

- 작성: 2026-08-03
- 이전 기록: Docs/2026-08-01_PlayerZombieHitRoadmap.md
- 첫 적용 대상: Player ↔ Zombie
- 상태: 현재 활성 로드맵

기존 Player·Zombie 타격 로드맵의 완료 기록은 이전 문서에 보존한다. 앞으로 새 전투 코드를 추가할 때는 이 문서의 흐름, 판정 순서와 단계 완료 조건을 우선한다.

## 1. 이번 재구성에서 고정할 계약

전체 입력 → 처리 → 출력 흐름:

```text
공격 입력 또는 Enemy AI 판단
→ 공격 준비
→ Animation Event가 실제 타격 구간 시작
→ MeleeHitDetector가 무기 이전 위치와 현재 위치 사이를 검사
→ UnitHitBox 접촉 후보 수집
→ 공격 실행 번호 + 대상 기준 중복 제거
→ AttackHitQueue가 같은 프레임 후보 보관
→ 대상 활성 상태 + ActivationSequence 재확인
→ Unit.ReceiveAttackHit
→ AttackHitResultCalculator가 결과만 계산
→ Unit.ApplyAttackHitResult가 체력·Stamina·경직을 한 번에 반영
→ 파생 Unit이 결과 종류에 맞는 상태머신 전환
→ 피격 이동·애니메이션
→ 공격자에게 AttackHitResultReady 전달
→ Hit Stop·VFX·Audio·카메라 반응
```

판정 우선순위:

```text
비활성·사망·아군·잘못된 공격·중복
→ 회피 무적
→ 패리
→ 가드 방향 확인
→ 가드 성공
→ Guard Break
→ 체력 피해
→ 강인도 판정
→ Damaged / Staggered / KnockedDown / Killed
```

반드시 지킬 불변 규칙:

1. `AttackHitResultCalculator`가 결과를 반환하기 전에는 `UnitHealth`, `UnitStamina`, `UnitStagger`와 상태머신을 변경하지 않는다.
2. 체력, Stamina와 경직은 `Unit.ApplyAttackHitResult` 한 곳에서 확정된 결과만 반영한다.
3. `Killed`는 `Staggered`와 `KnockedDown`보다 항상 우선한다.
4. `Dodged`, `Parried`와 `Ignored`는 체력·Stamina·경직을 변경하지 않는다.
5. 가드 성공은 체력을 줄이지 않고 Stamina만 줄인다. Stamina가 부족한 경우만 `GuardBroken`으로 전환한다.
6. 슈퍼아머는 체력 피해를 막지 않는다. 행동 중단 기준만 보정한다.
7. 파생 Unit은 공통 판정을 다시 구현하지 않고 결과에 맞는 상태 전환만 선택한다.
8. `Disable`된 Unit과 이전 풀 활성 주기의 Unit은 이미 대기열에 들어간 타격도 받지 않는다.
9. 연출 실행 실패는 판정 결과와 체력 값을 되돌리거나 바꾸지 않는다.
10. `Update`, `LateUpdate`와 타격 검색 구간에는 LINQ, 클로저, 반복 문자열 생성과 반복 `GetComponent`를 추가하지 않는다.

## 2. 현재 코드와 목표 코드의 차이

현재 실제 흐름:

```text
Controller.ReceiveHit
→ PlayerWorldUnit/ZombieWorldUnit/NightshadeSpearWorldUnit.ApplyHit
→ Unit.ApplyHealthAndStaggerHit
→ Unit.ApplyHealthHit에서 Health.TakeDamage를 먼저 호출
→ 살아 있으면 UnitStagger.AddStaggerDamage 호출
→ 파생 WorldUnit이 결과를 비교해 Hit 상태 전환
```

현재 확인된 문제와 근거:

| 현재 코드 | 문제 | 바꿀 위치 |
|---|---|---|
| `Unit`은 `UnitHealth`만 소유 | 경직 Reset·Update가 파생 Unit마다 반복됨 | `Unit.cs`가 Health·Stagger·Stamina·DefenseStatus 소유 |
| `ApplyHealthHit`이 결과 계산 중 `TakeDamage` 호출 | 결과 확정 전 상태가 부분 변경됨 | `AttackHitResultCalculator` 계산 후 `ApplyAttackHitResult` 반영 |
| `AttackHitResult`가 enum | 실제 적용 수치와 반응 정보를 다시 추측해야 함 | `AttackHitResultType` enum + `AttackHitResult` readonly struct |
| `Dodged`, `Blocked`가 enum에만 존재 | Roll·Block 상태가 판정으로 연결되지 않음 | `UnitDefenseStatus`, Animation Event, Block Enter·Exit 연결 |
| Player·Zombie는 `Staggered`만 Hit, Nightshade는 `Damaged`도 Hit | 같은 결과 이름의 행동 의미가 Unit마다 다름 | 결과 종류별 공통 전환 표 사용 |
| `CombatHitResolver`는 Receiver 참조만 저장 | Disable 또는 풀 재사용 후 옛 타격이 적용될 수 있음 | 대상 `ActivationSequence`, 공격 실행 번호를 함께 저장 |
| `MeleeHitDetector`가 CombatHitResolver를 필요할 때 다시 Scene 검색 | 타격 중 Scene 검색이 발생할 수 있음 | Controller/Prefab에서 명시 연결, Awake 검증 |
| 검색 배열 32개가 가득 차도 표시 없음 | Collider 누락을 발견하기 어려움 | Development 빌드에서 공격당 1회 경고 |
| `TakeDamage(float)`가 별도 Hit 상태를 만듦 | 전투 접촉과 환경 피해의 전환 기준이 다름 | 전투는 `ReceiveAttackHit`만 사용, 직접 피해는 환경·테스트 전용 |

## 3. 최종 파일 책임과 이름공간

공통 일반 C# 코드는 기존 `rudIsland.RPG3D.Characters`와 `rudIsland.RPG3D.Combat` 이름공간을 유지한다.

| 파일 | 최종 책임 |
|---|---|
| `Runtime/Characters/Unit.cs` | 공통 전투 값 소유, 생명주기 순서, `ReceiveAttackHit`, 결과 일괄 반영 |
| `Runtime/Characters/UnitHealth.cs` | 체력 감소·회복·Reset과 알림만 담당 |
| `Runtime/Characters/UnitStagger.cs` | 경직 현재값, 회복과 확정 결과 반영 |
| 새 `Runtime/Characters/UnitStamina.cs` | 현재 Stamina, 소비, 회복과 활성화 초기화 |
| 새 `Runtime/Characters/UnitDefenseStatus.cs` | 무적·가드·패리·슈퍼아머 창과 방어 설정 |
| 새 `Runtime/Combat/AttackHitResultType.cs` | 최종 결과 이름만 정의 |
| `Runtime/Combat/AttackHitResult.cs` | 결과 종류와 실제 적용할 수치·반응 보관 |
| `Runtime/Combat/AttackHitInput.cs` | 공격자가 제공하는 피해·방어·연출 입력 보관 |
| 새 `Runtime/Combat/AttackHitResultCalculator.cs` | 대상 값을 읽고 부작용 없이 결과 계산 |
| `Runtime/Combat/IAttackHitReceiver.cs` | 활성 여부, `ActivationSequence`, `ReceiveAttackHit` 계약 |
| `Runtime/Combat/AttackHitQueue.cs` | 같은 프레임 후보, 중복 키와 활성 주기 검증 |
| `Runtime/Combat/MeleeHitDetector.cs` | 스윕·접촉 수집·공격 실행 번호·결과 이벤트 |
| Player/Zombie `Controller` | Inspector·Transform·Animator·Detector와 일반 C# Unit 연결 |
| Player/Zombie `WorldUnit` | Unit 결과를 상태머신 상태 이름으로 전달 |
| Player/Zombie `StateMachine` | 결과에 따른 Hit·GuardBreak·Parried·Knockdown·Dead 상태 전환 |
| Animation Event Receiver | 공격·회피·패리·슈퍼아머의 시작과 종료 이벤트 전달 |

### 3.1 직관적인 네이밍 최종안

목표 호출 흐름은 이름만 읽어도 다음 순서가 보여야 한다.

    AttackHitInput
    → AttackHitResultCalculator.CalculateResult
    → AttackHitResult
    → Unit.ApplyAttackHitResult
    → HandleAttackHitResult

| 변경 전 또는 초기 계획 이름 | 사용할 이름 | 이름으로 드러나는 역할 |
|---|---|---|
| AttackHitData | AttackHitInput | 타격 판정에 들어가는 입력 |
| HitJudge | AttackHitResultCalculator | 공격 타격 결과를 계산 |
| HitResultType | AttackHitResultType | 공격 타격 결과의 종류 |
| UnitHitDefense | UnitDefenseStatus | Unit의 현재 방어 상태 |
| BlockAngle | GuardAngle | 정면 가드로 인정할 각도 |
| BlockStaminaDamage | GuardStaminaDamage | 가드 성공 시 줄어드는 Stamina |
| HitContact | AttackHitContact | 공격과 대상의 접촉 정보 |
| HitStrength | AttackHitStrength | 공격 타격의 세기 |
| HitReaction | AttackHitReaction | 공격 피격 후 사용할 반응 |
| LifeVersion | ActivationSequence | 풀 활성화가 바뀔 때 증가하는 순번 |
| CanReceiveHit | CanTakeHit | 지금 타격을 받을 수 있는지 |
| ReceiveHit | ReceiveAttackHit | 공격 타격 입력을 받음 |
| ApplyHitResult | ApplyAttackHitResult | 확정된 공격 타격 결과를 수치에 반영 |
| OnHitResultApplied | HandleAttackHitResult | 반영된 결과에 맞는 상태 전환 처리 |
| HitResultReady | AttackHitResultReady | 공격자에게 최종 결과가 준비됐음을 알림 |
| CombatHitResolver | AttackHitQueue | 같은 프레임 타격을 모아 순서대로 적용 |
| QueueHit | AddHit | 타격 후보를 대기열에 추가 |
| ResolvePendingHits | ApplyQueuedHits | 모아 둔 타격을 실제 대상에 적용 |
| HitResultEffectPlayer | AttackHitEffectPlayer | 결과에 맞는 타격 효과를 재생 |

다음 이름은 이미 대상과 역할이 분명하므로 유지한다.

| 유지할 이름 | 유지 이유 |
|---|---|
| UnitHealth | Unit의 체력 |
| UnitStamina | Unit의 Stamina |
| UnitStagger | Unit의 경직 누적과 회복 |
| AttackDamage | 공격의 체력 피해 |
| AttackHitResult | 확정된 공격 타격 결과 |
| AttackHitSettings | 공격별 타격 설정 |
| IAttackHitReceiver | 공격 타격을 받는 대상 계약 |
| MeleeHitDetector | 근접 공격 접촉을 찾는 검사기 |
| UnitHitBox | Unit의 피격 접촉 부위 |
| HitPushMovement | 피격 밀림 이동 |

새 이름에 Manager, Helper, Common과 단독 Data를 사용하지 않는다. 기존 코드 이름은 변경 전 흐름과 마이그레이션 설명에서만 사용하고, 목표 코드에서는 사용할 이름으로 통일한다.

`CombatManager`, `DamageManager`, `CombatHelper` 같은 새 범용 클래스는 만들지 않는다. 현재 규모에서는 판정은 `AttackHitResultCalculator`, 값 반영은 `Unit`, Unity 연결은 각 Controller에 두면 책임이 구분된다.

## 4. 목표 타입과 핵심 코드 모양

아래 코드는 한 번에 붙여 넣는 완성 코드가 아니라 각 단계에서 도달할 계약이다. 실제 적용 때는 단계별로 테스트를 먼저 추가하고 필요한 부분만 수정한다.

### 4.1 결과 이름과 결과 값 분리

변경 전:

```csharp
public enum AttackHitResult
{
    Ignored,
    Dodged,
    Blocked,
    Damaged,
    Staggered,
    Killed
}
```

변경 후:

```csharp
public enum AttackHitResultType
{
    Ignored,
    Dodged,
    Parried,
    Blocked,
    GuardBroken,
    Damaged,
    Staggered,
    KnockedDown,
    Killed
}

public readonly struct AttackHitResult
{
    public AttackHitResultType Type { get; }
    public float HealthDamage { get; }
    public float StaminaDamage { get; }
    public float StaggerDamage { get; }
    public AttackHitReaction Reaction { get; }
    public float HitStopTime { get; }

    public bool StopsDefenderAction =>
        Type == AttackHitResultType.Staggered ||
        Type == AttackHitResultType.GuardBroken ||
        Type == AttackHitResultType.KnockedDown ||
        Type == AttackHitResultType.Killed;

    public AttackHitResult(
        AttackHitResultType type,
        float healthDamage,
        float staminaDamage,
        float staggerDamage,
        AttackHitReaction reaction,
        float hitStopTime)
    {
        Type = type;
        HealthDamage = healthDamage;
        StaminaDamage = staminaDamage;
        StaggerDamage = staggerDamage;
        Reaction = reaction;
        HitStopTime = hitStopTime;
    }
}
```

실제 적용 시 생성자에서 NaN, Infinity와 음수를 0으로 정리한다. 자주 쓰는 `Ignored`, `Dodged` 결과는 정적 읽기 값으로 제공할 수 있지만, 결과마다 새 참조 객체를 만들지 않고 값 형식을 사용한다.

### 4.2 공격 입력 값 확장

`AttackHitInput`에 다음 값을 명시적으로 추가한다.

```csharp
public float HealthDamage => Damage.HealthDamage;
public float StaggerDamage { get; }
public float GuardStaminaDamage { get; }
public bool CanBeBlocked { get; }
public bool CanBeParried { get; }
public AttackHitStrength Strength { get; }
public float PushDistance { get; }
public float HitStopTime { get; }
public AttackHitContact Contact { get; }
```

기존 생성자가 `damage.HealthDamage`를 경직 피해 기본값으로 넣는 호환 동작은 단계 2에서 제거한다. 모든 `AttackHitSettings`와 테스트가 `HealthDamage`와 `StaggerDamage`를 각각 전달한 뒤에만 제거한다. 두 값의 암시적 연결을 남기면 체력 밸런스 변경이 경직 밸런스까지 바꾸므로 최종 구조에서는 금지한다.

`AttackHitSettings` 직렬화 필드 추가 순서:

```csharp
[SerializeField, Min(0f)] private float blockStaminaDamage;
[SerializeField] private bool canBeBlocked = true;
[SerializeField] private bool canBeParried = true;
[SerializeField, Min(0f)] private float hitStopTime;
```

기존 `damage`, `staggerDamage`, `strength`, `pushDistance` 필드 이름은 유지해 Scene과 Prefab의 직렬화 값을 보존한다. 새 필드는 기본값을 명시하고 Player·Zombie Inspector 값을 기록한 뒤 Play 검증한다.

### 4.3 UnitStamina

새 파일: `Assets/_Project/Runtime/Characters/UnitStamina.cs`

```csharp
public sealed class UnitStamina
{
    private readonly float recoverDelay;
    private readonly float recoverSpeed;
    private float remainingRecoverDelay;

    public float MaxStamina { get; }
    public float CurrentStamina { get; private set; }

    public UnitStamina(
        float maxStamina,
        float recoverDelay,
        float recoverSpeed)
    {
        // 유한한 양수/0 검증 후 최대값으로 시작한다.
    }

    public bool CanSpend(float amount)
    {
        return amount > 0f && CurrentStamina >= amount;
    }

    public void Spend(float amount)
    {
        // 결과에 확정된 값만 감소시키고 회복 대기를 다시 시작한다.
    }

    public void Update(float deltaTime, bool canRecover)
    {
        // 살아 있고 공격·가드 등 회복 금지 행동이 아닐 때만 회복한다.
    }

    public void Reset()
    {
        CurrentStamina = MaxStamina;
        remainingRecoverDelay = 0f;
    }
}
```

`CanSpend`는 판정에만 사용하고 값을 바꾸지 않는다. 실제 감소는 `Unit.ApplyAttackHitResult`가 `Spend(result.StaminaDamage)`를 한 번 호출한다.

### 4.4 UnitDefenseStatus

새 파일: `Assets/_Project/Runtime/Characters/UnitDefenseStatus.cs`

```csharp
public sealed class UnitDefenseStatus
{
    public bool IsInvincible { get; private set; }
    public bool IsGuarding { get; private set; }
    public bool IsParryWindowOpen { get; private set; }
    public bool IsSuperArmorActive { get; private set; }
    public float GuardAngle { get; }

    public UnitDefenseStatus(float guardAngle)
    {
        GuardAngle = Math.Max(
            0f,
            Math.Min(180f, guardAngle));
    }

    public void StartInvincible() => IsInvincible = true;
    public void StopInvincible() => IsInvincible = false;
    public void StartGuard() => IsGuarding = true;
    public void StopGuard() => IsGuarding = false;
    public void StartParryWindow() => IsParryWindowOpen = true;
    public void StopParryWindow() => IsParryWindowOpen = false;
    public void StartSuperArmor() => IsSuperArmorActive = true;
    public void StopSuperArmor() => IsSuperArmorActive = false;

    public void Reset()
    {
        IsInvincible = false;
        IsGuarding = false;
        IsParryWindowOpen = false;
        IsSuperArmorActive = false;
    }
}
```

`UnitDefenseStatus`에는 Animator, Transform과 Collider를 넣지 않는다. 가드 방향은 피격 순간 Controller가 넘기는 `transform.forward`를 사용하므로 캐시 갱신 시점이 생기지 않는다.

### 4.5 AttackHitResultCalculator

새 파일: `Assets/_Project/Runtime/Combat/AttackHitResultCalculator.cs`

목표 시그니처:

```csharp
public sealed class AttackHitResultCalculator
{
    public AttackHitResult CalculateResult(
        in AttackHitInput hit,
        Unit target,
        Vector3 targetForward)
    {
        // target의 현재 값을 읽기만 하고 절대 변경하지 않는다.
    }
}
```

판정 내부 순서의 의사 코드:

```csharp
if (!target.IsEnabled || target.IsDead)
    return AttackHitResult.Ignored;

if (hit.AttackerTeam == target.Team || !hit.Damage.IsValid)
    return AttackHitResult.Ignored;

if (target.DefenseStatus.IsInvincible)
    return AttackHitResult.Dodged;

if (target.DefenseStatus.IsParryWindowOpen && hit.CanBeParried)
    return AttackHitResult.Parried;

bool isFrontBlock = IsInsideGuardAngle(
    targetForward,
    hit.HitDirection,
    target.DefenseStatus.GuardAngle);

if (target.DefenseStatus.IsGuarding &&
    hit.CanBeBlocked &&
    isFrontBlock)
{
    if (target.Stamina.CanSpend(hit.GuardStaminaDamage))
        return Blocked 결과;

    return GuardBroken 결과;
}

float healthDamage = Math.Min(
    target.Health.CurrentHealth,
    hit.HealthDamage);

if (healthDamage >= target.Health.CurrentHealth)
    return Killed 결과;

if (hit.Strength == AttackHitStrength.Knockdown &&
    !target.DefenseStatus.IsSuperArmorActive)
    return KnockedDown 결과;

if (target.Stagger.WillReachLimit(hit.StaggerDamage) &&
    !target.DefenseStatus.IsSuperArmorActive)
    return Staggered 결과;

return Damaged 결과;
```

가드 각도 계산은 피해자의 정면과 공격이 날아온 쪽을 비교한다.

```csharp
Vector3 incomingDirection = -hit.HitDirection;
incomingDirection.y = 0f;
targetForward.y = 0f;

float minimumDot =
    Mathf.Cos(guardAngle * 0.5f * Mathf.Deg2Rad);
bool isInside = Vector3.Dot(
    targetForward.normalized,
    incomingDirection.normalized) >= minimumDot;
```

길이가 0인 방향은 정면 가드로 인정하지 않는다. 이 규칙으로 등 뒤 공격과 잘못된 접촉 방향이 우연히 막히는 것을 방지한다.

### 4.6 Unit 공통 생명주기

`Unit` 생성자 목표:

```csharp
protected Unit(
    UnitTeam team,
    float maxHealth,
    float staggerLimit,
    float staggerRecoverDelay,
    float staggerRecoverSpeed,
    float maxStamina,
    float staminaRecoverDelay,
    float staminaRecoverSpeed,
    float guardAngle)
{
    Team = team;
    Health = new UnitHealth(maxHealth);
    Stagger = new UnitStagger(
        staggerLimit,
        staggerRecoverDelay,
        staggerRecoverSpeed);
    Stamina = new UnitStamina(
        maxStamina,
        staminaRecoverDelay,
        staminaRecoverSpeed);
    DefenseStatus = new UnitDefenseStatus(guardAngle);
    hitResultCalculator = new AttackHitResultCalculator();
}
```

공개 읽기 값:

```csharp
public UnitHealth Health { get; }
public UnitStagger Stagger { get; }
public UnitStamina Stamina { get; }
public UnitDefenseStatus DefenseStatus { get; }
public int ActivationSequence { get; private set; }
public bool CanTakeHit => IsEnabled && !IsDead;
```

생명주기 순서:

```csharp
protected sealed override void OnCreate()
{
    // 공통 이벤트는 여기서 한 번만 연결한다.
    OnUnitCreate();
}

protected sealed override void OnEnable()
{
    IncreaseActivationSequence();
    DefenseStatus.Reset();
    Stagger.Reset();
    OnUnitResourceEnable();
    OnUnitEnable();
}

protected sealed override void OnTick(float deltaTime)
{
    if (!IsDead)
    {
        Stagger.Update(deltaTime);
        Stamina.Update(
            deltaTime,
            CanRecoverStamina());
    }

    OnUnitTick(deltaTime);
}

protected sealed override void OnDisable()
{
    DefenseStatus.Reset();
    Stagger.Reset();
    OnUnitDisable();
}

protected sealed override void OnDispose()
{
    DefenseStatus.Reset();
    OnUnitDispose();
    Health.ClearListeners();
}
```

`ActivationSequence`는 0을 사용하지 않고 `int.MaxValue` 다음에 1로 돌아간다. `Enable`할 때만 증가한다. 같은 활성 주기에서 잠깐 공격 상태가 끝난 것은 ActivationSequence를 바꾸지 않는다.

자원 활성화 정책:

```csharp
// PlayerUnit: 재활성화로 Health를 Reset하지 않는다.
protected override void OnUnitResourceEnable()
{
    Stamina.Reset();
}

// EnemyUnit: 풀에서 새로 나올 때 Health와 Stamina를 Reset한다.
protected override void OnUnitResourceEnable()
{
    Health.Reset();
    Stamina.Reset();
}
```

현재 `PlayerWorldUnit`이 `Unit`을 직접 상속하고 있으므로 1단계에서 `PlayerUnit` 상속으로 바꾼다. 이 변경은 팀 인수를 중복 전달하지 않게 하고 플레이어 체력 정책을 한곳에 둔다.

### 4.7 ReceiveAttackHit와 결과 반영

변경 전:

```csharp
AttackHitResult hitResult =
    ApplyHealthAndStaggerHit(in hit, unitStagger);

if (hitResult == AttackHitResult.Staggered)
{
    stateMachine.ChangeToHitState(in hit);
}
```

변경 후 공통 Unit:

```csharp
public AttackHitResult ReceiveAttackHit(
    in AttackHitInput hit,
    Vector3 targetForward)
{
    AttackHitResult result = hitResultCalculator.CalculateResult(
        in hit,
        this,
        targetForward);

    ApplyAttackHitResult(in result);
    HandleAttackHitResult(in result);
    return result;
}

private void ApplyAttackHitResult(
    in AttackHitResult result)
{
    if (result.HealthDamage > 0f)
        Health.TakeDamage(result.HealthDamage);

    if (result.StaminaDamage > 0f)
        Stamina.Spend(result.StaminaDamage);

    if (result.StaggerDamage > 0f)
    {
        Stagger.ApplyConfirmedDamage(
            result.StaggerDamage,
            result.Type == AttackHitResultType.Staggered ||
            result.Type == AttackHitResultType.KnockedDown);
    }
}
```

`HandleAttackHitResult`은 파생 Unit의 상태머신 연결 지점이다. 파생 Unit은 수치를 다시 계산하지 않는다.

```csharp
protected override void HandleAttackHitResult(
    in AttackHitResult result)
{
    switch (result.Type)
    {
        case AttackHitResultType.Staggered:
            stateMachine.ChangeToHitState(
                in result.Reaction);
            break;
        case AttackHitResultType.GuardBroken:
            stateMachine.ChangeToGuardBreakState();
            break;
        case AttackHitResultType.KnockedDown:
            stateMachine.ChangeToKnockdownState(
                in result.Reaction);
            break;
        // Killed는 UnitHealth.Died 이벤트가 Dead 상태를 선택한다.
    }
}
```

`Damaged`, `Blocked`, `Dodged`는 기본적으로 현재 행동을 끊지 않는다. Block 애니메이션 충격과 회피 효과는 상태 전환이 아니라 결과 연출 소비자가 처리한다.

### 4.8 피격 수신 계약과 풀 활성 주기

변경 후 `IAttackHitReceiver`:

```csharp
public interface IAttackHitReceiver
{
    bool CanTakeHit { get; }
    int ActivationSequence { get; }
    AttackHitResult ReceiveAttackHit(
        in AttackHitInput hit);
}
```

Controller는 Unity Transform 방향을 일반 C# Unit으로 넘긴다.

```csharp
public bool CanTakeHit =>
    playerWorldUnit != null &&
    playerWorldUnit.CanTakeHit;

public int ActivationSequence =>
    playerWorldUnit != null
        ? playerWorldUnit.ActivationSequence
        : 0;

public AttackHitResult ReceiveAttackHit(
    in AttackHitInput hit)
{
    if (!CanTakeHit)
        return AttackHitResult.Ignored;

    return playerWorldUnit.ReceiveAttackHit(
        in hit,
        transform.forward);
}
```

Zombie, Nightshade와 이후 Unit Controller도 같은 형태를 사용한다.

`AttackHitQueue.QueuedHit`에 다음 값을 추가한다.

```csharp
internal int AttackSequence { get; }
internal int TargetActivationSequence { get; }
```

추가 시점:

```csharp
if (!receiver.CanTakeHit)
    return false;

queuedHits.Add(new QueuedHit(
    sourceDetector,
    attackSequence,
    receiver,
    receiver.ActivationSequence,
    hit));
```

적용 시점:

```csharp
if (!receiver.CanTakeHit ||
    receiver.ActivationSequence !=
        queuedHit.TargetActivationSequence ||
    !sourceDetector.MatchesAttackSequence(
        queuedHit.AttackSequence))
{
    return;
}
```

공격 실행 번호 규칙:

- `StartHit`은 번호를 증가시킨다.
- 새 공격이 시작되면 이전 공격의 대기 중 타격은 번호 불일치로 폐기한다.
- 일반 `EndHit`은 번호를 바꾸지 않는다. 그래야 같은 프레임에 이미 확정 대기열에 들어간 양쪽 타격이 모두 적용된다.
- Detector `OnDisable`은 `EndHit` 후 번호를 한 번 더 증가시켜 풀 재사용 전 대기 타격을 폐기한다.

이 구분 없이 `EndHit`에서 항상 번호를 바꾸면 먼저 처리된 `Staggered`가 상대 Detector를 닫는 순간, 이미 같은 프레임에 들어간 상대 타격까지 사라져 기존 동시 타격 계약을 깨뜨린다.

## 5. 단계별 실제 수정 순서

각 단계는 아래 파일만 수정하고 완료 조건을 통과한 뒤 다음 단계로 넘어간다.

### 새 0단계: 목표 계약과 기준선 고정

수정 파일:

- 이 로드맵 문서만 수정
- 전투 `.cs`, Scene과 Prefab은 수정하지 않음

기준선 기록 항목:

1. 현재 작업 트리의 Player 카메라, `PlayerController`, Scene과 Prefab 변경 목록을 `git status --short`로 기록한다.
2. Unity Console 컴파일 오류 수를 기록한다.
3. EditMode 전체 테스트 결과와 전투 테스트 개수를 기록한다.
4. Unity Play에서 Player 1회 공격 → Zombie 체력·경직·상태 결과를 기록한다.
5. Zombie 1회 공격 → Player 체력·경직·상태 결과를 기록한다.
6. 같은 프레임 양쪽 공격 결과를 기록한다.
7. 비활성 Zombie에 CombatHitResolver 대기 타격을 넣은 뒤 현재 체력이 줄어드는지 확인한다.
8. Profiler에서 공격 활성 구간 GC Alloc과 물리 검색 횟수를 기록한다.

완료 조건:

- 현재 동작과 실패 사례가 숫자와 결과 이름으로 기록돼 있다.
- 카메라와 Scene 사용자 변경의 diff를 보존할 범위가 구분돼 있다.
- 이후 단계가 비교할 테스트 명령과 Play 절차가 정해져 있다.

### 새 1단계: Unit 피격 생명주기 통합

수정 파일:

- `Runtime/Characters/Unit.cs`
- `Runtime/Characters/PlayerUnit.cs`
- `Runtime/Characters/EnemyUnit.cs`
- 새 `Runtime/Characters/UnitStamina.cs`
- 새 `Runtime/Characters/UnitDefenseStatus.cs`
- `PlayerWorldUnit.cs`
- `ZombieWorldUnit.cs`
- `NightshadeSpearWorldUnit.cs`는 공통 생성자 컴파일 연결까지만 수정
- `UnitStaggerTests.cs`
- 새 `UnitStaminaTests.cs`
- 새 `UnitDefenseStatusTests.cs`
- 새 `UnitLifecycleTests.cs`

변경 순서:

1. `UnitStamina`, `UnitDefenseStatus`를 독립 일반 C# 클래스로 추가한다.
2. `Unit`이 Health·Stagger·Stamina·DefenseStatus를 생성하고 소유하도록 생성자를 확장한다.
3. `Unit.OnEnable`에서 ActivationSequence 증가 → DefenseStatus Reset → Stagger Reset → 자원 정책 → 파생 Enable 순서를 고정한다.
4. `Unit.OnTick`에서 Stagger 회복 → Stamina 회복 → 파생 입력·AI 갱신 순서를 고정한다.
5. `Unit.OnDisable`에서 DefenseStatus 창과 Stagger를 닫은 뒤 파생 상태머신을 중단한다.
6. Player는 `PlayerUnit`, 적은 `EnemyUnit`의 자원 Reset 정책을 사용한다.
7. Player·Zombie·Nightshade에 있던 `unitStagger/stagger` 필드와 Reset·Update 중복을 제거한다.

중요한 변경 전후:

```text
변경 전: 파생 WorldUnit 생성자마다 new UnitStagger
변경 후: Unit 생성자 한 곳에서 new UnitStagger

변경 전: 파생 OnEnable에서 unitStagger.Reset
변경 후: Unit.OnEnable에서 공통 Reset

변경 전: 파생 OnTick에서 unitStagger.Update
변경 후: Unit.OnTick에서 공통 Update 후 파생 상태머신 Update
```

Player 정책:

- Disable/Enable만으로 Health를 회복하지 않는다.
- 죽은 상태에서 Enable되면 기존처럼 Dead 상태를 복구한다.
- Stamina, 방어 창과 경직은 활성화할 때 새 행동 주기로 초기화한다.

Enemy 정책:

- 풀 Enable 때 Health와 Stamina를 최대값으로 복구한다.
- ActivationSequence를 올려 이전 활성 주기 타격을 구분한다.
- StateMachine Enable 전에 자원과 방어 상태 초기화를 끝낸다.

자동 테스트 이름:

- `Enable_IncreasesActivationSequence`
- `Enable_ResetsDefenseAndStagger`
- `PlayerEnable_DoesNotResetHealth`
- `EnemyEnable_ResetsHealthAndStamina`
- `Tick_UpdatesResourcesBeforeDerivedTick`
- `Disable_ClosesAllDefenseWindows`
- `Dispose_CanBeCalledTwice`

Unity 확인:

- Zombie 풀 회수·재소환 후 체력 최대, 경직 0, 가드·무적 false
- Player GameObject Disable·Enable 후 이전 체력 유지
- Disable 중 입력·AI·피격 이동이 멈춤

### 새 2단계: 계산과 상태 반영 분리

수정 파일:

- 새 `Runtime/Combat/AttackHitResultType.cs`
- `Runtime/Combat/AttackHitResult.cs`
- Runtime/Combat/AttackHitData.cs → AttackHitInput.cs로 이름 변경, meta GUID 유지
- 새 `Runtime/Combat/AttackHitResultCalculator.cs`
- `Runtime/Characters/Unit.cs`
- `Runtime/Characters/UnitStagger.cs`
- `Runtime/Combat/IAttackHitReceiver.cs`
- Player·Zombie Controller와 WorldUnit
- Nightshade Controller와 WorldUnit은 컴파일 연결 및 결과 기준 통일
- AttackHitDataTests.cs → AttackHitInputTests.cs로 이름 변경
- `UnitHitResultTests.cs`
- 새 `AttackHitResultCalculatorTests.cs`

변경 순서:

1. 기존 `AttackHitResult` enum을 `AttackHitResultType`으로 옮긴다.
2. 같은 파일 이름의 `AttackHitResult`를 readonly struct로 만든다.
3. 기존 비교 `hitResult == AttackHitResult.Staggered`를 `hitResult.Type == AttackHitResultType.Staggered`로 변경한다.
4. `AttackHitResultCalculator`가 유효성·체력·경직 결과를 계산하되 어떤 값도 변경하지 않게 테스트한다.
5. `Unit.ReceiveAttackHit → AttackHitResultCalculator.CalculateResult → Unit.ApplyAttackHitResult → HandleAttackHitResult` 순서를 추가한다.
6. `ApplyHealthHit`, `ApplyHealthAndStaggerHit` 호출을 Player·Zombie·Nightshade에서 제거한다.
7. 모든 전투 접촉은 새 `ReceiveAttackHit`만 사용한다.
8. `TakeDamage(float)`는 Context Menu 테스트와 환경 피해 전용임을 주석과 이름으로 구분하고 전투 Controller에서는 호출하지 않는다.

Player·Zombie·Nightshade 공통 상태 기준:

| 결과 | 체력 | 행동 전환 |
|---|---:|---|
| `Ignored` | 변화 없음 | 없음 |
| `Damaged` | 감소 | 현재 행동 유지 |
| `Staggered` | 감소 | Hit 상태 |
| `Killed` | 0 | Dead 상태, Hit 상태 금지 |

Nightshade의 현재 `Damaged`에서도 `ChangeToHitState`를 호출하는 분기는 제거한다. 체력 비율과 Phase 갱신은 `Damaged`, `Staggered`, `KnockedDown` 결과를 반영한 뒤 실행하되, Hit 상태 전환은 결과 표를 따른다.

자동 테스트 이름:

- `CalculateResult_DoesNotChangeTargetValues`
- `SameInput_ReturnsSameResult`
- `InvalidAttack_ReturnsIgnored`
- `FriendlyAttack_ReturnsIgnored`
- `DeadTarget_ReturnsIgnored`
- `LethalDamage_ReturnsKilledBeforeStaggered`
- `HealthDamageAndStaggerDamage_AreIndependent`
- `ApplyAttackHitResult_ChangesEachValueOnce`
- `NightshadeDamaged_DoesNotEnterHitState`

### 새 3단계: 회피·가드·패리

수정 파일:

- `UnitDefenseStatus.cs`
- `AttackHitResultCalculator.cs`
- `AttackHitInput.cs`
- `AttackHitSettings.cs`
- `PlayerController.cs`의 Stamina·가드 각도 Inspector 값과 방어 창 전달 부분
- `PlayerWorldUnit.cs`
- `PlayerBlockState.cs`
- `PlayerRollState.cs`
- `PlayerAnimationEventReceiver.cs`
- `PlayerAnimationController.cs`
- `PlayerStateMachine.cs`
- 새 `PlayerGuardBreakState.cs`
- 패리 공격자 반응을 위한 새 `PlayerParriedState.cs`
- Player Roll Clip Animation Event
- `AttackHitResultCalculatorTests.cs`, `UnitDefenseStatusTests.cs`, Player PlayMode 테스트

Block 연결 전후:

```csharp
// 변경 전
public void Enter()
{
    animationController.StopMove();
    animationController.SetBlocking(true);
}

// 변경 후
public void Enter()
{
    stateMachine.StartGuard();
    animationController.StopMove();
    animationController.SetBlocking(true);
}

public void Exit()
{
    stateMachine.StopGuard();
    animationController.StopMove();
    animationController.SetBlocking(false);
}
```

`PlayerStateMachine.StartGuard/StopGuard`는 Controller 콜백을 거쳐 `PlayerWorldUnit.DefenseStatus`를 열고 닫는다. State가 일반 C# Unit을 직접 찾는 숨은 의존성을 만들지 않도록 생성자에서 명시적 `Action`을 받거나 StateMachine의 명시 메서드로 전달한다.

Roll 무적은 상태 전체가 아니라 Animation Event로 연다.

```csharp
public void StartRollInvincibleAnimationEvent()
{
    playerController?.OpenRollInvincible();
}

public void EndRollInvincibleAnimationEvent()
{
    playerController?.CloseRollInvincible();
}
```

모든 Roll Clip에 같은 역할의 Event 두 개를 넣는다. 시작·끝 시간을 감으로 통일하지 않고 각 Clip에서 몸이 회전해 공격을 피하는 실제 중간 구간에 배치한다. `PlayerRollState.Exit`, Player Disable과 Dead 전환에서도 `CloseRollInvincible`을 호출해 Event 누락을 방어한다.

패리 연결:

- 첫 단계에서는 Player의 패리 입력과 애니메이션 자산이 확정된 경우에만 창을 연결한다.
- 패리 창 시작·종료는 Animation Event로 연결한다.
- `hit.CanBeParried == false`면 패리 창 안에서도 다음 가드 또는 체력 피해 판정으로 내려간다.
- `Parried` 결과는 피해자가 아니라 공격자의 `MeleeHitDetector.AttackHitResultReady`로 돌아간다.
- 공격자 Controller가 `Parried`를 받으면 Detector와 `AttackPhaseTracker`를 종료하고 Parried 상태로 전환한다.

Guard Break:

```text
현재 Stamina >= GuardStaminaDamage
→ Blocked
→ 체력 0 감소, Stamina 감소, Block Hit 연출

현재 Stamina < GuardStaminaDamage
→ GuardBroken
→ 남은 Stamina 0, 공격 종료, GuardBreak 상태
```

Guard Break 때 체력 피해를 함께 줄지는 첫 구현에서 금지한다. 먼저 결과 의미를 단순하게 고정하고, 이후 관통 피해가 필요하면 `GuardHealthDamageRate`라는 명시 설정으로 별도 확장한다.

자동 테스트 이름:

- `InvincibleTarget_ReturnsDodged`
- `ParryWindowAndParryableAttack_ReturnsParried`
- `UnparryableAttack_DoesNotReturnParried`
- `FrontGuardWithEnoughStamina_ReturnsBlocked`
- `BackAttack_DoesNotReturnBlocked`
- `GuardWithoutEnoughStamina_ReturnsGuardBroken`
- `Dodged_DoesNotChangeResources`
- `Blocked_ChangesOnlyStamina`
- `ExitAndDisable_CloseDefenseWindows`

Unity 확인:

- Roll 시작과 끝은 맞고 중간 Event 구간만 `Dodged`
- 정면 공격은 `Blocked`, 등 뒤 공격은 `Damaged` 또는 `Staggered`
- Stamina 부족 시 Guard Break 애니메이션과 입력 잠금
- 패리 불가 Heavy/Knockdown 공격은 패리 창을 통과

### 새 4단계: 강인도·슈퍼아머·피격 상태 통일

수정 파일:

- `UnitStagger.cs`
- `UnitDefenseStatus.cs`
- `AttackHitResultCalculator.cs`
- `AttackHitResult.cs`
- Player·Zombie·Nightshade StateMachine과 HitState
- 새 Unit별 Knockdown State
- 필요한 Unit의 Attack State와 Animation Event Receiver
- `AttackPhaseTracker.cs`의 취소 규칙 연결
- 경직·슈퍼아머 EditMode와 PlayMode 테스트

결과별 행동 규칙:

```text
Damaged       → 체력 감소, 행동 유지
Staggered     → 체력 감소, 공격 판정 종료, Hit 상태
KnockedDown   → 체력 감소, 공격 판정 종료, Knockdown 상태
Killed        → 체력 0, Dead 상태
```

슈퍼아머:

- 공격 준비·타격 중 설정한 구간에서만 `StartSuperArmor`를 호출한다.
- Exit, Disable, Dead에서 반드시 `StopSuperArmor`를 호출한다.
- 슈퍼아머 중에도 체력과 경직 수치는 반영한다.
- 첫 구현에서는 슈퍼아머가 `Staggered`를 `Damaged`로 바꾸고, `Knockdown` 허용 여부는 Unit 설정으로 분리한다.
- Boss 여부를 `if (IsBoss)`로 판정 안에 박지 않는다. `staggerLimit`, `allowsKnockdown`, `superArmor` 설정으로 표현한다.

Knockdown은 기존 Hit 상태로 보내지 않는다. 전용 상태와 클립이 없는 Unit은 `allowsKnockdown = false`로 시작하고 Heavy 또는 Staggered까지만 허용한다. 데이터는 Knockdown인데 일반 Hit 애니메이션을 재생하는 임시 처리는 만들지 않는다.

`AttackPhaseTracker` 재검증:

- Ready 중 `Staggered`: 공격 취소
- Hit 중 `Damaged`: 공격 유지
- Hit 중 `Staggered`: 공격 취소
- Recovery 중 `Damaged`: Recovery 유지
- 모든 구간 `Killed`: 즉시 종료
- `Parried`: 공격자 Tracker 종료 후 Parried 상태

자동 테스트 이름:

- `Damaged_DoesNotStopAction`
- `Staggered_StopsAction`
- `SuperArmor_ChangesStaggeredToDamaged`
- `SuperArmor_DoesNotPreventHealthDamage`
- `Knockdown_UsesSeparateResult`
- `Killed_OverridesKnockdown`
- `PlayerZombieNightshade_UseSameStopRule`

### 새 5단계: 공격 판정과 풀 생명주기 보완

수정 파일:

- `IAttackHitReceiver.cs`
- CombatHitResolver.cs → AttackHitQueue.cs로 이름 변경, meta GUID 유지
- `MeleeHitDetector.cs`
- 각 Controller의 Receiver 속성
- Prefab/Scene의 `AttackHitQueue` 명시 참조
- CombatHitResolverTests.cs → AttackHitQueueTests.cs로 이름 변경
- `MeleeHitDetectorTests.cs`
- 물리 EditMode/PlayMode 테스트

구체 변경:

1. AttackHitQueue의 `QueuedHit`에 공격 실행 번호와 대상 ActivationSequence를 저장한다.
2. AddHit과 ApplyQueuedHits 두 시점에서 `CanTakeHit`를 검사한다.
3. ApplyQueuedHits에서 대상 ActivationSequence와 Detector 공격 실행 번호를 재확인한다.
4. Detector는 Controller 또는 상위 Scene Root가 전달한 AttackHitQueue를 캐시한다.
5. `TryApplyHit` 중 AttackHitQueue가 null일 때 `FindObjectsByType`를 다시 호출하지 않는다.
6. Awake/OnEnable 검증에서만 같은 Scene의 AttackHitQueue를 찾는 호환 경로를 유지하고 Development 환경에 명시 연결 필요 경고를 낸다.
7. Overlap/Cast 반환 개수가 배열 길이와 같으면 Development 환경에서 Detector당 공격 1회만 경고한다.
8. 공격 종료와 Disable의 공격 실행 번호 처리 차이를 테스트한다.

검색 배열 진단 코드 모양:

```csharp
private void WarnIfSearchBufferIsFull(
    int foundCount)
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    if (foundCount < MaxDetectedColliderCount ||
        warnedAttackSequence == currentAttackSequence)
    {
        return;
    }

    warnedAttackSequence = currentAttackSequence;
    Debug.LogWarning(SearchBufferFullMessage, this);
#endif
}
```

`SearchBufferFullMessage`는 고정 문자열 상수로 준비한다. 경고는 배열이 가득 찬 공격에서 한 번만 호출하므로 정상 타격 프레임의 GC 목표에 영향을 주지 않는다.

빠른 회전 보완:

- 현재 Capsule 시작·중간·끝 Sweep은 유지한다.
- 이전 검 방향과 현재 검 방향의 각도를 계산한다.
- 각도가 설정한 한계를 넘을 때만 중간 자세를 1~3회 제한적으로 보간한다.
- 보간 횟수 상한을 두어 프레임 저하가 물리 검색 폭증으로 이어지지 않게 한다.
- 먼저 `HitSpeed`와 각도 데이터를 기록하고 15/30/60 FPS 누락이 재현될 때만 중간 Sweep을 활성화한다.

`HitSpeed`는 이동 거리 / `Time.deltaTime`으로 계산하되 0, NaN과 Infinity는 0으로 정리한다. 이 단계에서는 로그·테스트 데이터로만 사용하고 피해나 경직 계산에는 사용하지 않는다.

자동 테스트 이름:

- `QueuedHit_TargetDisabledBeforeApply_IsDiscarded`
- `QueuedHit_TargetReenabledWithNewActivationSequence_IsDiscarded`
- `QueuedHit_NewAttackStartedBeforeApply_IsDiscarded`
- `QueuedHit_EndHitBeforeApply_IsStillApplied`
- `DetectorDisable_InvalidatesQueuedHit`
- `SameAttackMultipleHitBoxes_AppliesOnce`
- `NextAttack_CanHitAgain`
- `FullSearchBuffer_ReportsDevelopmentWarningOnce`
- `FastRotation_HitsAt15_30_60FpsEquivalentSteps`

### 새 6단계: 피격 이동과 결과 연출 분리

수정 파일:

- `HitPushMovement.cs`
- Player/Zombie Movement와 Hit/GuardBreak/Knockdown State
- 새 `AttackHitEffectPlayer.cs` 또는 역할이 더 분명한 Unit별 효과 경계 컴포넌트
- Controller의 `AttackHitResultReady` 구독
- VFX·Audio Pool
- Cinemachine 반응 연결
- 이동·연출 EditMode와 PlayMode 테스트

피격 이동:

- 자세와 몸의 흔들림은 Animator가 담당한다.
- 실제 수평 밀림과 벽 충돌은 `CharacterController.Move`가 담당한다.
- 매 프레임 요청 이동과 실제 이동 차이를 비교해 벽 충돌을 확인한다.
- 벽에 막힌 방향의 남은 밀림은 계속 누적하지 않고 종료한다.
- 새 피격이 들어오면 이전 밀림을 더하지 않고 확정된 새 결과로 다시 시작한다.

연출 입력:

```text
AttackHitResult.Type
AttackHitResult.Reaction.Strength
AttackHitResult.Reaction.Direction
AttackHitInput.HitPoint
AttackHitInput.HitNormal
AttackHitResult.HitStopTime
```

연출 출력:

- `Blocked`: 방패 불꽃·금속음·약한 카메라 반응
- `Parried`: 강한 금속음·공격자 경직·짧은 Hit Stop
- `Damaged`: 접촉 부위 VFX·피격음
- `Staggered`: 강한 피격 VFX·더 큰 카메라 반응
- `GuardBroken`, `KnockedDown`: 전용 효과와 소리
- `Dodged`, `Ignored`: 체력 피격 VFX 없음

Hit Stop은 전역 `Time.timeScale`을 바꾸지 않는다. 공격자와 피해자의 Animator/상태 갱신을 짧게 멈추는 별도 타이머를 Controller 경계에 둔다. 타이머가 끝나거나 Unit이 Disable되면 반드시 원래 재생 속도와 갱신 상태를 복구한다.

`MeleeHitDetector.AttackHitResultReady` 구독은 공격마다 람다를 만들지 않는다. Controller Awake/Create에서 메서드 그룹으로 한 번 연결하고 OnDestroy/Dispose에서 해제한다. 여러 설정이 같은 Detector를 참조할 수 있으므로 중복 구독을 막는다.

자동 테스트 이름:

- `HitPush_StopsAtWall`
- `NewHit_ReplacesRemainingPush`
- `EffectFailure_DoesNotChangeHitResult`
- `HitStop_DoesNotChangeDamage`
- `Disable_RestoresPausedAnimator`
- `Controller_SubscribeAndUnsubscribeOnce`

Profiler 확인:

- 타격 활성 구간 관리 힙 할당 0 B 목표
- VFX·Audio Instantiate/Destroy 없음
- 이벤트 중복 구독 없음
- 반복 `GetComponent`와 Scene 검색 없음

### 새 7단계: 다른 Unit 확장

적용 순서:

```text
Player ↔ Zombie
→ Nightshade
→ Mummy Warrior
→ Demon Swordsman
→ 나머지 Unit
```

Unit마다 제공할 값:

- 최대 Health
- 최대 Stamina와 회복 규칙
- StaggerLimit과 회복 규칙
- GuardAngle
- 가드·패리 사용 여부
- Knockdown 허용 여부
- 공격별 HealthDamage, StaggerDamage와 GuardStaminaDamage
- 결과에 대응할 상태와 애니메이션
- 풀 Enable 때 자원 Reset 정책

Unit마다 새로 만들면 안 되는 것:

- 별도 체력·가드·경직 판정 함수
- `if (boss)`가 반복되는 공통 판정 복사본
- Unit 전용 `AttackHitResult` 의미
- Controller에서 직접 Health와 Stamina를 변경하는 코드

확장 파일 순서:

1. 해당 Controller가 `CanTakeHit`, `ActivationSequence`, `ReceiveAttackHit`를 공통 Unit에 전달한다.
2. 해당 WorldUnit 생성자를 공통 자원 설정에 연결한다.
3. 파생 WorldUnit의 `ApplyHit` 계산을 제거하고 결과 상태 전환만 남긴다.
4. 해당 StateMachine에 실제 보유한 결과 상태만 연결한다.
5. 공격 설정에 가드·패리·Stamina·Hit Stop 값을 명시한다.
6. Prefab Inspector와 Animation Event를 연결한다.
7. 공통 계약 테스트를 같은 기대 결과로 실행한다.

Nightshade 첫 확인:

- 현재 `Damaged`에서도 Hit 상태로 가는 코드를 제거한다.
- Phase 체력 비율 갱신은 유지한다.
- 높은 StaggerLimit과 공격 구간 슈퍼아머로 약한 공격 연속 경직을 막는다.

Mummy Warrior 첫 확인:

- 기존 `AttackHitData` 호환 생성자에 의존하는 호출을 명시적 StaggerDamage로 교체한다.
- 제한적인 Dead 처리와 풀 회수 순서를 먼저 기록한다.

Demon Swordsman 첫 확인:

- Sword/Beast 자세별 Hit·Death 자산을 결과 상태에 매핑한다.
- Boss 전용 판정 코드를 만들지 않고 StaggerLimit, 슈퍼아머 구간과 허용 Knockdown으로 차이를 표현한다.

## 6. 단계별 완료 기록 형식

각 단계가 끝날 때 아래 형식으로 이 문서에 추가한다. 하나라도 확인하지 못한 항목은 `미확인`으로 적고 단계를 완료 처리하지 않는다.

```text
단계:
목표 흐름:

입력:
판정:
상태 변경:
출력·연출:
Unit 생명주기:

수정한 파일:
자동 테스트:
Unity Play 확인:
Profiler 확인:

목표와 다른 점:
수정 후 재검증:
다음 단계:
```

공통 검토 질문:

1. 입력 데이터가 판정 전에 상태를 변경하지 않는가?
2. 모든 전투 접촉이 `AttackHitResultCalculator → AttackHitResult → Unit.ApplyAttackHitResult` 순서를 지키는가?
3. 파생 Unit이 공통 판정을 다시 구현하지 않는가?
4. Disable·Dispose·풀 재사용 이후 타격과 이벤트가 남지 않는가?
5. 애니메이션과 효과가 체력·가드·경직 결과를 결정하지 않는가?
6. Update·LateUpdate 타격 구간에 반복 할당과 `GetComponent`가 없는가?
7. Player와 Enemy가 같은 결과 이름을 같은 의미로 사용하는가?
8. `Killed`가 Hit, GuardBreak와 Knockdown 상태보다 먼저 처리되는가?
9. 기존 Player 카메라와 Scene 사용자 변경을 덮어쓰지 않았는가?

## 7. 첫 구현을 시작할 때의 실제 작업 순서

다음 구현 요청을 받으면 한 번에 0~7단계를 모두 수정하지 않는다.

```text
1. git status와 현재 diff 보존 범위 기록
2. 새 0단계 기준선 테스트와 Unity Play 결과 기록
3. 새 1단계 UnitStamina + UnitDefenseStatus 단위 테스트 추가
4. Unit 공통 생명주기에 연결
5. Player ↔ Zombie만 컴파일·EditMode·Play 검증
6. 새 1단계 완료 기록 작성
7. 새 2단계 AttackHitResultCalculator 테스트부터 시작
```

첫 구현에서 수정하지 않을 범위:

- `Assets/_Project/Characters/Player/Camera/PlayerTargetCamera.cs`
- 카메라 폴더의 사용자 작업
- 전투 연결과 무관한 `PlayerController` 카메라 필드·생성 코드
- `CharacterTestScene`의 전투 연결과 관계없는 오브젝트
- Demon Swordsman과 Zombie Prefab의 전투 설정과 무관한 사용자 변경
- VFX·Audio·Cinemachine 연출 자산

이 문서의 코드 예시는 구현 시작 전에 현재 파일을 다시 읽고 실제 시그니처와 직렬화 상태를 확인한 뒤 작은 diff로 적용한다.
