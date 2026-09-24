# 구조와 객체 소유 규칙

## 공통 기반과 구현 자율성

1. 각 기능과 엔티티는 프로젝트의 공통 구조·기술·기반 기능을 우선 활용한다. 공통 실행 흐름과 다른 기능에 연결되는 약속은 유지하되, 내부 클래스 구성과 처리 방식은 엔티티의 특성에 맞게 구현한다.
2. 비슷하다는 이유만으로 공통화를 강제하지 않는다. 같은 책임과 동작이 반복되고 함께 변경될 필요가 있을 때 공통 기반으로 옮긴다.
3. 변경의 영향은 가능한 한 해당 기능 안에 머물게 한다. 다른 기능이 내부 구현을 직접 알아야 하는 의존은 줄이고, 필요한 기능과 결과를 명확한 메서드·이벤트로 제공한다.

## 전역 접근과 객체 소유

1. 사용자 결정에 따라 MapManager·AddressableManager·EnemyContainer·ItemContainer·QuestContainer·HudContainer는 싱글톤으로 공유한다. 생성자는 private이며 Unity 메인 스레드에서 사용한다. Map·Addressable은 Instance에서 준비하고, Enemy·Item은 Create(scene), Quest·HUD는 Create(player) 후 Instance를 사용한다. 다른 클래스까지 자동으로 싱글톤으로 만들지 않는다. GameManager는 Boots가 소유하는 일반 C# 인스턴스로 두고 게임 수명의 준비·반환 순서를 맡긴다.
2. 사용자 결정에 따라 `PlayerController`는 기존 초기화를 마친 플레이어 한 명을 Instance로 제공한다. MonoBehaviour이므로 일반 C#용 Singleton<T> 상속 대신 자체 등록·해제를 사용한다. Instance 접근은 로드·생성하지 않으며, Disable은 등록을 유지하고 Release는 자신의 등록만 지운다. 플레이어 상태·인벤토리·강화 기록은 해당 인스턴스가 소유한다. 퀘스트 진행은 `QuestContainer`, HUD 연결은 `HudContainer`가 맡고 연결받은 플레이어 참조로 구독·해제를 처리한다. GameManager가 Addressables 원본 요청·생성 객체를 소유하고 객체 파괴 완료 후 원본을 반환한다. 객체 수명 변경과 내부 게임 로직 변경은 범위를 구분한다. 재생성을 추가할 때는 유지할 기록과 새로 초기화할 상태를 구분한다.
3. `04.Loading/MapManager.Instance`는 맵 요청을 AddressableManager.Instance에 전달한다. 씬 핸들과 사용 횟수는 AddressableManager의 loadedScenes에만 보관한다. MapManager의 ResourceCount·GetRefCount·IsBusy는 조회를 위임한다. 일반 에셋의 loadedAssets와 씬의 loadedScenes는 해제 API가 달라 구분하며, 공통 IsBusy로 로딩·정리 중 변경 요청을 거절한다. GameManager가 순서대로 await하며 로더에 자동 맵 교체·조명 선택·시간 정지·엔티티 실행을 넣지 않는다.
4. 상수, 변경하지 않는 값과 객체 상태를 저장하지 않는 계산 함수는 `static`을 허용한다. 위 싱글톤 외에 변경 가능한 전역 저장소를 임의로 추가하지 않는다. `static readonly` 배열·컬렉션도 내용이 바뀔 수 있다.
5. 공유 설정·프리팹·아이템 정의는 에셋 참조로 공유하고, 현재 체력·획득 여부 같은 진행 상태는 인스턴스에 둔다. 일반 에셋과 씬의 Addressables 핸들은 AddressableManager만 보관한다. 각 호출자는 자신이 성공한 로드 횟수만큼만 Release하며 다른 호출자의 요청까지 반환하지 않는다.
6. `ZombieConfig.playSession`은 설정 캐시 갱신용으로만 유지한다. `PlayerStatUpgradeSession`은 `PlayerController`가 소유하는 인스턴스 기록이며 전역 static으로 되돌리지 않는다. 재생성 시 기록을 전달한다면 보관자와 전달 시점을 명시한다.
7. 소유 범위와 기존 코드의 이전 상태는 `Assets/_Project/01.Boot/README.md`를 따른다. 게임 시작·지역 이동·새 게임·종료에서 누가 생성하고 해제하는지 명시한다.
8. Enemy·Item은 각각 하나의 소속 씬에 묶인다. 씬 또는 Quest·HUD의 플레이어를 바꾸려면 기존 컨테이너를 Dispose한 뒤 Create한다. Quest를 다시 Create하면 진행 기록도 새로 시작한다. Play 시작 시 싱글톤 참조를 초기화하되, 이것을 게임 중 자원 반환이나 Dispose의 대체로 사용하지 않는다.

## 책임 배치와 확장 기준

싱글톤의 보관·조회·해제는 `02.Core/Singleton<T>`에 모은다. 구체 클래스는 private 생성자와 필요한 Create 입력 검사를 담당한다. Play 초기화 콜백은 각 구체 타입에 둔다.

Boots는 Unity 이벤트와 Inspector 참조를 GameManager에 전달한다. 생성 객체·초기화 상태·로딩 작업·반환할 요청은 GameManager가 소유한다. 입력·전투·표시의 상세 규칙은 각 기능에 둔다. 새로운 기능도 기존 호출 흐름에서 연결할 위치를 정하고, Boots나 로더에 기능별 처리 전체를 넣지 않는다.

생성한 객체는 필수 참조 연결과 초기화를 마친 뒤 활성화한다. 반환은 갱신·입력·구독을 먼저 중지하고 객체 파괴가 완료된 뒤 원본 에셋 요청을 반환한다. 로드 성공 여부는 호출자가 기록하며 부분 실패에서도 자신이 확보한 요청만 정리한다. Play 종료와 게임 중 명시적 반환은 구분한다.

| 대상 | 맡길 책임 |
| --- | --- |
| GameManager | 게임 시작에 필요한 객체의 준비 순서, 생성 객체·완료된 요청 소유와 반환 |
| MapScene | 시작 위치·환경음 참조 제공, 환경음 재생·정지 |
| PlayerController | 플레이어 상태·인벤토리·강화 기록 소유, Unity 활성·비활성 전달과 공통 관리자 등록·반환 |
| QuestContainer | 퀘스트 진행 기록과 판정 컴포넌트 연결 |
| HudContainer | 플레이어 컨트롤러와 적 컨테이너를 HUD에 전달 |
| MapManager | 맵 로딩·반환·정리와 상태 조회를 AddressableManager에 위임 |

게임별 구현 설명은 [진입점](../../Assets/_Project/01.Boot/README.md), [플레이어](../../Assets/_Project/10.Player/README.md), [맵 컨테이너](../../Assets/_Project/04.Loading/README.md)를 참고한다.

실제 씬 배치·호출 순서·연결 완료 여부·검증·남은 작업은 각 기능 README에서 관리하고 변경 시 즉시 갱신한다. 이 문서에는 기능별 진행 상태를 중복 기록하지 않는다.

기능을 추가하기 전에 호출자, 상태 소유자, 갱신자, 생성·반환 책임을 먼저 정한다. 기존 클래스 안에서 책임을 명확히 표현할 수 있으면 함께 둔다. 독립적으로 바뀌는 책임이나 반복 중복이 확인될 때만 분리한다. 초기화 때 전달받을 수 있는 참조를 전역 검색이나 새로운 싱글톤으로 대체하지 않는다.

## 맵 씬의 사용 횟수

MapManager.LoadAsync → AddressableManager.LoadSceneAsync → 최초 요청만 Additive 씬 로딩 → loadedScenes 보관 또는 재사용 → refCount 증가.
MapManager.Release → AddressableManager.ReleaseScene → refCount 감소 → CleanUpAsync가 CleanUpUnusedScenesAsync에 위임 → 0인 씬 해제와 loadedScenes 제거.

ResourceCount는 보관 중인 씬 수이며 내부 오브젝트·텍스처 수나 메모리 사용량이 아니다. refCount는 이 컨테이너의 사용 요청 수이며 Addressables 내부 참조 횟수와 구분한다.

현재 구현은 비동기 작업 중 다음 변경 요청을 거절한다. 완료를 기다린 뒤 요청하며, 외부 SceneManager로 임의 해제하지 않는다. 마지막 사용자가 반환한 뒤 정리 완료를 기다린다. 싱글톤 접근 자체는 로드 요청이나 사용 횟수를 늘리지 않는다.
