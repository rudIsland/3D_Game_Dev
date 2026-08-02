# 적 캐릭터와 전투 애니메이션 구현 기록

- 작업 날짜: 2026-08-01
- 구현 기능: 적 캐릭터 추가, 좀비 전투 로직 개선, 플레이어·적 애니메이션 정리
- 개발 씬: Assets/_Project/Scenes/Dev/CharacterTestScene.unity
- 참고 문서:
  - Docs/2026-07-25_CharacterTestSceneLog.md
  - Docs/2026-07-27_PlayerStateWorldObjectLog.md
  - Docs/2026-08-01_PlayerZombieHitRoadmap.md

## 1. 작업 목표

캐릭터 테스트 씬에 새로운 적을 추가하고, 기존 좀비가 탐지와 추적만 하는 상태에서 실제 공격·피격·사망 흐름까지 이어지도록 개선한다.

애니메이션은 단순히 클립을 재생하는 데서 끝내지 않고 다음 게임 동작과 연결한다.

    입력
    → 일반 C# 상태머신에서 현재 행동 결정
    → 이동과 Animator에 실행 요청
    → 공격 활성 구간에만 타격 판정
    → 체력 감소
    → 피격 또는 사망 상태 전환
    → 사망 애니메이션 종료 후 WorldObject 회수

## 2. 오늘 작업 요약

| 기능 | 현재 단계 | 핵심 결과 |
|---|---|---|
| Demon Swordsman 보스 | 전투 행동 골격 구현 | 거리별 이동, 공격 선택, 연속 공격, 자세와 페이즈 변경 구성 |
| Mummy Warrior | 기본 전투 연결 | 입장, 대기, 추적, 공격, 피격, 사망과 창 판정 연결 |
| Undead Warrior | 표시와 생명주기 골격 | 프리팹, Controller와 Idle 상태머신 구성 |
| Zombie | 기본 양방향 전투 연결 | 탐지, 경계, 추적, 공격, 피격, 사망과 공격 판정 연결 |
| Player 애니메이션 | 전투 상태 연결 개선 | 방향별 구르기, 방어, 공격, 피격, 사망과 타격 이벤트 연결 |
| 애니메이션 자산 | 역할별 정리 | 이동, 경계, 공격, 피해 폴더와 Controller 구조 정리 |

각 적의 구현 단계는 서로 다르다. Mummy와 Zombie는 체력과 피격 흐름까지 연결됐지만, Undead는 아직 안전한 Idle 상태만 가진다. Demon Swordsman은 행동과 애니메이션 골격은 크지만 실제 플레이어 피해 판정은 별도 연결과 검증이 남아 있다.

## 3. 적 캐릭터 추가

### 3.1 Demon Swordsman 보스

주요 위치:

- Assets/_Project/Characters/Enemies/Boss/DemonSwordsman/Prefabs/DemonSwordsmanBoss.prefab
- Assets/_Project/Characters/Enemies/Boss/DemonSwordsman/Runtime/
- Assets/_Project/Characters/Enemies/Boss/DemonSwordsman/Settings/DemonSwordsmanBossSettings.asset
- Assets/_Project/Characters/Enemies/Boss/DemonSwordsman/Animations/Controllers/DemonSwordsmanBoss.controller

입력 → 처리 → 출력 흐름:

    플레이어 위치와 보스 체력
    → DemonSwordsmanController
    → DemonSwordsmanWorldUnit
    → DemonSwordsmanStateMachine
    → 이동 / 공격 / 자세 변경 / 페이즈 변경 상태
    → CharacterController 이동과 Animator 재생

| 상태 | 역할 |
|---|---|
| DemonSwordsmanMoveState | 거리와 방향에 따라 접근, 옆 이동, 후퇴와 제자리 회전 결정 |
| DemonSwordsmanAttackState | 공격 설정 실행, 분기 구간 확인과 연속 공격 연결 |
| DemonSwordsmanStyleChangeState | 검 자세와 맨손 자세 전환 |
| DemonSwordsmanPhaseChangeState | 체력 조건에 따른 1페이즈 → 2페이즈 전환 |

DemonSwordsmanBossSettings에 거리, 시간, 페이즈, 자세와 공격 설정을 모았다. 상태머신은 설정에 맞는 공격을 선택하고 실제 Animator 재생은 DemonSwordsmanAnimationController에 요청한다.

공격 애니메이션의 이동은 Animation Event에서 전달된 이동값을 DemonSwordsmanMovement가 CharacterController에 적용한다.

현재 주의점:

- 행동 선택과 애니메이션 재생 구조는 준비됐다.
- 테스트용 체력 감소와 페이즈 변경 경로가 있다.
- 실제 무기 범위가 플레이어의 IAttackHitReceiver에 피해를 보내는 최종 연결은 별도 확인이 필요하다.

### 3.2 Mummy Warrior

주요 위치:

- Assets/_Project/Characters/Enemies/Mummy Warrior/Prefabs/MummyWarrior.prefab
- Assets/_Project/Characters/Enemies/Mummy Warrior/AI/
- Assets/_Project/Characters/Enemies/Mummy Warrior/Animations/MummyWarriorAnimator.controller
- Assets/_ThirdParty/Mummy Warrior/Prefab/Lance Skin 1.prefab

입력 → 처리 → 출력 흐름:

    플레이어 위치 / ReceiveHit
    → MummyWarriorController
    → MummyWarriorWorldUnit
    → MummyWarriorStateMachine
    → Alive / Hit / Dead
    → 이동 / Animator / 창 타격 판정 / Despawn 요청

Alive 상태 안에서는 Enter → Idle → Chase → Attack 행동을 관리한다. 피격은 현재 행동을 중단하고 Hit으로 전환한다. 체력이 0이 되면 Dead가 가장 먼저 적용되며 이후 추적과 공격으로 돌아가지 않는다.

Mummy에는 전용 Death 클립이 없다. MummyExit를 죽음으로 대신 사용하지 않고, 죽는 순간의 자세에서 Animator를 멈춘다. 사체 유지 시간이 끝나면 기존 WorldObjectManager 회수 경로를 요청한다.

공격은 MummyWarriorAttackPattern 배열로 설정한다.

| 설정 | 역할 |
|---|---|
| 표시 이름과 Animator 상태 이름 | Inspector 구분과 재생 상태 연결 |
| 최소·최대 거리와 허용 각도 | 공격 가능 조건 |
| 피해량과 선택 가중치 | 공격 결과와 무작위 선택 비율 |
| 재사용 시간 | 같은 공격의 연속 사용 제한 |
| Hit Start/End Time | 창 판정을 여닫는 normalized time |
| 전환 시간과 재생 속도 | 공격 애니메이션 조절 |

Mummy의 판정 시작과 종료는 Animation Event에 넣지 않는다.

    Attack 재생
    → 상태머신이 normalized time 확인
    → Hit Start Time에 MeleeHitDetector 시작
    → Hit End Time에 MeleeHitDetector 종료
    → 같은 공격에서는 판정 구간을 다시 열지 않음

StartAttackHit은 Mummy 상태머신이 내부에서 호출하는 함수이며 Animation Event 함수가 아니다. AttackStart도 현재 Mummy API에 없다.

### 3.3 Undead Warrior

주요 위치:

- Assets/_Project/Characters/Enemies/Undead/Prefabs/UndeadWarrior.prefab
- Assets/_Project/Characters/Enemies/Undead/Scripts/UndeadWarriorController.cs
- Assets/_Project/Characters/Enemies/Undead/Scripts/UndeadWarriorStateMachine.cs

현재 흐름:

    Unity OnEnable
    → UndeadWarriorStateMachine.Enable
    → Idle 상태 Enter
    → 매 프레임 Idle Update
    → OnDisable에서 상태 종료

Undead는 모델과 프리팹을 테스트 씬에서 확인하고 이후 행동을 추가할 수 있도록 생명주기와 Idle 상태만 구성했다.

아직 플레이어 탐지, 이동, 공격, EnemyUnit과 UnitHealth 연결, 피격과 사망 상태는 없다. 이 상태를 Mummy나 Zombie와 같은 완성된 전투 AI로 기록하지 않는다.

## 4. 좀비 로직 개선

### 4.1 전체 흐름

    WorldObjectManager Tick
    → ZombieWorldUnit.Update
    → ZombieStateMachine.Update
    → Alive / Hit / Dead
    → Alive 안에서 Idle / Alert / Chase / Attack
    → 이동, 애니메이션, 타격 판정 또는 회수 요청

ZombieController는 Inspector 참조, CharacterController, Animator, 공격 Detector, 플레이어 Transform과 WorldObjectView를 연결한다. IAttackHitReceiver.ReceiveHit으로 들어온 공격은 ZombieWorldUnit이 팀과 피해 유효성을 확인한 뒤 기존 UnitHealth에 전달한다.

    AttackHitData 입력
    → 공격자 팀 확인
    → Enemy 공격이면 무시
    → UnitHealth.TakeDamage
    → 생존하면 Hit
    → 체력이 0이면 Dead

### 4.2 살아 있는 상태

| 상태 | 역할 |
|---|---|
| ZombieIdleState | 설정된 간격으로 플레이어 탐지 |
| ZombieAlertState | 처음 발견했을 때 경계 애니메이션 재생 |
| ZombieChaseState | 플레이어 방향 회전과 추적 |
| ZombieAttackState | 공격 선택, 재생 완료 대기와 다음 상태 결정 |

Idle에서는 idleTargetCheckInterval 간격으로만 탐지한다. 플레이어를 발견한 뒤에는 위치와 거리 값을 프레임당 한 번 갱신하고 하위 상태들이 같은 값을 사용한다.

공격 거리 안에서도 attackFacingAngle이 맞지 않으면 먼저 제자리 회전한다. 거리와 방향이 모두 맞을 때만 공격으로 전환한다.

### 4.3 공격 선택

공격 종류는 Swing, Kick, UpDown 세 가지다. 가까운 거리에서는 Kick 비중을 높이고, 공격 거리의 바깥쪽에서는 Swing과 UpDown 비중을 높인다. 직전에 사용한 공격의 가중치는 0으로 만들어 바로 반복되는 것도 피한다.

공격 중에는 추적 회전을 멈추고 지면과 중력만 유지한다. 애니메이션 종료 후 거리와 방향을 다시 확인하여 재공격, 추적 또는 Idle을 결정한다.

### 4.4 피격과 사망

피격 흐름:

    유효 피해
    → 현재 공격 판정 즉시 종료
    → ZombieHitState
    → 수평 이동 중단
    → Hit 애니메이션 처음부터 재생
    → 완료 후 Alive 복귀

피격 중 다시 맞으면 같은 상태를 새로 만들지 않고 Restart로 Hit 애니메이션을 처음부터 다시 재생한다.

사망 흐름:

    UnitHealth.Died
    → ZombieDeadState
    → 현재 공격 판정 종료
    → Dead 애니메이션
    → 설정된 사체 유지 시간
    → WorldObjectManager에 Despawn 요청

사망 상태는 피격과 Alive 복귀보다 우선한다. 사망 후에는 이동, 공격과 추가 피격 상태로 돌아가지 않는다.

## 5. 공격 판정과 Animation Event

플레이어와 Zombie는 MeleeHitDetector, AttackHitData와 IAttackHitReceiver를 함께 사용한다.

    공격 클립의 시작 이벤트
    → 현재 공격 번호와 피해 정보 생성
    → 해당 무기의 MeleeHitDetector.StartHit
    → 활성 구간에서 대상 검색
    → 같은 공격에서 이미 맞은 대상 제외
    → IAttackHitReceiver.ReceiveHit
    → 종료 이벤트에서 MeleeHitDetector.EndHit

Player Animation Event 함수:

- StartAttackHitAnimationEvent(int attackNumber)
- EndAttackHitAnimationEvent()

Zombie Animation Event 함수:

- StartAttackHitAnimationEvent(int attackNumber)
- EndAttackHitAnimationEvent()
- EndAttackAnimationEvent()
- EndAlert()

기존 AttackStart, ActiveWeapon, SetHitIndex, DisActiveWeapon와 EndAttack 이름 대신 현재 Receiver의 역할이 드러나는 이름으로 정리했다.

중요:

- Player와 Zombie는 Animation Event로 타격 구간을 연다.
- Mummy는 공격 설정의 normalized time으로 타격 구간을 연다.
- Mummy 클립에 Zombie Event 함수를 추가하지 않는다.

## 6. 애니메이션 작업

### 6.1 Player

주요 작업:

- 뒤, 대각선과 좌우 방향별 구르기 클립 구성
- 방어 시작, 유지, 피격과 종료 클립 구분
- 일반 공격 1~5와 달리기 공격 이름 정리
- 공격 번호에 맞는 상태 완료 판단 추가
- 피격 중 다시 맞았을 때 Hit를 처음부터 재생
- PlayerAnimationEventReceiver를 통해 검 판정 구간 연결

주요 Animator 값은 MoveAmount, BlockMoveX/Y, RollDirectionX/Y, Roll, SprintRoll, IsBlocking, Attack, AttackIndex, Hit과 Death다.

### 6.2 Zombie

애니메이션 자산을 00_Move, 10_Alert, 30_Attack, 40_Damage와 Archive로 정리했다.

Animator는 State와 AttackType을 기준으로 Idle, Alert, Chase, 세 공격, Hit와 Dead를 구분한다. ZombieAnimationController는 상태 요청 변환, 공격과 Hit 재시작, 완료 확인, 공격 종료 CrossFade와 공격 클립의 루트 회전을 담당한다.

### 6.3 Mummy Warrior

MummyWarriorAnimator.controller 하나만 실제 Controller로 사용한다. 구형 AnimationState 정수 Controller는 제거했다.

Base Layer:

- Locomotion 2D Freeform Cartesian Blend Tree
- MoveSide와 MoveSpeed
- Idle (0, 0)
- 왼쪽 걷기 (-1, 0.5)
- 전진 걷기 (0, 0.5)
- 오른쪽 걷기 (1, 0.5)
- 달리기 (0, 1)
- AlternateIdle 재생 후 Locomotion 복귀

Full Body Actions Override Layer:

- Attack
- Hit
- Block
- Turn
- StepBack
- Enter
- Exit
- Death 진입 지점

현재 클립이 전신 동작이므로 Avatar Mask는 사용하지 않는다. Hit와 Death 전환을 다른 행동보다 앞에 두며 죽은 뒤에는 다른 상태로 돌아가지 않는다.

MummyWarriorAnimationController는 범용 Play 대신 SetMovement, PlayAttack, PlayHit, PlayBlock, PlayTurn, PlayStepBack, PlayEnter, PlayExit와 PlayDeath처럼 역할이 보이는 메서드를 제공한다.

### 6.4 Demon Swordsman

보스 Animator는 자세별 Locomotion, 좌우 회전, 설정 기반 공격, 검과 맨손 자세 변경, 페이즈 변경의 Fear와 Rage, 자세별 Hit를 연결한다.

공격 설정에는 Animator 상태 이름과 CrossFade 시간도 포함된다. 상태머신은 공격 종류를 선택하고 실제 재생은 DemonSwordsmanAnimationController가 담당한다.

## 7. CharacterTestScene 변경

기존 테스트 환경을 유지하면서 DemonSwordsmanSpawnPoint, MummyWarriorRoot와 Display, UndeadWarriorRoot와 Display, Zombie 공격 판정과 새 Animator 참조를 추가했다.

Mummy의 기존 Root와 장비 위치는 유지하고 CharacterController, MummyWarriorController, MummyWarriorAnimationController, 창 MeleeHitDetector와 HitStart/HitEnd를 연결했다.

## 8. 유지보수 기준

MonoBehaviour Controller는 Inspector 참조, Unity 생명주기, Animator, CharacterController, Animation Event와 물리 판정 같은 Unity 경계를 담당한다. 상태 결정, 거리 판단, 공격 선택과 재사용 시간은 일반 C# 상태머신이 담당한다.

반복 구간에서는 다음 기준을 적용했다.

- Animator 이름은 해시로 한 번 계산해 재사용
- 자주 쓰는 컴포넌트 캐시
- 거리 비교는 제곱 거리 사용
- MeleeHitDetector 검색 배열 재사용
- 상태 객체는 생성할 때 한 번 만들고 재사용
- Update 경로에서 LINQ와 클로저 사용하지 않음

확장 방법:

- Mummy 공격: Animator 상태 추가 후 Attack Patterns 배열에 항목 추가
- Zombie 공격: 공격 종류, 선택 가중치, Detector와 Animation Event 연결
- Demon 공격: DemonSwordsmanBossSettings에 공격 패턴 추가
- Undead 행동: Idle 옆에 Chase, Attack, Hit과 Dead를 작은 단계로 추가

## 9. 오늘 확인한 결과

Unity 자동 확인:

- 프로젝트 C# 동적 컴파일 성공
- Mummy Animator Layer 2개와 Full Body Actions Override 확인
- Mummy 의미 기반 Animator 파라미터 12개 확인
- Mummy 구형 숫자형 Controller 제거 확인
- Mummy 프리팹의 Controller, CharacterController와 MeleeHitDetector 연결 확인
- CharacterTestScene의 Mummy 연결 확인
- CharacterTestScene Missing Script 0개 확인

Mummy Play Mode 확인:

    일반 피해
    → Hit가 현재 행동보다 우선 진입

    치명타
    → IsDead = true
    → Animator.speed = 0
    → 현재 자세 유지

Mummy 공격 설정은 기본값 기준으로 거리, 허용 각도와 1.2초 재사용 시간이 적용되는 것을 확인했다.

Console 확인:

- Mummy 구현과 직접 관련된 Error: 0개
- 확인된 Warning: TextMesh Pro 예제의 deprecated API 경고 8개

## 10. 아직 확인하거나 구현할 작업

1. Player와 Zombie의 모든 공격 클립에 현재 Event 함수가 정확히 연결됐는지 검사
2. Swing, Kick, UpDown Detector 위치와 Layer Mask를 Scene View Gizmo로 확인
3. 15/30/60 FPS에서 공격당 같은 대상이 한 번만 맞는지 확인
4. Zombie 공격 중 피격 시 판정이 즉시 닫히는지 확인
5. Zombie 사망 애니메이션과 사체 유지 시간 후 풀 회수 확인
6. Demon Swordsman의 실제 무기 판정과 플레이어 피해 연결
7. Demon Swordsman 피격·사망 상태 추가 여부 결정
8. Undead Warrior 탐지와 이동 구현
9. Mummy 전용 Death 클립이 생기면 기존 Death 진입 지점에 연결

## 11. 다음 작업 순서

    Player·Zombie Animation Event 전수 확인
    → Zombie 양방향 전투 Play 검증
    → Demon Swordsman 무기 판정 연결
    → Demon 피격·사망 설계
    → Undead 탐지·추적 구현
    → Mummy 추가 공격 패턴과 Death 클립 연결

다음 작업에서도 새 시스템을 한꺼번에 만들지 않는다. 현재 AttackHitData, IAttackHitReceiver, MeleeHitDetector, UnitHealth와 WorldObjectManager 흐름을 유지하면서 적 하나와 행동 하나씩 검증한다.
