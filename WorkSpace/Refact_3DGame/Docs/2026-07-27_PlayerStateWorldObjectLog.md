# 플레이어 상태머신과 WorldObject 연결 기록

- 작업 날짜: 2026-07-27
- 구현 기능: 플레이어 상태머신, 방어·구르기 애니메이션 정리, 카메라 계층 분리, WorldObject Tick 연결
- 개발 씬: `Assets/_Project/Scenes/Dev/CharacterTestScene.unity`

## 1. 작업 목표

플레이어 이동, 방어와 구르기를 한 `Update` 안의 조건문으로 처리하지 않고 현재 행동에 맞는 상태로 나눈다. 이후 공격, 연속 공격과 피격 상태를 추가할 수 있도록 상태 전환 위치를 한곳으로 모은다.

월드 객체 공통 생명주기를 검토하고, 첫 적용 범위로 플레이어의 매 프레임 갱신을 `WorldObjectManager`가 호출하도록 변경한다.

## 2. 플레이어 상태머신

관련 파일:

- `Assets/_Project/Characters/Player/States/IPlayerState.cs`
- `Assets/_Project/Characters/Player/States/PlayerStateMachine.cs`
- `Assets/_Project/Characters/Player/States/PlayerMoveState.cs`
- `Assets/_Project/Characters/Player/States/PlayerBlockState.cs`
- `Assets/_Project/Characters/Player/States/PlayerRollState.cs`

상태 공통 규칙:

```text
Enter
→ Update
→ Exit
```

상태별 역할:

| 상태 | 역할 |
|---|---|
| `PlayerMoveState` | 일반 이동과 `MoveAmount` 애니메이션 값 갱신 |
| `PlayerBlockState` | 수평 이동 정지, 중력 유지, 방어 애니메이션 유지 |
| `PlayerRollState` | 구르기 거리 커브, 방향 회전과 구르기 애니메이션 처리 |

상태 전환 우선순위:

```text
구르기 진행 중
→ 구르기가 끝날 때까지 유지

구르기 상태가 아님
→ 방어 입력 확인
→ 구르기 입력 확인
→ 일반 이동
```

상태 객체는 `PlayerStateMachine` 생성 시 한 번만 만들고 재사용한다. 매 프레임 새로운 상태 객체, LINQ 또는 클로저를 만들지 않는다.

## 3. 이동 코드 책임 분리

관련 파일:

- `Assets/_Project/Characters/Player/Movement/PlayerMovement.cs`

`PlayerMovement`는 상태 전환을 결정하지 않고 현재 상태가 요청한 이동만 처리한다.

| 메서드 | 역할 |
|---|---|
| `UpdateMove` | 입력 방향 계산, 가속·감속, 회전, 중력과 일반 이동 |
| `StartBlock` | 방어 시작 시 남아 있는 수평 이동 속도 제거 |
| `UpdateBlock` | 수평 이동 없이 중력과 지면 유지 |
| `TryStartRoll` | 구르기 가능 여부 확인, 시작 방향과 거리 저장 |
| `UpdateRoll` | 시간 커브에 맞춰 회전과 이동 거리 적용 |

애니메이션 Root Motion은 사용하지 않으며 실제 위치 이동은 `CharacterController.Move()`가 담당한다.

## 4. 방어 애니메이션과 회전

방어 중에는 카메라 방향으로 플레이어를 강제로 회전시키지 않는다. 방어를 시작한 현재 플레이어 방향을 유지하고 `PlayerBlockState`는 수평 이동만 막는다.

방어 애니메이션 흐름:

```text
방어 입력 시작
→ IsBlocking = true
→ BlockStart
→ BlockIdle

방어 중 피격
→ BlockImpact
→ BlockIdle

방어 입력 해제
→ 이동 상태
```

현재 사용 중인 추출 `.anim` 클립 세 개의 Root Transform Position Y 설정을 같은 기준으로 맞췄다.

| 클립 | Bake Into Pose Y | Based Upon |
|---|---|---|
| `Player_20_BlockStart.anim` | OFF | Feet |
| `Player_21_BlockIdle.anim` | OFF | Feet |
| `Player_22_BlockImpact.anim` | OFF | Feet |

원본 FBX와 추출된 `.anim`은 서로 독립된 에셋이다. Animator가 추출 `.anim`을 참조하면 FBX Import 설정을 바꾸는 것만으로는 현재 재생 클립 설정이 바뀌지 않는다.

## 5. 구르기 애니메이션 설정 검토

현재 Roll과 SprintRoll은 코드가 이동 거리를 처리한다.

```text
Root Transform Rotation Bake Into Pose: ON
Root Transform Position Y Bake Into Pose: ON
Root Transform Position XZ Bake Into Pose: ON
Root Transform Position Y Based Upon: Original
Animator.applyRootMotion: false
```

`Bake Into Pose`를 켜면 해당 축의 움직임을 뼈 동작에 포함하고, 끄면 Root Motion 값으로 분리한다. 현재는 Root Motion을 사용하지 않으므로 Rotation과 XZ를 Bake하여 플레이어 방향과 이동 거리를 코드가 담당하도록 유지한다.

Roll 시작과 종료에서 높이 튐이 발생하면 Y 기준을 `Feet`로 비교할 수 있지만, 구르기는 발이 머리 위로 올라가는 자세가 포함되므로 현재 저장값인 `Original`을 유지했다.

## 6. 카메라 계층 정리

카메라가 플레이어 이동 회전에 함께 끌려가지 않도록 `CameraGroup`을 `PlayerRoot`의 자식에서 씬 루트로 분리했다.

현재 계층:

```text
Scene Root
├─ CameraGroup
│  └─ Cinemachine FreeLook
└─ PlayerRoot
   └─ CameraTarget
```

`CameraTarget`은 플레이어 위치를 따라가고, `CameraGroup`은 플레이어 Transform 회전을 직접 상속하지 않는다. 카메라는 부모 연결이 아니라 Cinemachine의 Follow와 LookAt으로 플레이어를 추적한다.

## 7. WorldObject 구조 분석

관련 파일:

- `Assets/_Project/Runtime/World/IWorldObject.cs`
- `Assets/_Project/Runtime/World/WorldObject.cs`
- `Assets/_Project/Runtime/World/WorldObjectView.cs`
- `Assets/_Project/Runtime/World/WorldObjectPool.cs`
- `Assets/_Project/Runtime/World/WorldObjectManager.cs`
- `Assets/_Project/Runtime/World/SpawnSettings.cs`

클래스별 역할:

| 클래스 | 역할 |
|---|---|
| `IWorldObject` | `Create`, `Enable`, `Tick`, `Disable`, `Dispose` 생명주기 규칙 |
| `WorldObject` | 중복 호출과 잘못된 호출 순서를 방지하는 기본 구현 |
| `WorldObjectView` | Unity GameObject와 일반 C# RuntimeObject 연결 |
| `WorldObjectPool` | View와 RuntimeObject를 제거하지 않고 재사용 |
| `WorldObjectManager` | 등록, 활성 객체 Tick, Spawn과 Despawn 순서 관리 |
| `SpawnSettings` | 프리팹, 초기 예열 개수와 최대 보관 개수 설정 |

공통 생명주기:

```text
Create
→ Enable
→ Tick
→ Disable
→ Dispose
```

`Disable`은 다시 사용할 수 있는 임시 중지이고 `Dispose`는 이벤트와 입력 같은 연결을 마지막으로 정리하는 단계다. `Dispose`는 메모리를 즉시 제거하거나 `GC.Collect()`를 호출하지 않는다.

Manager는 `List`로 활성 객체를 순회하고 `HashSet`으로 중복 등록을 막는다. Tick 도중 들어온 등록·비활성화·풀 반환 요청은 `PendingAction`에 저장한 후 순회가 끝나면 처리한다.

## 8. 플레이어 Tick 연결

관련 파일:

- `Assets/_Project/Characters/Player/PlayerController.cs`
- `Assets/_Project/Characters/Player/PlayerWorldUnit.cs`

기존 흐름:

```text
PlayerController.Update
→ 입력 확인
→ PlayerStateMachine.Update
```

변경 후 흐름:

```text
WorldObjectManager.Update
→ 활성 객체 Tick
→ PlayerWorldUnit.OnUnitTick
→ 입력 확인
→ PlayerStateMachine.Update
```

`PlayerWorldUnit` 생명주기:

| 단계 | 처리 |
|---|---|
| `Create` | `PlayerInputReader.Create()` |
| `Enable` | 입력과 상태머신 활성화 |
| `Tick` | 구르기·방어 피격 입력 소비와 상태머신 갱신 |
| `Disable` | 상태머신과 입력 비활성화 |
| `Dispose` | Input Actions와 콜백 최종 정리 |

`PlayerController`는 Inspector 참조 확인과 객체 조립을 담당한다. 자체 `Update()`는 제거했으며 Manager에 `PlayerWorldUnit`을 등록, 활성화, 비활성화하고 최종 해제한다.

## 9. 플레이어와 오브젝트 풀의 관계

현재 플레이어는 오브젝트 풀에 들어가지 않는다.

```text
씬에 배치된 PlayerRoot
→ PlayerWorldUnit 생성
→ WorldObjectManager.Register
→ WorldObjectManager.Enable
→ 활성 목록에서 Tick
```

플레이어는 `SpawnSettings`, `WorldObjectView`, `WorldObjectPool`, `TrySpawn`, `Despawn`을 사용하지 않는다. 하나만 존재하며 카메라와 입력이 연결된 씬 고정 객체이므로 Manager의 Tick 관리만 적용했다.

적, 아이템과 투사체처럼 반복 생성되는 객체는 다음 단계에서 View와 Pool을 사용한다.

## 10. GC 관리 기준

현재 Manager와 플레이어 Tick 경로는 다음 원칙을 따른다.

- 상태 객체를 처음에 한 번 생성하고 재사용
- 매 프레임 `for`문으로 기존 목록 순회
- Animator 파라미터 이름을 `StringToHash`로 캐시
- Tick에서 LINQ, 문자열 생성, 새로운 배열과 리스트 생성 방지
- 반복적인 `GetComponent` 호출 없이 `Awake`에서 컴포넌트 캐시
- 풀 객체는 View와 RuntimeObject를 함께 재사용

공통 구조가 할당을 줄여도 이후 추가되는 공격, AI와 탐색의 `OnTick()`에서 `new`, LINQ, 문자열, 클로저를 반복 생성하면 GC가 발생할 수 있다.

## 11. 확인 결과

- 플레이어 이동·방어·구르기 상태 소스 분리 확인
- `PlayerController.Update()` 제거 확인
- `WorldObjectManager`와 PlayerController 씬 참조 확인
- `CameraGroup`이 씬 루트이고 `CameraTarget`이 PlayerRoot 자식인 것 확인
- 방어 Start, Idle, Impact 클립의 Y 기준이 Feet로 동일한 것 확인
- Unity 스크립트 컴파일 성공
- Unity 콘솔 Error와 Exception 없음

## 12. 다음 작업

1. 공격 상태 추가
2. 공격 입력 버퍼와 연속 공격 전환 규칙 설계
3. 피격 상태와 공격 취소 우선순위 결정
4. 적 RuntimeObject와 WorldObjectView 구현
5. 플레이 모드와 Profiler에서 플레이어 Tick의 `GC Alloc` 확인
