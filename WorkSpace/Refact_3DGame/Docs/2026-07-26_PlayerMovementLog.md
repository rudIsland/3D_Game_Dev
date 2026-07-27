# 플레이어 이동 구현 기록

- 작업 날짜: 2026-07-26
- 구현 기능: 플레이어 입력, 이동, 회전, 중력, 이동 애니메이션
- 개발 씬: `Assets/_Project/Scenes/Dev/CharacterTestScene.unity`

## 1. 구현 목표

플레이어가 카메라 방향을 기준으로 걷고 달리며, 이동 방향으로 회전하도록 구현한다. 실제 이동 속도에 맞춰 Idle, Walk, Run 애니메이션이 자연스럽게 전환되도록 구성한다.

## 2. 입력 구성

관련 파일:

- `Assets/_Project/Characters/Player/Input/PlayerInput.inputactions`
- `Assets/_Project/Characters/Player/Input/PlayerControls.cs`
- `Assets/_Project/Characters/Player/Input/PlayerInputReader.cs`

`Player` Action Map에 다음 Input Action을 구성했다.

| Input Action | 값 형식 | 연결된 입력 | 역할 |
|---|---|---|---|
| `Look` | `Vector2` | 마우스 이동 | Cinemachine 카메라 회전 |
| `Move` | `Vector2` | `W`, `A`, `S`, `D` | 플레이어 이동 방향 |
| `Sprint` | `Button` | `Left Shift` | 걷기와 달리기 전환 |

입력 전달 흐름:

```text
W, A, S, D
→ Move Input Action
→ PlayerControls 콜백
→ PlayerInputReader.OnMove()
→ MoveValue 저장
→ PlayerMovement가 이동 방향 계산
```

`PlayerInputReader`가 관리하는 값과 생명주기:

- `MoveValue`: 현재 이동 방향
- `IsSprinting`: 현재 달리기 입력 여부
- `Create`: 입력 객체 생성과 콜백 연결
- `Enable`: Player Action Map 활성화
- `Disable`: 입력 비활성화와 저장된 값 초기화
- `Destroy`: 콜백 해제와 입력 객체 정리

`Look` 입력은 같은 Input Action Asset을 사용하는 `CinemachineInputProvider`가 처리하므로 `PlayerInputReader.OnLook()`에서는 별도 동작을 수행하지 않는다.

## 3. 플레이어 이동 구현

관련 파일:

- `Assets/_Project/Characters/Player/PlayerController.cs`
- `Assets/_Project/Characters/Player/Movement/PlayerMovement.cs`

이동 처리 흐름:

```text
MoveValue 읽기
→ 입력 크기를 1 이하로 제한
→ MainCamera의 앞쪽과 오른쪽 방향을 바닥 평면에 맞춤
→ 카메라 기준 이동 방향 계산
→ 이동 방향으로 플레이어 회전
→ 접지 상태와 중력으로 수직 속도 계산
→ 걷기 또는 달리기 속도 선택
→ CharacterController.Move()
```

이동 설정:

| 설정 | 값 |
|---|---:|
| 걷기 속도 | `2.8` |
| 달리기 속도 | `5.5` |
| 회전 속도 | 초당 `720`도 |
| 중력 | `-22` |
| 바닥에 붙이는 수직 속도 | `-2` |

구현 내용:

- 대각선 입력을 정규화하여 대각선 이동이 더 빨라지는 현상 방지
- 카메라의 위아래 기울기를 제거하여 바닥 평면에서 이동
- `Quaternion.RotateTowards`를 사용하여 이동 방향으로 부드럽게 회전
- `CharacterController.isGrounded`를 확인하여 지면에서 중력 속도 초기화
- `Left Shift` 입력에 따라 걷기 속도와 달리기 속도 선택

## 4. Update를 사용한 이유

현재 플레이어는 `Rigidbody`가 아니라 `CharacterController.Move()`로 이동한다.

```text
PlayerController.Update()
→ PlayerMovement.Update(Time.deltaTime)
→ CharacterController.Move(속도 × deltaTime)
```

`Time.deltaTime`을 이동량에 곱하므로 렌더링 프레임 수가 달라도 초당 이동 속도가 유지된다. `FixedUpdate`는 `Rigidbody.AddForce`나 `Rigidbody.MovePosition`처럼 Unity 물리 주기에 맞춰야 하는 이동을 구현할 때 사용한다.

따라서 현재 `CharacterController` 이동은 `Update`에서 처리한다.

## 5. 이동 애니메이션 연결

관련 파일:

- `Assets/_Project/Characters/Player/Animations/Controllers/PlayerMovement.controller`
- `Assets/_Project/Characters/Player/Animations/Clips/Player_Idle.anim`
- `Assets/_Project/Characters/Player/Animations/Clips/Player_Walk.anim`
- `Assets/_Project/Characters/Player/Animations/Clips/Player_Run.anim`

`PlayerMovement.controller`에 `MoveAmount` 파라미터를 사용하는 Blend Tree를 구성했다.

| MoveAmount | 애니메이션 |
|---:|---|
| `0` | Idle |
| 약 `0.51` | Walk |
| `1` | Run |

애니메이션 처리 흐름:

```text
CharacterController의 실제 수평 속도
→ 달리기 최대 속도로 나누기
→ 0~1 범위로 제한
→ MoveAmount에 전달
→ Idle, Walk, Run 혼합
```

적용 내용:

- `Animator.StringToHash("MoveAmount")` 결과를 정적 필드에 저장
- 매 프레임 문자열로 Animator 파라미터를 검색하지 않도록 구성
- 코드 이동과 애니메이션 이동이 중복되지 않도록 Root Motion 비활성화
- `animationSmoothTime`을 `0.12`초로 설정하여 전환값을 부드럽게 변경

## 6. 생명주기와 책임 구분

`PlayerController`는 Unity와 일반 C# 로직을 연결하는 역할을 담당한다.

```text
PlayerController.Awake
→ CharacterController와 Animator 확인
→ PlayerInputReader 생성
→ PlayerMovement 생성

PlayerController.OnEnable
→ PlayerInputReader.Enable

PlayerController.Update
→ PlayerMovement.Update
→ 이동 애니메이션 갱신

PlayerController.OnDisable
→ PlayerInputReader.Disable

PlayerController.OnDestroy
→ PlayerInputReader.Destroy
```

Unity가 직접 호출하는 `Awake`, `OnEnable`, `Update`, `OnDisable`, `OnDestroy`는 `PlayerController`에 유지했다. Unity 생명주기에 직접 묶일 필요가 없는 입력 객체의 생성, 활성화, 비활성화와 해제는 `PlayerInputReader`가 명시적인 메서드로 관리한다.

현재 규모에서는 `PlayerController`의 생명주기까지 별도 실행 클래스로 옮기면 호출 클래스만 늘어나므로 분리하지 않았다. 런타임 플레이어 교체, 오브젝트 풀, 리플레이 또는 Unity 외부 테스트가 필요해질 때 별도 실행 객체 분리를 검토한다.

## 7. 이름공간

플레이어 코드를 역할별 이름공간으로 구성했다.

```text
rudIsland.RPG3D.Player
rudIsland.RPG3D.Player.Input
rudIsland.RPG3D.Player.Movement
```

## 8. 현재 구현 범위

완료한 기능:

- 카메라 자유 시점
- `WASD` 걷기
- `Left Shift` 달리기
- 카메라 방향 기준 이동
- 이동 방향 회전
- 중력과 지면 유지
- Idle, Walk, Run 애니메이션 혼합

아직 구현하지 않은 기능:

- 점프 입력과 점프 높이 계산
- 점프 시작, 공중, 착지 애니메이션 전환
- 공격 중 이동 제한
- 게임패드 이동 Binding

## 9. 플레이 모드 확인 항목

1. `WASD` 입력과 카메라 방향이 일치하는지 확인
2. 대각선 이동 속도가 직선 이동보다 빨라지지 않는지 확인
3. `Left Shift`를 누르고 놓을 때 걷기와 달리기가 전환되는지 확인
4. 경사로, 계단과 충돌 통로에서 `CharacterController`가 정상 동작하는지 확인
5. Idle, Walk, Run 전환이 실제 이동 속도와 일치하는지 확인
6. Unity Profiler에서 이동 중 반복적인 `GC Alloc`이 발생하지 않는지 확인

## 10. 다음 작업

1. 플레이 모드에서 기본 이동과 애니메이션 검증
2. `Jump` Input Action 추가
3. 점프와 중력 계산 책임 구분
4. Jump Start, In Air, Jump Land 애니메이션 연결
5. Unity Profiler를 사용한 이동 구간 GC 확인


# PlayerInputReader 관련 내용
InputAction은 키보드나 마우스 입력을 Move, Sprint 같은 게임 행동으로 바꿔
  주는 Unity Input System 객체다.

  InputAction에는 이름, 종류, 전달할 값의 형식, 연결된 입력과 현재 입력 단계
  가 있다.

  입력 단계는 다음과 같이 바뀐다.

  - started: 입력 시작
  - performed: 입력 조건 만족
  - canceled: 입력 해제 또는 취소

  InputAction은 캐릭터를 직접 움직이지 않는다. 입력이 발생했다는 사실과 입력
  값을 다른 코드에 전달한다. 전달받은 값으로 캐릭터를 움직이는 일은
  PlayerMovement가 담당한다.

  Binding은 실제 키와 InputAction을 연결하는 설정이다.

  현재 Move Action은 2DVector Composite를 사용한다. 이 설정이 W, A, S, D를 하
  나의 Vector2 방향값으로 합친다.

  W → (0, 1)
  S → (0, -1)
  A → (-1, 0)
  D → (1, 0)

  PlayerInputReader는 다음 코드로 자신을 입력 수신자로 등록한다.

  playerControls.Player.SetCallbacks(this);

  SetCallbacks(this)가 실행되면 기존 수신자의 콜백 연결을 해제하고 목록을 비
  운 뒤, 현재 PlayerInputReader를 새 수신자로 등록한다.

  PlayerControls에서는 다음과 같은 방식으로 InputAction과 콜백 메서드를 연결
  한다.

  Move.started += instance.OnMove;
  Move.performed += instance.OnMove;
  Move.canceled += instance.OnMove;

  여기서 등록하는 것은 InputAction 자체가 아니다. OnMove, OnSprint 같은 수신
  자의 메서드를 InputAction 이벤트에 연결한다.

  Move 입력이 발생하면 OnMove()가 호출된다.

  public void OnMove(InputAction.CallbackContext context)
  {
      MoveValue = context.ReadValue<Vector2>();
  }

  context.ReadValue<Vector2>()로 현재 이동 방향을 읽고 MoveValue에 저장한다.

  Sprint 입력도 같은 방식으로 처리한다.

  public void OnSprint(InputAction.CallbackContext context)
  {
      IsSprinting = context.ReadValueAsButton();
  }

  Left Shift를 누르면 true, 놓으면 false가 IsSprinting에 저장된다.

  .inputactions 파일은 입력 설정을 JSON 형식으로 저장한다. JSON은 입력을 빠르
  게 처리하려고 사용하는 형식이 아니다. PlayerControls 객체를 만들 때 저장된
  입력 설정을 불러오는 데 사용한다.

  InputActionAsset.FromJson()

  InputActionMap의 Map도 Dictionary나 Hash Map 같은 자료구조를 뜻하지 않는다.
  관련된 InputAction을 하나로 묶은 그룹이다.

  Player Action Map
  ├─ Look
  ├─ Move
  └─ Sprint

  Action Map으로 관련 입력을 한 번에 켜거나 끌 수 있다.

  playerControls.Player.Enable();
  playerControls.Player.Disable();

  현재 입력 전달 순서는 다음과 같다.

  키보드 입력
  → Binding이 실제 키와 Action 연결
  → InputAction 상태 변경
  → PlayerInputReader 콜백 호출
  → MoveValue 또는 IsSprinting 저장
  → PlayerMovement가 저장된 값을 읽어 이동

왜 json과 map으로 입력을 구성했는지는 예상이지만 json은 컴퓨터가 읽기 쉽고빠른 형태고 map은 O(1)의 시간복잡도를 가진 자료구조로 빠른 입력이 필요한 로직에 적합하여 구성된 것으로 생각된다.