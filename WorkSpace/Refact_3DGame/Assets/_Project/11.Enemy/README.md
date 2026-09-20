# 적 엔티티

몬스터 종류별 코드·설정·모델을 모은 폴더다. 각 몬스터의 현재 구현 범위는 아래 문서에서 확인한다.

| 폴더 | 현재 폴더 내용 |
| --- | --- |
| [Zombie](Zombie/README.md) | 탐지·추격·공격·귀환·피격·사망 코드, 설정과 배치 프리팹 |
| [NightShade](NightShade/README.md) | 보스 행동 선택, 공격·회복 행동, 전투 범위와 초기화 코드 |
| [DemonSwordsman](DemonSwordsman/README.md) | 검·야수 동작 리소스와 모델 프리팹 |
| [Fighter](Fighter/README.md) | 모델·애니메이션·프리팹, 전용 C# 코드 없음 |
| [Mummy Warrior](Mummy%20Warrior/README.md) | 모델·애니메이션·프리팹과 보관 애니메이션 |
| [Mutant](Mutant/README.md) | 모델·애니메이션·프리팹, 전용 C# 코드 없음 |
| [Undead](Undead/README.md) | 모델·프리팹, 전용 C# 코드 없음 |
| [Shared](Shared/README.md) | 적 공통 전투 정보, 공격 데이터와 경로 안내 |

## 구현된 적의 동작 흐름

소환 설정 → `WorldObjectManager.TrySpawn` → Controller가 연결한 런타임 유닛 활성화 → 대상·피해·애니메이션 정보를 읽어 상태 결정 → 이동·공격·체력 변경 → 사망 처리 후 회수 요청 순서로 이어진다.

좀비와 NightShade는 같은 월드 객체 생명주기를 사용하지만 행동 선택과 이동 경로 처리는 각각 구현한다. 모든 몬스터가 같은 AI를 사용한다고 가정하지 않는다.

## 새 적을 연결할 때

1. 전용 Controller에서 Unity 컴포넌트와 설정을 연결한다.
2. 전용 런타임 유닛과 상태·행동 처리를 구성한다.
3. 풀 생성용 `SpawnSettings`를 WorldObjectManager에 등록한다.
4. 피해 수신과 애니메이션 이벤트, 체력 표시 연결을 확인한다.
5. Zone 자동 소환을 사용할 때는 Controller의 `IZoneEnemy` 구현도 확인한다.

리소스 전용 폴더의 프리팹을 배치하는 것만으로 전투 AI가 연결되지는 않는다. 모델 파일과 실제 배치용 프리팹의 위치가 다를 수 있으므로 각 README의 경로를 먼저 확인한다.
