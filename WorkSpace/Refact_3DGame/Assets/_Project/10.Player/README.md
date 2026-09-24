# 플레이어

플레이어의 입력, 이동, 전투, 카메라 전환, 상호작용과 성장 기능을 관리한다. 배치용 프리팹은 [PlayerRoot.prefab](../Runtime/Characters/PlayerRoot.prefab)에 있다.

## 폴더 구성

[PlayerController](Lifecycle/PlayerController.cs)는 초기화를 마친 플레이어 한 명을 Instance로 제공하고 Unity 객체의 등록·활성·해제 경계를 담당한다. GameManager는 Addressables 원본 요청·생성 객체를 소유하고 Init에 이동 카메라를 전달한다. PlayerWorldUnit은 Unit의 기존 생명주기를 사용한다. 별도 생명주기 관리자나 등록 목록은 없다. HUD·퀘스트는 전달받은 참조로 구독·해제한다. [진입점과 실행 순서](../01.Boot/README.md)를 참고한다.

| 폴더 | 설명 |
| --- | --- |
| `Lifecycle` | `PlayerUnit`, Unity 참조 연결과 플레이어 런타임 객체의 생명주기 |
| `Input` | Input System 입력과 누른 행동 저장 |
| `Movement` | 속도·중력·충돌 이동, 자유 시점·락온 방향, 행동 이동 곡선 |
| `StateMachine` | 시점·행동·피격·사망 상태와 공격·구르기 입력 예약 |
| `Combat` | 공격 범위, 무기 궤적, 방어 판정과 피해 수신 |
| `Animation`, `Audio` | Animator 파라미터, 애니메이션 이벤트와 효과음 |
| `Camera` | 락온 카메라 제어 |
| `Interaction` | 상호작용 대상 탐지와 실행 |
| `Inventory`, `Stats` | 가방, 스태미나와 능력치 강화 |
| `Config`, `Configs` | 설정 타입과 실제 설정 에셋 |
| `Models` | 모델, 텍스처, 애니메이션 원본·클립·Controller |

## 생성과 갱신 흐름

1. GameManager가 비활성 GameObjects 아래에 PlayerRoot를 생성하고 MapScene의 StartArrival 위치·수평 방향을 적용한다.
2. `PlayerController.Init(MainCamera.transform)`이 씬 인스턴스·중복 등록을 검사하고 CreatePlayerObjects를 호출한다. 내부 PlayerWorldUnit.Init → Create 후 Instance를 등록한다. 활성 객체라면 Enable까지 호출한다. GameManager는 비활성 부모 아래에서 호출해 조기 입력을 막는다. 상호작용 감지기의 Unity Start는 유지한다.
3. HUD 연결 후 부모를 활성화하면 `OnEnable` → PlayerController.Enable → PlayerWorldUnit.Enable로 입력이 시작된다. `Boots.Update` → `GameManager.Tick` → `PlayerController.Tick` → `PlayerWorldUnit.Tick`에서 타격 정지, 상호작용, 상태머신과 스태미나 회복을 처리한다. IsPaused면 PlayerController가 갱신을 건너뛴다.
4. 상태머신이 현재 행동에 맞는 이동·공격·방어·애니메이션 처리를 호출한다.
5. 반환 시 Disable → HUD 구독 해제 → PlayerController.Release → PlayerWorldUnit.Dispose가 자원을 정리하고 Instance 등록도 지운다. Release는 비활성화를 자동 호출하지 않으므로 PlayerController가 먼저 Disable한다. GameManager가 Unity 객체 파괴 완료 후 원본 요청을 반환한다. 초기화 실패도 같은 정리 경로를 사용하며 해제한 객체는 재초기화하지 않는다.

## 단일 플레이어와 원본 수명

- Instance는 초기화한 플레이어를 제공하는 단순 프로퍼티다. 생성 전·해제 후에는 null이며, 호출자가 필요한 시점에 확인한다. 검색·자동 로드·자동 생성·DontDestroyOnLoad는 하지 않는다.
- 다른 플레이어가 등록되어 있으면 초기화를 거절한다. 같은 객체·카메라의 중복 Init은 재생성이나 재구독 없이 종료한다. 실패한 객체의 Release는 다른 플레이어의 등록에 영향을 주지 않는다.
- Disable은 입력과 행동만 멈추며 Instance·체력·인벤토리·강화 기록을 유지한다. Release는 최종 해제이며 자신의 등록을 지운다. OnDestroy도 Release를 호출한다. 별도 Play 초기화 콜백 없이 매 실행 새 객체를 생성·등록한다.
- 원본 프리팹과 편집 상태의 객체는 등록하지 않는다. Init은 카메라 필드를 바꾸기 전에 이 조건부터 확인한다.
- 싱글톤 참조는 Addressables 핸들이 아니다. 원본 요청과 파괴 완료 대기는 GameManager·AddressableManager의 기존 계약을 따른다.

## 이동 입력이 화면에 반영되는 순서

`PlayerInputReader` → `PlayerMoveState` → `PlayerMovement` → `CharacterController.Move` → 실제 수평 이동 속도 계산 → `PlayerAnimationController.UpdateLocomotion` 순서다.

자유 이동은 카메라 기준, 락온 이동은 대상 기준으로 방향을 계산한다. 가속·감속·반대 방향 전환 가속도를 구분하고, 충돌 후 실제 이동 속도를 걷기·달리기 애니메이션에 반영한다. 구르기와 공격 전진은 애니메이션 진행률에 따른 이동 곡선을 사용한다.

방어 준비 중에는 방어 방향으로 회전하고, 준비가 끝나고 방어 대기 애니메이션에 들어가면 방어 이동을 허용한다. 공격·구르기는 짧은 입력 예약을 사용한다. 발을 지형에 맞추는 IK는 이 이동 처리에 포함되어 있지 않다.

## 수정 목적별 시작 파일

| 수정할 내용 | 시작 파일 |
| --- | --- |
| 이동 속도·회전·스태미나 수치 | [PlayerCharacterConfig.asset](Configs/PlayerCharacterConfig.asset) |
| 이동·충돌 결과 처리 | [PlayerMovement.cs](Movement/PlayerMovement.cs) |
| 걷기·달리기 표현 | [PlayerAnimationController.cs](Animation/PlayerAnimationController.cs) |
| 행동 전환·입력 예약 | [PlayerActionStateMachine.cs](StateMachine/Actions/PlayerActionStateMachine.cs) |
| 공격별 설정 | `StateMachine/States/Attack/AttackData` |
| 초기 연결·등록 | [PlayerController.cs](Lifecycle/PlayerController.cs) |

## Unity에서 확인

플레이어의 `Test Damage` 메뉴는 같은 Lifecycle 폴더의 [PlayerCheat.cs](Lifecycle/PlayerCheat.cs)에 분리했다. PlayerController의 Editor 전용 partial 선언이므로 별도 컴포넌트 부착 없이 기존 Inspector 메뉴에서 피해 처리를 호출한다.
분리 후 Unity 컴파일은 통과했으며, 피해 메뉴의 실제 Play 실행은 이번 분리 작업에서 재검증하지 않았다.

- Start를 단독으로 실행한다. 이동 카메라는 GameManager가 전달하며, PlayerRoot 프리팹의 두 시점 카메라·Config·상호작용 컴포넌트를 확인한다.
- Animator, CharacterController, 무기 판정과 방패 판정 연결을 확인한다.
- 걷기·달리기, 벽 앞 이동, 락온 옆걸음, 구르기와 방어 전환을 확인한다.
- 이동 애니메이션의 `MovePlaybackSpeed` 연결과 실제 이동 속도가 함께 반영되는지 확인한다.

관련 문서: [공통 캐릭터](../13.Characters/README.md), [아이템](../12.Item/README.md), [게임 생성과 반환](../05.Manager/README.md).

## 확인과 남은 작업

Instance를 단순 프로퍼티로 정리하고 Play 초기화 콜백을 제거한 최종 코드에서도 컴파일, 플레이어 1개 생성·등록, 반환 후 Instance null과 플레이어·에셋·씬 요청 0을 확인했다. 이 최종 코드의 Domain Reload 비활성 재시작은 별도로 검증하지 않았다.

Unity 컴파일과 Play에서 단일 등록, 중복 Init의 내부 객체 유지, 비활성화·재활성화의 Instance 유지, 중복 객체·관리자 시작 거절, 원본 프리팹 초기화 거절을 확인했다. 명시적 반환 직후 Instance 해제와 파괴 완료 전 원본 요청 유지, 반환 완료 후 객체·에셋·씬 요청 0을 확인했다. Config를 비운 임시 객체의 초기화 실패 후 미등록·반환 후 재사용 거절·요청 0도 확인했다. 아래는 변경 전 시작 흐름에서 확인한 결과다.

Start 단독 Play에서 초기 체력 100, 이동·회전·카메라 추적, 달리기 소비·회복, 분리한 Space 입력의 구르기 비용 25와 HUD 갱신을 확인했다. TestDamage 후 HUD 90/100과 반환 시 입력·플레이어 제거도 확인했다. 적 실행과 락온 전투의 통합 확인은 다음 단계이며 전체 시작 검증은 [진입점 안내](../01.Boot/README.md#확인과-남은-작업)에 기록한다.
