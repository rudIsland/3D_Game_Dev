# 적 공통 기능

적 종류와 관계없이 사용하는 전투 정보, 공격 데이터와 NavMesh 경로 안내를 둔다. 공격 패턴과 상태 전환은 각 적 폴더가 담당한다.

## 폴더 구성

[EnemyContainer](EnemyContainer.cs)는 적 등록 시 Unit.Init → Create, 활성화 시 Enable, 갱신 시 Tick, 반환 시 Disable, 등록 제거 시 Release를 호출한다. 풀 재사용은 Disable → Enable이며 기존 풀·목록 관리 방식을 유지한다. [진입점](../../01.Boot/README.md)을 참고한다.

| 위치 | 설명 |
| --- | --- |
| `IEnemyCombatStatus.cs` | 이름, 체력, 경직, 전투 여부와 변경 알림을 UI에 제공 |
| `Lifecycle` | `EnemyUnit`의 적 생명주기와 재활성화 시 체력 초기화 |
| `Combat/Hit` | 적에게 피해를 전달하는 요청·결과와 수신 인터페이스 |
| `AttackData` | 공통 공격 데이터와 공격 선택 관련 설정 |
| `Navigation` | 경로 안내 인터페이스와 NavMeshAgent 기반 구현 |
| `Shaders` | 적 관련 셰이더 리소스 |

## 경로 안내 흐름

대상 위치 입력 → NavMesh 목적지 갱신 → Agent의 이동·회피 방향 읽기 → 적 전용 이동 코드에 방향 반환 순서다. `EnemyNavMeshPathGuide`는 CharacterController가 이동한 실제 위치에 Agent의 계산 위치를 맞춘다. Transform 이동 자체를 Agent에 맡기는 구조가 아니다.

이 경로 안내는 좀비에서 사용한다. NightShade는 전투 범위와 바닥 검사를 포함하는 별도 `NightShadeSwordBattleSpace`를 사용한다.

## 전투 정보 흐름

적 런타임 유닛의 체력·경직·전투 상태 변경 → `IEnemyCombatStatus` 정보와 이벤트 → UI 갱신 순서다. `ShowScreenHealthBar`는 화면 HUD 표시 여부이며, 모든 체력바를 일괄 제어하는 값으로 해석하지 않는다.

## 확인할 연결

NavMeshAgent 기반 적은 NavMesh 배치와 Agent·CharacterController의 역할 분리를 확인한다. 새 공통 기능은 실제로 여러 적이 사용하는지 확인한 뒤 추가한다.

관련 문서: [좀비](../Zombie/README.md), [NightShade](../NightShade/README.md), [공통 캐릭터](../../13.Characters/README.md).
