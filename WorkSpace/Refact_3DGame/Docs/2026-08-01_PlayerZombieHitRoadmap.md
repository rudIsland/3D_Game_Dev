# Unit 현실적 타격·피격 전투 리팩토링 로드맵

- 최초 작성: 2026-08-01
- 현재 목표로 재작성: 2026-08-02
- 개발 씬: `Assets/_Project/Scenes/Dev/CharacterTestScene.unity`
- 첫 적용 대상: `Player ↔ Zombie`
- 진행 규칙: 한 단계씩 구현하고 확인한다. 사용자가 goal 연속 진행을 요청한 동안에는 현재 단계 검증을 끝낸 뒤 다음 단계로 계속 진행한다.

## 1. 목표

소울류 액션 RPG에 필요한 현실적인 접촉과 신체 반응을 목표로 전투 구조를 다시 만든다.

현재 구현을 최종 구조로 가정하지 않는다. 기존 기능은 검증 가능한 중간 단계로만 보존하고, 이동 충돌과 피격 몸체를 분리한 뒤 무기 이동 경로, 신체 접촉, 충격 결정과 피격 반응이 서로 독립적으로 확장되는 구조로 옮긴다.

이 로드맵에서 말하는 현실감은 모든 캐릭터를 Ragdoll로 움직이는 물리 시뮬레이션이 아니다. 화면에서 보이는 무기와 몸의 접촉, 공격 세기와 방향, 행동 중단 여부와 신체 반응이 같은 결과를 나타내는 것을 의미한다.

전체 목표 흐름:

```text
공격 입력 또는 AI 판단
→ 공격 준비
→ 실제 타격 구간
→ 무기·주먹·발의 이전 위치와 현재 위치 검사
→ 신체 HitBox와 접촉
→ HitContact 생성
→ 공격 설정과 접촉 결과로 충격 결정
→ 피해·경직·넘어짐·사망 판정
→ 방향과 신체 부위에 맞는 피격 반응
→ 공격자와 피해자의 짧은 반응
→ 타격 효과·소리·카메라 반응
→ 공격 후딜 또는 다음 행동
```

## 2. 반드시 지킬 진행 규칙

1. 한 번에 한 단계만 구현한다.
2. 현재 단계의 컴파일, 자동 테스트와 Unity Play 확인이 끝나기 전에는 다음 단계로 넘어가지 않는다.
3. 코드 수정 전에는 현재 입력 → 처리 → 출력 흐름을 먼저 설명한다.
4. 수정 대상, 수정 이유, 예상 변경 범위와 변경 전후 핵심 코드를 먼저 제시한다.
5. 기존 동작을 유지하고 필요한 연결부만 작은 범위로 수정한다.
6. 한 단계에서 새 클래스는 가능하면 한 개 이하로 제한한다.
7. 현재 규모에서 함께 있어도 되는 책임을 억지로 나누지 않는다.
8. 반복되는 판정이나 변경 이유가 분명해진 경우에만 별도 클래스로 분리한다.
9. Scene, Prefab과 Animation Clip은 코드 확인 후 필요한 자산만 수정한다.
10. 일반 진행에서는 사용자가 다음 단계를 요청하기 전까지 다음 단계 파일을 수정하지 않는다. goal 연속 진행을 명시한 경우에는 현재 단계 검증을 끝낸 뒤 다음 단계로 계속 진행한다.

## 3. 현재 구현된 기반

### 3.1 현재 입력 → 처리 → 출력 흐름

```text
Player 공격 입력 또는 Enemy 공격 상태
→ 공격 애니메이션 재생
→ Animation Event 또는 normalized time으로 타격 구간 시작
→ MeleeHitDetector가 현재 검날과 프레임 사이 이동 경로 검색
→ IAttackHitReceiver.ReceiveHit
→ UnitHealth.TakeDamage
→ Hit 또는 Dead 상태
→ 애니메이션 종료 후 행동 복귀 또는 사체 회수
```

### 3.2 완료된 공통 기능

- `AttackDamage`: 유효한 체력 피해 보관
- `AttackHitData`: 피해량, 공격자 팀과 공격 번호 전달
- `IAttackHitReceiver`: 공격자가 구체적인 대상 클래스를 몰라도 타격 전달
- `MeleeHitDetector`: 현재 검날 캡슐과 이전 프레임 이동 경로 검색
- 같은 공격에서 같은 대상 한 번만 타격
- 다음 공격에서 같은 대상 다시 타격
- `UnitHealth`: 체력 감소, 회복, 사망 알림
- Player: 공격, 피격과 사망 상태 연결
- Zombie: 공격, 피격, 사망과 회수 연결
- Mummy Warrior: 공격, 피격과 제한적인 사망 처리 연결

### 3.3 현재 Unit별 지원 상태

| Unit | 공격 판정 | 피격 | 사망 | 남은 핵심 작업 |
|---|---:|---:|---:|---|
| Player | 있음 | 있음 | 있음 | 회피 무적, 실제 방어, 방향성 피격과 Stamina |
| Zombie | 있음 | 있음 | 있음 | 모든 피해가 같은 Hit 반응을 만드는 문제 |
| Mummy Warrior | 있음 | 있음 | 제한적 | 공통 피격 결과 연결과 전용 사망 클립 |
| Demon Swordsman | 미연결 | 미연결 | 미연결 | 무기 판정, 공통 피격 계약과 보스 경직 규칙 |
| Undead Warrior | 없음 | 없음 | 없음 | EnemyUnit과 UnitHealth 연결부터 필요 |

### 3.4 현재 확인된 자산 상태

- Player 일반 공격 1~5와 달리기 공격에 타격 시작·종료·공격 종료 Animation Event가 있다.
- Zombie Swing, Kick, UpDown 공격에 같은 역할의 Animation Event가 있다.
- Player와 Zombie는 기본 Hit 클립 한 개와 Dead 클립 한 개를 가진다.
- Mummy Warrior는 `MummyHitLeft` 피격 클립 한 개를 가진다.
- Demon Swordsman은 Sword와 Beast 자세별 Hit·Death 자산을 가진다.
- `CharacterTestScene`의 Player `attackHitDetector` 직렬화 참조는 비어 있고 런타임 자식 검색에 의존한다.

기존 로드맵에 있던 “플레이어 검 범위가 아직 연결되지 않았다”는 설명은 현재 코드와 맞지 않는다. 기본 양방향 타격은 이미 연결됐으며 이제 타격 품질을 확장해야 한다.

### 3.5 목표 구조와 비교한 현재 한계

- 이동용 `CharacterController`가 피격 몸체 역할까지 함께 맡는다.
- 머리, 몸통, 팔과 다리 접촉을 구분하지 못한다.
- 무기 접촉과 피해 결정이 같은 호출 안에서 즉시 처리된다.
- 같은 프레임의 양쪽 타격 결과가 실행 순서에 따라 달라질 수 있다.
- 공격 준비, 실제 타격과 후딜의 방향 추적 규칙이 공격별 데이터로 분리되지 않았다.
- Hit 애니메이션이 한 개라서 공격 방향과 신체 부위가 동작에 드러나지 않는다.

## 4. 직관적인 네이밍 규칙

이름은 대상 + 역할 또는 대상 + 행동 형태를 사용한다. `Data`, `Manager`, `Helper` 같은 범용 단어만 단독으로 사용하지 않는다.

스테미나의 올바른 영문 철자는 `Stamina`다. `Stemina`는 사용하지 않는다.

| 의미 | 사용할 이름 | 역할 |
|---|---|---|
| 스테미나 | `Stamina` | 공격, 방어와 구르기에 사용하는 힘 |
| Unit 스테미나 | `UnitStamina` | 현재 Stamina, 소비와 회복 관리 |
| 맞은 위치 | `HitPoint` | VFX와 소리가 재생될 월드 위치 |
| 맞은 표면 방향 | `HitNormal` | 불꽃과 혈흔이 향할 방향 |
| 공격이 들어온 방향 | `HitDirection` | 앞·뒤·왼쪽·오른쪽 피격 판단 |
| 실제 접촉 결과 | `HitContact` | 위치, 표면 방향, 공격 방향, 신체 부위와 속도 보관 |
| 맞은 신체 부위 | `HitBodyPart` | `Head`, `Body`, `Arm`, `Leg` 구분 |
| 피격 몸체 | `UnitHitBox` | 뼈를 따라가며 무기 접촉을 받는 Trigger |
| 공격 판정 형태 | `AttackShape` | 검, 주먹, 발과 방패의 검사 위치와 크기 |
| 공격별 설정 | `AttackHitSettings` | 피해, 경직, 판정 시간과 반응 세기 보관 |
| 피격 반응 | `HitReaction` | 방향, 세기와 신체 부위에 맞는 반응 결과 |
| 공격 세기 | `HitPower` | `Light`, `Heavy`, `Knockdown` 구분 |
| 체력 피해 | `HealthDamage` | 실제로 줄어드는 체력 값 |
| 경직 피해 | `StaggerDamage` | Unit의 경직 수치에 더하는 값 |
| 경직 한계 | `StaggerLimit` | 강한 Hit 상태로 바뀌는 기준 |
| 밀리는 거리 | `PushDistance` | 피격 시 이동할 기본 거리 |
| 타격 결과 | `AttackHitResult` | 회피·방어·피해·경직·사망 결과 |
| 무적 상태 | `IsInvincible` | 현재 타격 피해를 무시하는지 표시 |
| 방어 각도 | `BlockAngle` | 정면 방어가 인정되는 범위 |
| 방어 Stamina 피해 | `BlockStaminaDamage` | 막았을 때 줄어드는 Stamina |
| 타격 멈춤 시간 | `HitStopTime` | 공격자와 피해자가 잠깐 멈추는 시간 |
| 타격 효과 재생 | `HitEffectPlayer` | VFX와 Audio 재생 담당 |

기존 이름은 역할이 충분히 보이면 유지한다.

- `UnitHealth`: 유지
- `AttackDamage`: 유지
- `HitContact`: 무기와 피격 몸체의 실제 접촉 결과만 보관
- `AttackHitData`: 공격 정보와 `HitContact`를 피해자에게 전달
- `IAttackHitReceiver`: 공격자가 대상의 구체 클래스를 모르도록 유지
- `MeleeHitDetector`: 최종 전투 중심이 아니라 무기 이동 경로 검사 역할로 축소
- `PlayerAttackHitSettings`: 공격 번호별 피해량과 밀림 거리를 함께 보관

Unity 직렬화 필드나 타입을 바꾸는 경우 Scene과 Prefab 값이 사라지지 않도록 변경 전후 YAML과 Inspector 값을 확인한다.

## 5. 책임 분리 기준

### 5.1 공통 일반 C# 로직

다음 책임은 Unity 생명주기에 직접 묶지 않는다.

- 체력과 사망: `UnitHealth`
- Stamina 소비와 회복: `UnitStamina`
- 경직 누적과 회복: `UnitStagger`
- 실제 접촉 정보: `HitContact`
- 공격 정보와 결과: `AttackHitData`, `AttackHitResult`
- 같은 프레임에 모인 타격의 처리 순서
- Unit별 상태 전환 판단: 기존 StateMachine

### 5.2 Unity 경계

다음 책임은 MonoBehaviour 또는 Unity 자산에 둔다.

- Inspector 참조
- Animation Event 수신
- Animator 파라미터와 CrossFade
- 뼈를 따라가는 `UnitHitBox`
- 무기·주먹·발의 `PhysicsScene` 이동 경로 검색
- `CharacterController.Move`
- Animation Rigging의 상체 방향과 발 IK 보정
- VFX, Audio와 Cinemachine 카메라 반응

### 5.3 함께 유지할 책임

- Player 피격 상태 전환은 `PlayerStateMachine`에 둔다.
- Zombie 피격 상태 전환은 `ZombieStateMachine`에 둔다.
- 피격 수평 이동은 각 캐릭터의 기존 Movement 클래스가 담당한다.
- Hit 애니메이션 재생과 완료 확인은 각 AnimationController가 담당한다.
- 일반 피격은 애니메이션 중심으로 유지하고 강한 넘어짐과 사망에서만 물리 반응을 검토한다.

공통화를 이유로 모든 Unit을 하나의 거대한 전투 Controller에 넣지 않는다.

## 6. 단계별 구현 순서

| 단계 | 기능 | 주요 입력 → 처리 → 출력 | 완료 조건 |
|---:|---|---|---|
| 0 | 기존 기능 기준선 | 현재 공격 → 피해·밀림·사망 확인 | 기존 결과와 남은 문제를 기록함 |
| 1 | 접촉 정보 분리 | 무기 접촉 → `HitContact` → `AttackHitData` | 공격 설정과 실제 접촉 결과가 분리됨 |
| 2 | 신체 HitBox | 무기 경로 → `UnitHitBox` → 신체 부위 접촉 | 이동 Collider 없이 피격 몸체만 공격에 반응함 |
| 3 | 공격 형태 | 공격 번호 → 검·주먹·발 `AttackShape` → 이동 경로 검사 | 공격 동작과 판정 위치가 일치함 |
| 4 | 같은 프레임 타격 | 모든 접촉 수집 → 한 번에 피해 결정 | 실행 순서와 관계없이 동시 타격 결과가 같음 |
| 5 | 공격별 설정 | 공격별 설정 → 피해·경직·판정 시간·회전 제한 | Player와 Zombie 공격을 같은 데이터 기준으로 조절함 |
| 6 | 충격과 경직 | 공격 세기·경직 피해 → 행동 유지·Hit·Knockdown | 약한 공격이 모든 행동을 무조건 끊지 않음 |
| 7 | 방향성 피격 반응 | 접촉 방향·신체 부위 → `HitReaction` | 정면·측면·후면 반응이 구분됨 |
| 8 | 공격 방향과 후딜 | 준비·타격·후딜 → 방향 추적과 취소 제한 | 공격 중 과도한 방향 추적이 사라짐 |
| 9 | 절차적 자세 보정 | 피격 방향 → 상체·골반·발 보정 | 적은 Hit 클립으로도 충격 방향이 몸에 드러남 |
| 10 | 타격 전달감 | 성공 결과 → Hit Stop·VFX·Audio·카메라 | 접촉 위치와 공격 세기가 화면과 소리에 반영됨 |
| 11 | 회피·방어 | 구르기·방어 방향·Stamina → 회피·막기·Guard Break | 방어 행동이 같은 충격 규칙을 사용함 |
| 12 | 애니메이션 확장 | 방향·세기 → 새 Hit·Knockdown 클립 | 새 자산이 기존 반응 규칙에 바로 연결됨 |
| 13 | 다른 Unit 확장 | 공통 접촉·충격·반응 → 다른 Enemy | Unit별 중복 판정 없이 같은 규칙을 사용함 |

## 7. 새 단계별 상세 계획

### 1단계: 접촉 정보 분리

주요 수정 대상:

- 새 `Runtime/Combat/HitContact.cs`
- `Runtime/Combat/AttackHitData.cs`
- `Runtime/Combat/MeleeHitDetector.cs`
- 관련 EditMode 테스트

```text
변경 전: AttackHitData가 공격 설정과 접촉 정보를 직접 보관
변경 후: HitContact가 실제 접촉을 보관하고 AttackHitData가 이를 전달
```

첫 연결에서는 기존 Collider 접촉을 `Body`로 기록한다. 신체 HitBox와 실제 무기 속도는 다음 단계에서 연결한다.

### 2단계: 신체 HitBox

이동용 `CharacterController`와 피격용 `UnitHitBox`를 분리한다.

1. Player와 Zombie에 `Body` HitBox를 먼저 연결한다.
2. 공격 검색은 Trigger HitBox를 포함한다.
3. 이동 Collider는 공격 검색 대상에서 제외한다.
4. 기본 동작 확인 후 `Head`, `Arm`, `Leg` HitBox를 뼈에 연결한다.
5. 공격 검색은 Tag가 아니라 물리 Layer로 제한한다.

### 3단계: 공격 형태

- 검: 손잡이·중간·검끝의 이전 위치부터 현재 위치까지 검사
- 주먹: 손뼈 주변 Sphere 검사
- 발차기: 발목과 발끝 사이 Capsule 검사
- 방패: 방패 앞면 Box 검사

공격마다 Collider를 생성하지 않는다. 미리 연결한 검사 위치와 크기를 `AttackShape`가 선택하고 실제 타격 구간에만 검색한다.

### 4단계: 같은 프레임 타격

```text
이번 프레임의 모든 접촉 수집
→ 중복 대상 정리
→ 양쪽 공격 결과 결정
→ 피해와 피격 상태 함께 적용
```

이미 실제 타격 구간에 들어간 두 공격은 동시 타격될 수 있다. 준비 동작 중 먼저 맞은 공격은 경직 결과에 따라 취소한다.

### 5단계: 공격별 설정

`AttackHitSettings`는 다음 값을 공격별로 보관한다.

- 체력 피해
- 경직 피해
- 공격 세기
- 판정 시작과 종료
- 공격 형태와 크기
- 밀림 거리
- Hit Stop 시간
- 준비 중 방향 추적 각도
- 취소 가능한 구간

Player와 Zombie가 서로 다른 필드 구조를 사용하지 않고 같은 의미의 설정을 사용하게 한다.

### 6단계: 충격과 경직

`StaggerDamage`와 `StaggerLimit`으로 행동 중단 여부를 결정한다.

- 한계 미만: 체력만 감소하고 행동 유지
- 한계 이상: 현재 공격 취소 후 Hit
- 강한 공격: Knockdown
- 높은 강인도의 Enemy와 Boss: 약한 공격으로 계속 끊을 수 없음

### 7단계: 방향성 피격 반응

피해자 기준 접촉 방향과 신체 부위로 `HitReaction`을 만든다.

- 정면, 후면, 왼쪽과 오른쪽
- 머리, 몸통, 팔과 다리
- Light, Heavy와 Knockdown

피격 순간 Enemy 루트를 Player 방향으로 강제 회전하지 않는다. 정면과 측면에서는 상체만 제한적으로 공격자를 향하고, 후면 피격은 뒤에서 들어온 충격을 먼저 표현한다. 행동을 회복한 뒤 Player를 다시 추적한다.

### 8단계: 공격 방향과 후딜

| 구간 | 방향 추적 | 타격 | 취소 |
|---|---|---|---|
| 준비 | 공격별 제한 각도 안에서 허용 | 없음 | 공격별 설정 |
| 타격 | 고정 또는 매우 작은 보정 | 있음 | 경직 결과로 결정 |
| 후딜 | 없음 | 없음 | 지정된 시점 이후 허용 |

발이 과하게 미끄러지는 공격은 Root Motion 값 조정 또는 제한적인 목표 위치 보정을 사용한다.

### 9단계: 절차적 자세 보정

- 가슴과 골반은 충격 반대 방향으로 짧게 기울임
- 목과 시선은 허용 각도 안에서 공격자 인지
- 발은 지면을 유지
- 일반 Hit에는 완전 Ragdoll을 사용하지 않음
- 강제 넘어짐과 사망에서만 물리 혼합 검토

### 10단계: 타격 전달감

접촉 결과를 사용해 Hit Stop, VFX, Audio와 카메라 반응을 재생한다. 전역 `Time.timeScale` 대신 공격자와 피해자 반응만 짧게 멈춘다.

반복 효과는 풀을 사용하고 타격 활성 구간의 런타임 GC 할당은 0 B를 목표로 한다.

### 11단계: 회피와 방어

```text
접촉
→ 회피 중인가
→ 방어 방향 안인가
→ Stamina가 남았는가
→ Dodged, Blocked, Guard Break 또는 Damaged
```

### 12단계: 애니메이션 확장

1. 앞·뒤·왼쪽·오른쪽 일반 Hit
2. 앞·뒤 Heavy Hit
3. Knockdown과 Get Up
4. Guard Break
5. Boss 전용 Hit와 Death

### 13단계: 다른 Unit 확장

```text
Mummy Warrior
→ Demon Swordsman
→ Undead Warrior
```

Player와 Zombie에서 접촉·충격·반응 계약이 검증된 뒤 한 Unit씩 옮긴다.

## 7A. 이전 로드맵 상세 기록

아래 내용은 0~3단계에서 이미 구현한 기능과 과거 계획을 확인하기 위한 기록이다. 앞으로의 구현 순서는 6장과 7장의 새 단계를 따른다.

### 과거 0단계: 현재 전투 기준선 확인

수정 예정 파일: 없음. 먼저 읽기와 Play 확인만 한다.

확인 항목:

1. Player 1~6번 공격 Animation Event 위치
2. Zombie 1~3번 공격 Animation Event 위치
3. Player, Zombie와 Mummy Detector의 `HitStart`, `HitEnd`, `Target Layers`
4. 한 공격에서 대상당 피해 한 번
5. 다음 공격에서 같은 대상 재타격
6. 공격 중 피격되면 현재 Detector 즉시 종료
7. 사망 후 추가 피해와 상태 복귀 없음
8. 15/30/60 FPS에서 빠른 검날 누락 없음
9. Profiler 타격 활성 구간 `GC Alloc`
10. `CharacterTestScene`의 Missing Script와 비어 있는 필수 참조

0단계가 실패하면 새 기능을 만들지 않고 현재 기본 타격의 실패 원인부터 고친다.

### 1단계: 접촉점과 공격 방향

주요 수정 대상:

- `Runtime/Combat/AttackHitData.cs`
- `Runtime/Combat/MeleeHitDetector.cs`
- `Tests/EditMode/AttackHitDataTests.cs`
- `Tests/EditMode/MeleeHitDetectorTests.cs`

예상 변경:

```text
현재: 피해량 + 공격자 팀 + 공격 번호
변경: 기존 값 + HitPoint + HitNormal + HitDirection
```

SphereCast는 `RaycastHit.point`와 `normal`을 사용한다. OverlapCapsule은 현재 검날 위치와 `Collider.ClosestPoint`를 사용해 접촉점을 계산한다.

이 단계에서는 방어, 경직, 피격 이동과 연출을 추가하지 않는다.

### 2단계: 타격 결과 반환

주요 수정 대상:

- `Runtime/Combat/IAttackHitReceiver.cs`
- 새 `Runtime/Combat/AttackHitResult.cs`
- Player, Zombie와 Mummy의 `ReceiveHit` 연결부
- 관련 EditMode 테스트

변경 방향:

```csharp
// 현재
void ReceiveHit(in AttackHitData hit);

// 목표
AttackHitResult ReceiveHit(in AttackHitData hit);
```

결과 이름:

- `Ignored`
- `Dodged`
- `Blocked`
- `Damaged`
- `Staggered`
- `Killed`

판정 순서:

```text
이미 죽었는가
→ 같은 팀인가
→ 무적 상태인가
→ 방어에 성공했는가
→ 체력 피해
→ 경직 한계를 넘었는가
→ 사망했는가
```

2단계에서는 아직 `Dodged`, `Blocked`, `Staggered`를 실제 게임 상태에 연결하지 않고 결과 구조와 기존 피해 결과부터 안전하게 연결한다.

### 3단계: 방향성 피격 이동

주요 수정 대상:

- `PlayerHitState`, `PlayerMovement`
- `ZombieHitState`, `ZombieMovement`
- Player와 Zombie StateMachine 연결부

현재 Hit 클립이 한 개뿐이므로 애니메이션 방향을 억지로 늘리지 않는다.

```text
HitDirection
→ 피해자 기준 앞·뒤·왼쪽·오른쪽 계산
→ 같은 Hit 애니메이션 재생
→ 방향에 맞는 PushDistance를 CharacterController.Move로 적용
```

일반 피격 이동에 `Rigidbody.AddForce`를 섞지 않는다. 현재 캐릭터가 사용하는 CharacterController 충돌과 중력 흐름을 유지한다.

### 4단계: 경직 누적

새 클래스:

- `UnitStagger`

주요 값:

- `CurrentStagger`
- `StaggerLimit`
- `StaggerRecoverDelay`
- `StaggerRecoverSpeed`

흐름:

```text
StaggerDamage 입력
→ CurrentStagger 증가
→ StaggerLimit 미만이면 체력만 감소
→ 한계 이상이면 Staggered
→ 강한 Hit 상태
→ 일정 시간 후 경직 수치 회복
```

Player, 일반 Enemy와 Boss의 `StaggerLimit`을 다르게 설정한다.

### 5단계: 구르기 무적

구르기 전체를 무적으로 만들지 않는다.

```text
구르기 시작
→ 무적 전 구간
→ IsInvincible 활성 구간
→ 무적 종료
→ 구르기 후딜
```

무적 시작과 종료는 현재 구르기 클립의 normalized time을 기준으로 Inspector에서 조절한다.

완료 조건:

- 시작 직후에는 피격 가능
- 구르기 중간에는 `Dodged`
- 구르기 끝부분에는 다시 피격 가능
- `Dodged`에서는 체력, Hit Stop과 피격 VFX를 적용하지 않음

### 6단계: 방어와 Stamina

새 클래스:

- `UnitStamina`

주요 값:

- `MaxStamina`
- `CurrentStamina`
- `StaminaRecoverDelay`
- `StaminaRecoverSpeed`

방어 흐름:

```text
Enemy 공격 입력
→ Player가 방어 중인지 확인
→ 공격 방향이 BlockAngle 안인지 확인
→ BlockStaminaDamage 소비
→ Stamina가 남으면 Blocked
→ 부족하면 Guard Break와 Staggered
```

Stamina UI는 핵심 규칙이 통과한 뒤 같은 단계의 마지막 연결로 추가한다.

### 7단계: 공격 움직임과 후딜

공격을 세 구간으로 구분한다.

| 구간 | 회전 | 타격 | 취소 |
|---|---|---|---|
| 준비 | 제한된 방향 보정 허용 | 없음 | 공격별 설정 |
| 타격 | 매우 작은 방향 보정만 허용 | 있음 | 원칙적으로 제한 |
| 후딜 | 방향 보정 없음 | 없음 | 지정된 시점 이후만 허용 |

Player가 공격 애니메이션 전체에서 대상을 따라 과하게 회전하지 않도록 공격별 회전 가능 시간과 최대 각도를 설정한다.

### 8단계: Hit Stop

Hit Stop은 전역 `Time.timeScale`을 먼저 사용하지 않는다.

```text
Damaged 또는 Staggered 결과
→ 공격자와 피해자의 Animator·행동만 잠깐 정지
→ unscaled 시간으로 종료 확인
→ 원래 재생 속도와 행동 복구
```

초기 조절 범위:

- Light: `0.025~0.04초`
- Heavy: `0.06~0.10초`
- Blocked: `0.03~0.06초`

정확한 값은 공격 애니메이션과 실제 Play 결과를 보며 조절한다.

### 9단계: 타격 VFX와 Audio

새 Unity 경계 클래스:

- `HitEffectPlayer`

입력:

- `HitPoint`
- `HitNormal`
- `HitPower`
- `AttackHitResult`

출력:

- 일반 피해: 혈흔 또는 먼지와 살 타격음
- 방어: 금속 불꽃과 방패음
- 강한 경직: 큰 효과와 강한 타격음
- 회피: 피해 효과 없음

반복 타격에서 `Instantiate`와 `Destroy`를 반복하지 않고 효과 풀을 사용한다.

### 10단계: 카메라 반응

현재 설치된 Cinemachine 2.10.5의 Impulse를 사용한다.

- Player가 공격하거나 피격된 경우만 기본 카메라 반응 허용
- 화면 밖 Enemy끼리의 타격은 카메라를 흔들지 않음
- Light, Heavy와 Blocked 결과에 따라 세기 구분
- 반복 공격에서 화면을 읽기 어려울 정도로 흔들리지 않도록 최대 세기 제한

### 11~13단계: 다른 Unit 확장

적 하나씩 진행한다.

```text
Mummy Warrior
→ Demon Swordsman
→ Undead Warrior
```

Mummy는 현재 창 판정과 Hit 상태를 공통 결과에 연결한다.

Demon Swordsman은 실제 무기 Detector와 `IAttackHitReceiver`부터 연결한다. 보스는 높은 `StaggerLimit`을 사용하고 페이즈 변경 중 경직 면역 여부를 별도 규칙으로 둔다.

Undead Warrior는 바로 공격부터 만들지 않는다. `EnemyUnit`, `UnitHealth`, Hit, Dead 순서를 먼저 연결한 뒤 공격을 추가한다.

### 14단계: 애니메이션 자산 확장

현재 단일 Hit 클립으로 방향성 이동과 규칙을 먼저 완성한다. 새 자산이 준비되면 다음 순서로 확장한다.

1. 앞·뒤·왼쪽·오른쪽 Light Hit
2. Heavy Hit
3. Guard Break
4. Knockdown과 Get Up
5. Boss 전용 Hit와 Death

Ragdoll은 일반 타격에 사용하지 않는다. 강제 넘어짐 또는 사망 연출에서만 별도 단계로 검토한다.

## 8. 주요 코드 방향

아래 코드는 최종 구현을 한꺼번에 적용하는 코드가 아니라 단계별 목표 모양이다.

```csharp
public readonly struct HitContact
{
    public Vector3 HitPoint { get; }
    public Vector3 HitNormal { get; }
    public Vector3 HitDirection { get; }
    public HitBodyPart BodyPart { get; }
    public float HitSpeed { get; }
}

public readonly struct AttackHitData
{
    public AttackDamage Damage { get; }
    public UnitTeam AttackerTeam { get; }
    public int AttackNumber { get; }
    public HitContact Contact { get; }
    public HitPower HitPower { get; }
    public float StaggerDamage { get; }
    public float PushDistance { get; }
}

public interface IAttackHitReceiver
{
    AttackHitResult ReceiveHit(in AttackHitData hit);
}
```

외부 효과 자산, AudioSource와 Cinemachine 참조는 `AttackHitData`에 직접 넣지 않는다. 핵심 타격 결과를 받은 Unity 경계 컴포넌트가 Inspector 설정을 선택한다.

## 9. 예상 파일 범위

### 공통 Combat

- `Assets/_Project/Runtime/Combat/AttackHitData.cs`
- `Assets/_Project/Runtime/Combat/IAttackHitReceiver.cs`
- `Assets/_Project/Runtime/Combat/MeleeHitDetector.cs`
- 새 `AttackHitResult.cs`
- 필요 단계에서 새 `HitPower.cs`

### 공통 Unit

- `Assets/_Project/Runtime/Characters/Unit.cs`
- `Assets/_Project/Runtime/Characters/UnitHealth.cs`
- 4단계의 새 `UnitStagger.cs`
- 6단계의 새 `UnitStamina.cs`

### Player

- `PlayerWorldUnit.cs`
- `PlayerStateMachine.cs`
- `PlayerHitState.cs`
- `PlayerBlockState.cs`
- `PlayerRollState.cs`
- `PlayerMovement.cs`
- `PlayerAnimationController.cs`
- `PlayerController.cs`

### Enemy

- Zombie의 WorldUnit, StateMachine, HitState, Movement와 AnimationController
- Mummy Warrior의 WorldUnit, StateMachine과 AnimationController
- Demon Swordsman의 Controller, WorldUnit, StateMachine과 공격 설정
- Undead Warrior의 Controller와 StateMachine

### Unity 자산

- `CharacterTestScene.unity`
- Player·Enemy Prefab
- Animator Controller
- 공격 Animation Clip Event
- VFX, Audio와 Cinemachine 연결

각 단계에서는 위 파일을 전부 수정하지 않고 해당 단계에 직접 필요한 파일만 선택한다.

## 10. 자동 테스트 기준

### EditMode

- 유효하지 않은 피해 무시
- 같은 팀 공격 무시
- 사망 후 공격 무시
- `HitPoint`, `HitNormal`, `HitDirection` 전달
- 같은 공격에서 대상당 한 번 타격
- 다음 공격에서 다시 타격
- 빠른 검날 이동 경로 타격
- `AttackHitResult` 결과 구분
- `UnitStagger` 누적, 한계와 회복
- 구르기 무적 시작과 종료
- 방어 각도와 Stamina 소비
- Stamina 부족 시 Guard Break

### PlayMode

- Player 1~6번 공격 실제 적중
- Zombie 1~3번 공격 실제 적중
- 15/30/60 FPS에서 같은 피해 횟수
- 공격 중 피격되면 Detector 즉시 종료
- 앞·뒤·왼쪽·오른쪽 피격 이동
- 벽 근처에서 피격 이동이 벽을 통과하지 않음
- 약한 공격이 Boss 행동을 매번 끊지 않음
- 구르기 중간 구간만 회피
- 정면 방어와 등 뒤 피격 결과 구분
- 사망 후 이동, 공격, 피격과 상태 복귀 없음

### Profiler

- 타격 활성 구간 `GC Alloc 0 B` 목표
- 반복 `GetComponent` 없음
- LINQ와 클로저 없음
- 물리 검색 배열 재사용
- VFX와 Audio 오브젝트 풀 재사용
- 검색 버퍼가 가득 찬 경우 Development 환경에서 확인 가능

## 11. 한 단계 완료 보고 형식

각 단계가 끝나면 다음 형식으로 기록한다.

```text
단계:
수정한 기능:
수정한 파일:

입력:
처리:
출력:

자동 테스트:
Unity Play 확인:
Profiler 확인:

남은 문제:
다음 단계:
```

자동 테스트만 통과하고 Unity Play 확인이 남았다면 해당 단계는 완료로 표시하지 않는다.

## 12. 현재 진행 상태

### 확인한 단계

- 기존 기능 기준선: Player와 Zombie가 한 공격에서 피해를 한 번 받고 사망하는 동작을 Unity Play에서 확인했다. 15/30/60 FPS와 Profiler GC 수치는 아직 측정하지 않았다.
- 기존 접촉 정보: `HitPoint`, `HitNormal`, `HitDirection` 전달을 구현했다.
- 기존 타격 결과: `AttackHitResult` 반환과 Player, Zombie, Mummy 피해 결과 연결을 구현했다.
- 기존 방향성 피격 이동: `HitDirection`, `PushDistance`를 Player와 Zombie의 Hit 상태에 전달하고 `CharacterController.Move`로 밀리게 구현했다.
- 새 1단계 접촉 정보 분리: `HitContact`가 실제 접촉 결과를 보관하고 `AttackHitData`가 공격 정보와 접촉 결과를 함께 전달하도록 변경했다.
- 새 2단계 신체 HitBox: Player와 Zombie의 이동 Collider와 피격용 `Body` HitBox를 분리하고 전용 물리 Layer로 검색하도록 변경했다.
- 새 3단계 공격 형태: `AttackShape`가 Capsule, Sphere와 Box의 검사 위치·크기를 보관하고 공격 부위에 맞는 현재 범위와 이동 경로를 검사하도록 변경했다.
- 새 4단계 같은 프레임 타격: `CombatHitResolver`가 이번 프레임의 타격 후보를 모아 중복을 제거하고 양쪽 피해를 함께 적용하도록 변경했다.
- 새 5단계 공격별 설정: Player와 Zombie가 공통 `AttackHitSettings[]`에서 공격 번호에 맞는 Detector, 체력 피해와 밀림 거리를 선택하도록 변경했다.

### 새 1단계 확인 결과

```text
입력: 기존 무기 접촉 위치, 표면 방향과 공격 방향
처리: HitContact 생성 후 AttackHitData에 연결
출력: 기존 피해·밀림·사망 흐름을 유지하며 신체 부위와 속도 확장 지점 확보
```

- `HitBodyPart`: `Unknown`, `Head`, `Body`, `Arm`, `Leg` 추가
- `HitSpeed`: 잘못된 값은 0으로 정리
- 현재 기존 Collider 접촉은 `Body`로 기록
- 현재 `MeleeHitDetector`는 실제 무기 속도를 아직 계산하지 않아 `HitSpeed`가 0
- 런타임과 EditMode 테스트 어셈블리 빌드 오류 0개
- Unity 관련 테스트 10개 통과, 실패 0개
- `FormerlySerializedAs`와 `UnityEngine.Serialization` 추가 없음
- Scene과 Prefab 변경 없음

### 새 2단계 확인 결과

```text
입력: Player 또는 Zombie의 실제 공격 판정 경로
처리: 상대 전용 HitBox Layer의 Trigger UnitHitBox를 검색하고 부모 피해 수신자와 Body 부위를 찾음
출력: 이동용 Collider는 무시하고 Body 접촉을 한 공격에서 한 번만 피해 흐름에 전달
```

- `PlayerHitBox` Layer 17과 `EnemyHitBox` Layer 18 추가
- Player에 `PlayerBodyHitBox`, Zombie에 `EnemyBodyHitBox` Trigger Capsule 연결
- Player Detector는 기존 Enemy Layer와 `EnemyHitBox` Layer를 함께 검색
- Zombie 3개 Detector는 기존 Player 대상 Layer와 `PlayerHitBox` Layer를 함께 검색
- `UnitHitBox`가 부모의 `IAttackHitReceiver`와 `HitBodyPart.Body`를 연결
- Unit에 `UnitHitBox`가 있으면 기존 이동 Collider는 공격 대상으로 사용하지 않음
- 아직 옮기지 않은 Unit은 기존 Collider 판정을 유지하여 현재 전투 동작 보존
- 런타임과 EditMode 테스트 어셈블리 빌드 오류 0개
- 접촉 정보와 근접 공격 직접 테스트 12개 통과, 실패 0개
- Unity Play 양방향 검증 통과: Player → Zombie와 Zombie → Player 모두 `Damaged`, `Body`, 1회 타격
- `FormerlySerializedAs`와 `UnityEngine.Serialization` 추가 없음
- 머리, 팔과 다리 세부 HitBox는 공격 형태와 접촉 범위가 확인된 뒤 별도 작은 단계로 연결

### 새 3단계 확인 결과

```text
입력: Player 또는 Zombie의 공격 번호가 선택한 MeleeHitDetector
처리: Detector의 AttackShape 종류에 따라 Capsule, Sphere 또는 Box의 현재 범위와 이전 위치부터 현재 위치까지 검사
출력: UnitHitBox 접촉을 기존 피해·밀림·사망 흐름에 한 번만 전달
```

- `AttackShapeType`: `Capsule`, `Sphere`, `Box` 추가
- `AttackShape`: 검사 종류, 시작점, 끝점, 반지름과 Box 크기를 직렬화
- Player 검: 기존 `HitStart → HitEnd` Capsule과 반지름 0.12 유지
- Zombie Swing: 오른손 손가락 끝 중심 Sphere와 반지름 0.18 적용
- Zombie Kick: 오른발 → 발끝 Capsule과 반지름 0.18 적용
- Zombie UpDown: 왼손 → 오른손 Capsule과 반지름 0.18 적용
- Mummy 창: 기존 시작점 → 끝점 Capsule과 반지름 0.18 유지
- Capsule은 시작·중간·끝, Sphere는 중심점, Box는 중심 이동 경로를 프레임 사이에 추가 검사
- 공격 중 Collider와 GameObject를 새로 만들지 않고 기존 검색 배열과 접촉 캐시 재사용
- 런타임과 EditMode 테스트 어셈블리 빌드 오류 0개
- 현재 겹침, 빠른 이동, 회전, UnitHitBox와 한 공격당 한 번 타격을 포함한 직접 테스트 16개 통과
- Unity Play 실제 공격 4개 통과: Player 검, Zombie 손, 발과 양손 공격 모두 `Damaged`, `Body`, 1회
- Play 검증용 Zombie 탐지·공격 거리는 0.1로 제한한 뒤 원래 값 30과 1.8로 복원
- Box는 아직 Player와 Zombie의 실제 공격에는 사용하지 않고 이후 방패 판정 확장 지점으로 유지
- 실제 이동 속도 계산은 아직 연결하지 않아 `HitSpeed`는 0이며 공격 설정·충격 단계에서 연결
- `FormerlySerializedAs`와 `UnityEngine.Serialization` 추가 없음

### 새 4단계 확인 결과

```text
입력: 같은 프레임에 Player와 Zombie의 MeleeHitDetector가 찾은 타격 후보
처리: CombatHitResolver가 공격 순번과 피해 대상을 기준으로 중복을 제거하고 LateUpdate에서 저장된 후보를 함께 적용
출력: 먼저 처리된 피격이 상대 공격을 종료해도 이미 실제 타격 구간에서 찾은 양쪽 공격은 각각 한 번 적용
```

- `MeleeHitDetector`가 `ReceiveHit`를 즉시 호출하지 않고 `CombatHitResolver.QueueHit`에 타격 후보 전달
- `CombatHitResolver`는 초기 용량 32의 재사용 List와 HashSet으로 현재 후보, 적용 중 후보와 중복 키를 관리
- 적용 중 새로 들어온 타격은 다음 적용 묶음에 남겨 현재 반복 목록을 바꾸지 않음
- 감지기는 부모 확정기를 우선 사용하고, 없으면 초기화 시 같은 Scene의 확정기를 찾아 Additive Scene에서도 다른 Scene 확정기를 사용하지 않음
- `WorldObjectManager`에 Scene 공용 `CombatHitResolver` 1개 연결, 현재 근접 감지기 5개가 같은 확정기를 사용
- 단독 타격, 같은 공격과 대상의 중복, 양쪽 타격과 적용 중 새 후보 등록을 포함한 직접 테스트 19개 통과
- 런타임과 EditMode 테스트 어셈블리 빌드 오류 0개
- Unity Play 동시 타격 통과: 확정 전 결과는 양쪽 0회, 확정 후 Player 검 → Zombie와 Zombie 손 → Player 모두 `Damaged`, `Body`, 1회
- Play 검증용 Zombie 탐지·공격 거리는 0.1로 제한한 뒤 원래 값 30과 1.8로 복원
- 준비 동작 중 피격된 공격의 취소 여부는 아직 기존 Hit 상태 규칙을 유지하며 새 6단계 충격과 경직에서 구분
- `FormerlySerializedAs`와 `UnityEngine.Serialization` 추가 없음

### 새 5단계 확인 결과

```text
입력: Player 또는 Zombie 애니메이션 이벤트가 전달한 공격 번호
처리: 공통 AttackHitSettings 배열에서 같은 번호의 Detector, 체력 피해와 밀림 거리를 찾음
출력: 선택한 Detector가 AttackHitData를 만들고 실제 타격 후보를 CombatHitResolver에 전달
```

- Player 전용 `PlayerAttackHitSettings`를 공통 `AttackHitSettings`로 이름 변경하고 기존 meta GUID 보존
- `AttackHitSettings`: 공격 번호, `MeleeHitDetector`, `AttackDamage`와 밀림 거리를 보관
- Player는 공용 Detector 필드를 제거하고 1~6번 각 설정이 사용할 Detector를 직접 참조
- Zombie는 Detector 3개, Damage 3개와 공용 밀림 거리 필드를 제거하고 1~3번 공통 설정 배열로 통합
- Player 1~6번 피해 10 유지, 밀림 거리는 0.4, 0.4, 0.4, 0.45, 0.55와 0.5 유지
- Zombie 1~3번 피해 10과 밀림 거리 0.3 유지
- Player Scene 설정 6개와 Zombie Prefab 설정 3개의 공격 번호, Detector, 피해와 밀림 거리 직렬화 확인
- 공통 설정 검색, 없는 번호, 잘못된 피해, 중복 번호, 잘못된 밀림 거리와 Detector 저장 테스트 6개 통과
- 런타임과 EditMode 테스트 어셈블리 빌드 경고 0개, 오류 0개
- Unity Play 설정별 검사 통과: Player 1~6번 6/6, Zombie 1~3번 3/3
- Unity Play 실제 피해 통과: Player → Zombie와 Zombie → Player 모두 `Damaged`, `Body`, 1회
- 경직 피해, 공격 세기, Hit Stop, 방향 추적과 취소 구간은 아직 소비 코드가 없어 미리 저장하지 않고 해당 단계에서 추가
- Play 검증용 Zombie 탐지·공격 거리는 0.1로 제한한 뒤 원래 값 30과 1.8로 복원
- 옛 타입·개별 공격 필드 참조 제거 확인
- `FormerlySerializedAs`와 `UnityEngine.Serialization` 추가 없음

### 새 6단계 확인 결과

```text
입력: AttackHitData의 체력 피해와 StaggerDamage
처리: 체력 피해를 먼저 적용하고 UnitStagger가 경직 수치를 누적·회복하며 StaggerLimit 도달 여부를 판단
출력: 한계 미만은 Damaged로 현재 행동 유지, 한계 도달은 Staggered로 현재 공격 종료 후 Hit, 체력이 끝나면 Killed 우선
```

- 일반 C# `UnitStagger`가 `CurrentStagger`, `StaggerLimit`, 회복 대기와 초당 회복 수치를 보관
- `AttackHitSettings`와 `AttackHitData`에 실제 소비되는 `StaggerDamage` 추가
- 기존 `AttackHitData` 생성자는 체력 피해를 기본 경직 피해로 사용해 Mummy와 기존 호출 흐름 유지
- `Unit.ApplyHealthAndStaggerHit`이 체력 결과를 먼저 확정하고 살아 있는 `Damaged` 결과에서만 경직 한계 판단
- Player와 Zombie는 `Staggered`일 때만 Hit 상태로 전환하며 `Damaged`에서는 진행 중 행동 유지
- Player 경직 한계 10, 회복 대기 1초, 초당 회복 20 적용
- Zombie 경직 한계 20, 회복 대기 1초, 초당 회복 10 적용
- Player 1~6번과 Zombie 1~3번 공격의 경직 피해 10 직렬화
- 공격 취소 여부를 외부에서 안전하게 확인할 `IsAttackHitActive` 읽기 값 추가
- 런타임과 EditMode 테스트 어셈블리 빌드 오류 0개, Unity C# 컴파일 오류 0개
- 경직 누적, 한계 도달, 회복 대기·회복, 잘못된 값, 공격 데이터·설정과 사망 우선순위를 포함한 Unity 직접 테스트 25/25 통과
- Unity Play 통과: Zombie는 Player 약한 공격 1회에 `Damaged`이며 공격 유지, 2회째 `Staggered`이며 공격 종료
- Unity Play 통과: Player는 Zombie 공격 1회에 `Staggered`이며 진행 중 공격 종료
- Unity Play 통과: 남은 체력보다 큰 피해는 경직보다 `Killed` 우선
- 현재 없는 Knockdown 상태와 클립을 기존 Hit로 가장하지 않으며, 공격 세기와 방향을 함께 고르는 새 7단계 `HitReaction`에서 데이터부터 연결
- `FormerlySerializedAs`와 `UnityEngine.Serialization` 추가 없음

### 새 7단계 확인 결과

```text
입력: 실제 접촉의 HitDirection, HitBodyPart와 공격 설정의 HitStrength
처리: 피해자가 자신의 Forward와 Right를 기준으로 공격이 들어온 Front, Back, Left 또는 Right를 계산
출력: 방향, Light·Heavy·Knockdown, 신체 부위와 기존 밀림 값을 담은 HitReaction을 Hit 상태에 전달
```

- `HitReactionDirection`: `Front`, `Back`, `Left`, `Right` 추가
- `HitStrength`: `Light`, `Heavy`, `Knockdown` 추가
- 값 형식 `HitReaction`이 방향, 세기, 신체 부위, 밀림 방향과 밀림 거리를 함께 보관
- 공격자가 피해자를 미는 방향의 반대쪽을 공격이 들어온 위치로 계산
- 정면·후면과 좌·우가 같은 대각선 경계에서는 정면·후면을 우선해 경계 흔들림을 줄임
- 잘못되거나 길이가 없는 방향은 `Front`와 밀림 없음으로 안전하게 처리
- 피해자 Transform의 현재 `Forward`와 `Right`를 Player와 Zombie StateMachine이 전달
- PlayerHitState와 ZombieHitState가 두 개의 개별 밀림 값 대신 `HitReaction` 하나를 보관
- 현재 Hit 클립 한 개는 그대로 재생하고 `LastHitReaction` 읽기 값으로 이후 방향별 클립 연결 지점 제공
- Player 1~4타는 `Light`, 5타와 달리기 공격은 `Heavy`로 직렬화
- Zombie 손 공격은 `Light`, 발과 양손 공격은 `Heavy`로 직렬화
- `Knockdown` 데이터는 전달되지만 전용 상태와 애니메이션이 없어 현재 Hit 클립으로 가장하지 않음
- 런타임과 EditMode 테스트 어셈블리 빌드 오류 0개, Unity C# 컴파일 오류 0개
- 네 방향, 대각선 경계, 잘못된 값, 세기, 신체 부위와 기존 경직 회귀를 포함한 Unity 직접 테스트 34/34 통과
- Unity Play에서 Player 네 방향과 Zombie 네 방향 모두 예상 `HitReactionDirection` 확인
- Unity Play에서 `Light`, `Heavy`, `Knockdown`과 머리·몸통·팔·다리 값 보존 확인
- Unity Play에서 피격 처리 직전과 직후 Player·Zombie 루트 회전 변화 0도 확인
- Unity Play에서 기존 경직과 `Killed` 사망 우선순위 유지
- 상체가 공격자를 제한적으로 인지하는 보정은 현재 Rig 연결이 없어 9단계 절차적 자세 보정에서 적용
- `FormerlySerializedAs`와 `UnityEngine.Serialization` 추가 없음

### 3단계 확인 결과

```text
입력: 공격의 HitDirection + PushDistance
처리: HitPushMovement가 수평 방향과 프레임별 이동 거리를 계산
출력: PlayerMovement 또는 ZombieMovement가 CharacterController.Move 호출
```

- Unity 스크립트 컴파일 통과
- 공통 밀림 계산과 `AttackHitData` 테스트 6개 통과
- 충돌 없는 위치에서 Player 오른쪽 0.4m, Zombie 뒤쪽 0.3m 이동 통과
- 현재 Scene Inspector 값 확인: Player와 Zombie 모두 공격 밀기 0.3m, 피격 밀림 시간 0.18초
- 1차 공격 이동 균형 조정: Player 1~5타와 달리기 공격의 전진 거리를 약 0.5~0.83m 범위로 낮춤
- 원인 구분을 위해 1차 확인 전까지 피격 밀림 0.3m와 0.18초는 유지
- 실제 Player 타격 측정: 설정 0.3m 중 0.273m 이동, 옆 충돌 발생
- 2차 피격 균형 조정: Player 공격 밀기만 0.4m로 올리고 피격 밀림 시간 0.18초와 Zombie 공격 밀기 0.3m는 유지
- Player 공격 데이터화: 1~6번 공격의 피해량과 밀림 거리를 `PlayerAttackHitSettings`에서 함께 선택
- 공격별 밀림 거리: 1~3타 0.4m, 4타 0.45m, 5타 0.55m, 달리기 공격 0.5m
- 피격 중 새 공격이 들어오면 남은 밀림을 누적하지 않고 새 공격의 밀림 거리로 다시 시작
- 연속 피격 Play 자동 검증: 두 AttackHitData를 연속 적용했을 때 모두 Damaged였고 두 번째 설정 0.5m만큼 다시 이동
- Scene과 Prefab의 직렬화 필드를 `attackHitSettings`로 직접 변경하고 `FormerlySerializedAs`는 사용하지 않음
- 기존 Hit 애니메이션 한 개 유지
- Rigidbody와 `AddForce`를 추가하지 않음

### 2026-08-02 중단 기록

- 사용자 요청으로 자동 단계 진행 중단
- 새 6단계 충격과 경직 완료
- 새 7단계 방향성 피격 반응 완료
- 새 8단계는 현재 공격 흐름과 실제 Animation Event 시점 분석까지만 완료
- Player는 공격 전체에서 입력 방향 회전을 계속하고, Zombie는 공격 전체의 루트 회전을 적용하는 현재 문제 확인
- 모든 Player 6개와 Zombie 3개 공격 클립에 `StartAttackHitAnimationEvent → EndAttackHitAnimationEvent → EndAttackAnimationEvent` 순서가 있는 것 확인
- 새 `AttackPhaseTracker.cs`와 `AttackPhaseTrackerTests.cs` 파일은 작성했지만 Player·Zombie 연결 전 상태
- 중단 직전 Unity 가져오기·컴파일 명령은 연결 탐색 실패로 실행되지 않아 새 두 파일의 Unity 컴파일과 테스트 통과를 확인하지 않음
- 공격 상태, Controller, AnimationEventReceiver, 루트 회전과 씬·프리팹에는 8단계 변경을 아직 적용하지 않음
- 새 8단계는 완료 처리하지 않음

### 재개할 작업

`8단계: 공격 방향과 후딜`을 현재 공격 애니메이션 이벤트와 상태 흐름을 유지하면서 공격 구간 판정부터 작은 범위로 적용한다.

1. Unity 연결을 새로 확인하고 `AttackPhaseTracker.cs`와 테스트 파일을 가져온 뒤 프로젝트 파일을 갱신한다.
2. `AttackPhaseTrackerTests` 6개와 런타임·EditMode 테스트 어셈블리 컴파일을 먼저 확인한다.
3. Player와 Zombie 공격의 시작, 타격 시작, 타격 종료와 상태 종료를 `Ready → Hit → Recovery → 종료`에 연결한다.
4. Player 입력 회전과 Player·Zombie 공격 루트 회전은 `Ready`에서만 허용한다.
5. 공격별 방향 보정 가능 시간, 최대 보정 각도와 취소 가능 시점을 직관적인 설정 값으로 데이터화한다.
6. `Damaged`는 공격을 유지하고 `Staggered` 이상만 현재 공격을 끊는 6단계 규칙을 보존한다.
7. 구간 전환, 경계 시간, 방향 제한, 후딜 중 입력과 경직 취소를 자동 테스트한다.
8. Unity Play에서 큰 목표 각도, 타격 순간 방향 고정, 후딜과 연속 피격 시 공격 중단을 확인한다.
