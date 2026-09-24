# 플레이어

플레이어의 입력, 이동, 전투, 카메라 전환, 상호작용과 성장 기능을 관리한다. 배치용 프리팹은 [PlayerRoot.prefab](../Runtime/Characters/PlayerRoot.prefab)에 있다.

## 폴더 구성

[PlayerController](Lifecycle/PlayerController.cs)가 플레이어 상태·인벤토리·강화 기록을 직접 소유한다. HUD·퀘스트는 이 컨트롤러를 전달받는다. Create와 Tick은 명시적으로 호출하는 구조이며 Boots의 자동 실행은 아직 연결하지 않았다. [진입점과 이전 순서](../01.Boot/README.md)를 참고한다.

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

1. `PlayerController.Awake`가 Inspector 참조와 설정을 확인하고 일반 C# 객체를 구성한다.
2. `WorldObjectManager.Register`로 `PlayerWorldUnit`을 생성하고, Controller의 활성·비활성 상태에 맞춰 관리자를 호출한다.
3. 관리자의 `Tick`에서 타격 정지, 상호작용, 상태머신과 스태미나 회복을 처리한다.
4. 상태머신이 현재 행동에 맞는 이동·공격·방어·애니메이션 처리를 호출한다.
5. 해제 시 입력과 이벤트 연결을 정리한다.

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

- `PlayerController`의 WorldObjectManager, 이동 카메라, 두 시점 카메라와 Config 연결을 확인한다.
- Animator, CharacterController, 무기 판정과 방패 판정 연결을 확인한다.
- 걷기·달리기, 벽 앞 이동, 락온 옆걸음, 구르기와 방어 전환을 확인한다.
- 이동 애니메이션의 `MovePlaybackSpeed` 연결과 실제 이동 속도가 함께 반영되는지 확인한다.

관련 문서: [공통 캐릭터](../13.Characters/README.md), [아이템](../12.Item/README.md), [월드 객체](../02.Core/WorldObjects/README.md).

플레이어의 `Test Damage` 메뉴는 같은 Lifecycle 폴더의 [PlayerCheat.cs](Lifecycle/PlayerCheat.cs)에 분리했다. PlayerController의 Editor 전용 partial 선언이므로 별도 컴포넌트 부착 없이 기존 Inspector 메뉴에서 피해 처리를 호출한다.
