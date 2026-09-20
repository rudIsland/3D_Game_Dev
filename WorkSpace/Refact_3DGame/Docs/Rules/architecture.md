# 구조와 객체 소유 규칙

## 전역 접근과 객체 소유

1. 게임 전체에서 오래 사용한다는 이유만으로 `static` 또는 `Instance`로 공개하지 않는다. 현재 `Boots`는 실행 로직 없는 틀이다. 각 컨테이너의 책임을 정리한 뒤 게임 시작·갱신·종료를 별도 단계에서 연결한다.
2. 플레이어·인벤토리·강화 기록은 `PlayerController` 인스턴스가 소유한다. 퀘스트 진행은 `QuestContainer`, HUD 연결은 `HudContainer`가 맡고 필요한 `PlayerController`를 전달받는다. 플레이어 생성·갱신의 자동 실행과 재생성 시 기록 유지는 추후 연결한다.
3. `04.Loading/MapContainer`는 일반 C# 인스턴스로 맵 씬의 Addressables 핸들을 `cache`에 보관하고 `refCount`로 사용 횟수를 센다. `ResourceCount`는 캐시된 씬 수이며 내부 에셋 수가 아니다. `LoadAsync`로 사용 횟수를 늘리고 `Release`로 줄이며 `CleanUpAsync`로 0인 씬을 해제한다. 자동 맵 교체·조명 선택·시간 정지·엔티티 실행과 Boots 연결은 넣지 않는다. MapManager와 MapLoader는 제거했다.
4. 상수, 변경하지 않는 값과 객체 상태를 저장하지 않는 계산 함수는 `static`을 허용한다. `static readonly` 배열·컬렉션도 내용이 바뀔 수 있으므로 변경 가능한 전역 저장소로 사용하지 않는다.
5. 공유 설정·프리팹·아이템 정의는 에셋 참조로 공유하고, 현재 체력·획득 여부 같은 진행 상태는 인스턴스에 둔다. Addressables 핸들은 로딩을 요청한 소유자가 보관하고 해제한다.
6. `ZombieConfig.playSession`은 설정 캐시 갱신용으로만 유지한다. `PlayerStatUpgradeSession`은 `PlayerController`가 소유하는 인스턴스 기록이며 전역 static으로 되돌리지 않는다. 플레이어 재생성 시 기록을 전달하는 방식은 진입점 연결 단계에서 정한다.
7. 소유 범위와 기존 코드의 이전 상태는 `Assets/_Project/01.Boot/README.md`를 따른다. 게임 시작·지역 이동·새 게임·종료에서 누가 생성하고 해제하는지 명시한다.

## 현재 단계

Boots는 빈 진입점이다. 컨테이너 책임을 먼저 정리하고 실행은 이후 연결한다. 이 문서의 추후 연결 항목을 이미 작동하는 기능으로 설명하지 않는다.

| 대상 | 현재 책임 |
| --- | --- |
| PlayerController | 플레이어 상태·인벤토리·강화 기록 소유, 명시적 Create·Tick 제공 |
| QuestContainer | 퀘스트 진행 기록과 판정 컴포넌트 연결 |
| HudContainer | 플레이어 컨트롤러와 적 컨테이너를 HUD에 전달 |
| MapContainer | Address별 씬 핸들·cache·refCount, 명시적 로딩과 정리 |

게임별 구현 설명은 [진입점](../../Assets/_Project/01.Boot/README.md), [플레이어](../../Assets/_Project/10.Player/README.md), [맵 컨테이너](../../Assets/_Project/04.Loading/README.md)를 참고한다.

## 맵 씬의 사용 횟수

LoadAsync → 최초 요청만 Additive 씬 로딩 → cache 보관 또는 재사용 → refCount 증가.
Release → refCount 감소 → CleanUpAsync → 0인 씬 해제와 cache 제거.

ResourceCount는 보관 중인 씬 수이며 내부 오브젝트·텍스처 수나 메모리 사용량이 아니다. refCount는 이 컨테이너의 사용 요청 수이며 Addressables 내부 참조 횟수와 구분한다.

현재 구현은 비동기 작업 중 다음 변경 요청을 거절한다. 완료를 기다린 뒤 요청하며, 같은 씬을 여러 MapContainer에서 중복 소유하거나 외부 SceneManager로 임의 해제하지 않는다. 마지막 사용자가 반환한 뒤 정리 완료를 기다리고 컨테이너를 폐기한다.
