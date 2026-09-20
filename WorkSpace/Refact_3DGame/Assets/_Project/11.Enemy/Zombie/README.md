# 좀비

지상 구역에서 플레이어를 탐지하고 추격·공격하며, 전투가 끝나면 귀환하는 적이다. 실제 배치용 프리팹은 [Prefabs/Zombie.prefab](Prefabs/Zombie.prefab)이다.

## 폴더 구성

| 폴더 | 설명 |
| --- | --- |
| `Lifecycle` | Unity 연결용 `ZombieController`와 실행용 `ZombieWorldUnit` |
| `StateMachine/States` | 생존·피격·사망 상태와 상태 전환 |
| `StateMachine/States/Alive` | 대기·경계·추격·공격·귀환 행동 |
| `Movement` | CharacterController 이동과 경로 안내 연결 |
| `Attack` | 공격 범위와 타격 판정 |
| `Animation` | Animator 제어와 공격·경계 동작 이벤트 |
| `Config`, `Configs` | 설정 타입, `ZombieConfig.asset`, `ZombieSpawnSettings.asset` |
| `Models` | 모델과 애니메이션 리소스 |
| `Models/Animations/Archive` | 이전 프리팹과 사용하지 않는 동작 보관 |

## 동작 흐름

1. 소환 요청으로 풀에서 프리팹을 꺼내 위치·회전을 지정한다.
2. `EnemyUnit`이 체력을 초기화하고 `ZombieWorldUnit`이 경직·타격 정지·공격 판정과 상태머신을 준비한다.
3. 대상과 소속 Zone 정보를 읽어 대기·경계·추격·공격·귀환을 결정한다.
4. 경로 방향을 바탕으로 이동하고 애니메이션 이벤트에 맞춰 공격 판정을 연다.
5. 피해는 체력·경직과 피격 상태에 반영한다. 사망 상태 처리 후 회수를 요청한다.

`ZombieWorldUnit`의 Tick은 타격 정지 확인 → 상태 갱신 → 공격 판정 → 귀환 후 회복 순서다.

## 수치와 표시

- 탐지 등 전투 수치는 [ZombieConfig.asset](Configs/ZombieConfig.asset)에서 확인한다. 현재 탐지 거리 설정은 14m다.
- `IEnemyCombatStatus`로 체력과 전투 상태를 제공한다.
- 화면 HUD 체력바는 사용하지 않고, 머리 위 체력바의 표시는 `UI/CombatHud/EnemyHeadHealthBar.cs`에서 처리한다.

## Unity에서 확인

- Controller의 Animator, Config, 공격별 HitShape 연결을 확인한다.
- 소환 설정이 WorldObjectManager에 등록되어 있는지 확인한다.
- Zone의 BoxCollider, 플레이어, 소환 지점과 NavMesh를 확인한다.
- 현재 Zone 재소환은 시작 시와 플레이어가 구역 밖에서 안으로 들어올 때 빈 자리를 채우는 방식이다. 처치 후 제자리에서 기다리는 시간제 재소환은 아니다.
- 체력바는 전투 상태와 연동되므로 탐지·전투 해제 때 표시 변화를 확인한다.

관련 문서: [월드 객체·Zone](../../02.Core/WorldObjects/README.md), [적 공통](../Shared/README.md).
