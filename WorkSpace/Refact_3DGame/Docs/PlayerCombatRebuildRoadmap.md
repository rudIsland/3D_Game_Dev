# 플레이어 전투 재구현 로드맵

## 목표

플레이어 공격을 다음 한 문장으로 이해할 수 있는 구조로 다시 만든다.

> 공격 버튼을 누르면 플레이어 상태머신이 공격 애니메이션을 재생하고, 애니메이션의 Attack Window 동안 범위 안의 적에게 한 번 데미지를 준다.

처음부터 패링, 가드, 공격 강도, 밀치기, `AttackHitInput`, `HitBox`를 넣지 않는다. 기본 공격이 눈으로 확인되고 코드 흐름으로 설명될 때까지 전투 규칙을 하나씩만 추가한다.

## 현재 백업 위치

기존 공유 Combat 코드는 다음 위치에 보관되어 있다.

`Backups/CombatBeforeStateMachineRebuild_20260808_131334`

특히 Assets에서 제거된 원본은 다음 하위 폴더에 있다.

`Backups/CombatBeforeStateMachineRebuild_20260808_131334/RemovedFromAssets/Runtime/Combat`

필요하면 이 백업을 참고하되, 새 코드에 복잡한 구조를 그대로 되돌려 넣지 않는다.

## 단계별 작업 순서

### 0단계. 기존 Combat 백업

- 공유 `Assets/_Project/Runtime/Combat` 코드를 백업한다.
- 원본에는 `AttackHitInput`, `AttackHitSettings`, `MeleeHitDetector`, `HitReaction`, `UnitHitResult` 같은 여러 개념이 섞여 있었다.
- 이 단계는 완료했다.

### 1단계. 옛 Combat 의존성 제거

목표는 새 전투 기능을 만들기 전에 프로젝트가 옛 Combat 없이 컴파일되는 상태를 만드는 것이다.

완료한 작업:

- `Unit`에서 옛 피격 입력·결과 계산 의존성을 제거했다.
- Player와 Enemy Controller에서 `MeleeHitDetector`, `AttackHitSettings`, `IAttackHitReceiver` 의존성을 제거했다.
- Player와 Enemy의 피격은 당분간 단순한 `TakeDamage(float)`와 상태머신의 피격 상태만 사용한다.
- Player 대상 찾기는 `Unit`을 직접 찾지 않고 적 컴포넌트의 `IUnitDeathState`를 찾는다.
- 씬과 프리팹에 남은 `MeleeHitDetector`, `CombatHitResolver`, `UnitHitBox` 직렬화 블록을 제거했다.
- 적별 공격 선택·설정 코드는 `Code/Combat`에서 `Code/Attack`으로 이름을 바꿨다. 이것은 공유 Combat 시스템이 아니라 적의 공격 애니메이션 선택용 코드다.
- `UnitDefenseStatus`, `HitPushMovement`, `UnitMovementSeparation`, `UnitStagger`, `UnitTeam`, `UnitStamina` 로직을 제거했다.
- Player와 Enemy 이동은 현재 `CharacterController.Move`와 중력만 사용한다.
- 피격 상태는 애니메이션만 재생하고 밀치기 이동은 사용하지 않는다.
- 제거한 Runtime 파일과 관련 테스트는 백업 폴더의 `RemovedFromAssets/RuntimeCleanup`에 보관했다.

확인 결과:

- `rudIsland.RPG3D.Runtime.csproj`: 오류 0개
- `Assembly-CSharp.csproj`: 오류 0개
- 현재 Assets의 C#·씬·프리팹에서 제거한 Runtime 로직과 옛 Combat 타입 이름을 찾지 못했다.

### 2단계. 플레이어 공격 버튼과 상태머신 연결

입력 → 처리 → 출력 순서만 만든다.

1. 플레이어 입력 코드가 공격 버튼을 받는다.
2. 입력 코드는 `PlayerStateMachine`에 공격을 요청한다.
3. `PlayerStateMachine`은 현재 상태가 공격 가능한지 확인한다.
4. 가능하면 `PlayerAttackState`로 바꾼다.
5. `PlayerAttackState`가 공격 애니메이션을 재생한다.
6. 애니메이션이 끝나면 상태머신이 이전 상태 또는 대기 상태로 돌아간다.

이 단계에서는 적을 찾거나 데미지를 주지 않는다. 먼저 공격 애니메이션이 재생되는지만 확인한다.

확인할 것:

- 공격 버튼을 눌렀을 때 PlayerStateMachine의 공격 전환이 한 번만 일어나는가?
- 이동·피격·사망 중 공격을 막을 것인가?
- 공격 애니메이션 종료 시 상태가 반드시 풀리는가?

### 3단계. 애니메이션 Attack Window 열고 닫기

애니메이션 클립에 두 개의 이벤트만 추가한다.

- `StartAttackHit`: 공격 판정을 시작한다.
- `EndAttackHit`: 공격 판정을 끝낸다.

처음 구현에서는 이벤트가 Player Controller의 간단한 메서드를 호출하고, Player Attack State가 `isAttackWindowOpen` 값을 관리해도 된다.

중요한 규칙:

- 애니메이션 전체 시간이 공격 시간이 아니다.
- `StartAttackHit`과 `EndAttackHit` 사이만 공격할 수 있다.
- 이 단계에서도 아직 `AttackHitInput`과 별도 `HitBox`를 만들지 않는다.

### 4단계. 열린 Window 안에서 간단한 범위 검사

공격 Window가 열려 있는 동안 플레이어 주변의 공격 범위를 검사한다.

처음에는 다음처럼 단순하게 유지한다.

- `Physics.OverlapSphereNonAlloc` 사용
- 공격 위치는 플레이어 앞쪽 한 곳
- 대상 레이어는 Enemy 한 종류
- 실제 적의 Collider는 사용
- 별도의 `HitBox` 컴포넌트는 만들지 않음

범위 검사 코드는 매 프레임 실행할 수 있지만, `Collider[]` 배열은 미리 만들어 GC 할당을 피한다.

이 단계의 출력은 “범위 안에서 적 Collider를 찾았다”까지다. 데미지 계산은 다음 단계에서 한다.

### 5단계. 찾은 적에게 고정 데미지 전달

찾은 Collider의 부모에서 적 Controller 또는 적 World Unit을 찾는다.

처음에는 다음 흐름으로 제한한다.

1. Collider를 찾는다.
2. 적의 대상 컴포넌트를 찾는다.
3. `TakeDamage(10f)`를 호출한다.
4. 적 Health가 줄어든다.
5. 적이 피격 상태로 바뀐다.
6. Health가 0이면 사망 상태로 바뀐다.

이 단계에서 만들 값은 `damage = 10f` 하나뿐이다. 공격 강도, 방어력, 스태거 수치, 방향별 반응은 넣지 않는다.

### 6단계. 같은 공격에서 같은 적 한 번만 맞히기

Attack Window가 열릴 때 맞힌 적 목록을 비운다.

Window가 닫힐 때까지 같은 대상은 다시 데미지를 받지 않게 한다.

처음에는 `HashSet`보다 작은 `Transform[]` 또는 `List<Transform>`로 흐름을 확인해도 된다. 대상 수가 적고 공격 한 번당 목록을 새로 만들지 않도록 재사용한다.

### 7단계. 플레이어 공격 하나 완성

다음 조건을 만족하면 플레이어 기본 공격을 완료한 것으로 본다.

- 버튼 입력으로 공격 상태에 들어간다.
- 공격 애니메이션이 재생된다.
- Attack Window가 애니메이션 이벤트로 열린다.
- Window 안의 적만 맞는다.
- 같은 적은 한 번만 맞는다.
- 고정 데미지가 전달된다.
- Window와 공격 상태가 반드시 닫힌다.

### 8단계. 적 공격에 같은 구조 적용

플레이어 공격이 확인된 후에만 적 공격을 같은 순서로 적용한다.

적마다 별도의 Combat 시스템을 만들지 않고, 공격자와 공격 애니메이션만 다르게 둔다.

### 9단계. 나중에 추가할 규칙

기본 공격이 안정된 뒤 아래 순서로 하나씩 추가한다.

1. 공격 데이터 클래스
2. 공격별 데미지
3. 공격 방향
4. 팀 구분
5. 가드
6. 패링
7. 스태거와 밀치기
8. `AttackHitInput` 같은 입력 데이터 묶음
9. 여러 공격 모양 또는 특수 공격 범위

각 규칙은 추가할 때마다 “누가 입력하고, 누가 처리하고, 무엇이 출력되는가”를 먼저 기록한다.

## 새 공격 흐름 요약

```text
공격 버튼
  -> PlayerStateMachine
  -> PlayerAttackState
  -> 공격 애니메이션 재생
  -> StartAttackHit 이벤트
  -> 간단한 범위 검사
  -> 적 Collider 찾기
  -> 적 TakeDamage(10)
  -> 적 피격 또는 사망 상태
  -> EndAttackHit 이벤트
  -> PlayerAttackState 종료
```

## 지금 하지 않는 것

- `AttackHitInput` 전달
- `HitBox` 전용 컴포넌트
- `MeleeHitDetector`
- `CombatHitResolver`
- 패링·가드
- 공격 강도와 방어력 계산
- 밀치기 방향 계산
- 공격 결과 객체와 피격 반응 객체 분리

이 항목들은 기본 공격의 흐름이 완성된 뒤 필요한 경우에만 추가한다.

## 다음 실습

다음 작업은 2단계다.

1. `PlayerStateMachine`에서 공격 상태로 전환한다.
2. `PlayerAttackState`에서 공격 애니메이션을 재생한다.
3. 공격 상태가 끝나는 시점을 확인한다.
4. Unity Animator에서 Attack Window 이벤트를 연결한다.

2단계가 확인되기 전에는 범위 검사나 데미지 코드를 추가하지 않는다.