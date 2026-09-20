# 캐릭터 공통 규칙

플레이어와 적이 함께 사용하는 체력, 생명주기, 피해 계산과 피격 규칙을 둔다. 특정 캐릭터의 입력이나 AI는 해당 엔티티 폴더에서 처리한다.

## 폴더 구성

| 폴더 | 설명 |
| --- | --- |
| `Code/Core` | `Unit`, `PlayerUnit`, `EnemyUnit`, `UnitHealth`와 생명 설정 |
| `Code/Core/Enemy` | 적에게 피해를 전달하는 요청·결과와 수신 인터페이스 |
| `Code/Combat/AttackData` | 공격 피해 데이터 |
| `Code/Combat/Systems` | 피해 계산, 경직 누적, 피격 반응, 타격 정지와 공격 대상 보정 |

## 동작 흐름

`WorldObjectManager` 호출 → `WorldObject` 생명주기 → `Unit` 공통 처리 → 각 캐릭터의 전용 처리 순서로 실행한다.

- `Create`: 전용 생성 작업을 호출한다.
- `Enable`: 활성화 번호를 올리고 자원 준비 후 전용 활성화 작업을 호출한다.
- `Tick`: 해당 유닛의 갱신 작업을 호출한다.
- `Disable`: 전용 비활성화 작업을 호출한다.
- `Dispose`: 전용 정리 작업과 체력 이벤트 해제를 처리한다.

`EnemyUnit`은 활성화할 때 체력을 초기화한다. 기본 `Unit` 자체가 모든 캐릭터의 체력을 매번 초기화하는 것은 아니다. `ActivationSequence`는 같은 풀 객체가 다시 활성화된 경우를 구분하는 번호다.

## 먼저 읽을 코드

- [Unit.cs](Code/Core/Unit.cs): 공통 호출 순서와 확장 지점.
- [EnemyUnit.cs](Code/Core/EnemyUnit.cs): 적 재활성화 시 체력 초기화.
- [UnitHealth.cs](Code/Core/UnitHealth.cs): 현재 체력과 변경·사망 알림.
- [HitDamageCalculator.cs](Code/Combat/Systems/HitDamageCalculator.cs): 피해 적용 결과.
- [StopPoint.cs](Code/Combat/Systems/StopPoint.cs): 경직 수치 누적과 회복.

## 변경 후 확인

공통 피해·생명주기를 바꾸면 플레이어, 좀비, NightShade에 모두 영향이 갈 수 있다. Unity에서 피해, 사망, 비활성화와 풀 재사용을 구분해서 확인한다. 특정 적의 공격 선택 규칙은 이 폴더에 넣지 않는다.

관련 문서: [월드 객체](../../00.Core/WorldObjects/README.md), [플레이어](../../01.Player/README.md), [적](../../02.Enemy/README.md).
