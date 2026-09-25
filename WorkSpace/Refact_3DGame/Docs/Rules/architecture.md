# 구조와 객체 소유 규칙

## 공통 기반과 구현 자율성

1. 각 기능과 엔티티는 프로젝트의 공통 구조·기술·기반 기능을 우선 활용한다. 공통 실행 흐름과 다른 기능에 연결되는 약속은 유지하되, 내부 클래스 구성과 처리 방식은 엔티티의 특성에 맞게 구현한다.
2. 비슷하다는 이유만으로 공통화를 강제하지 않는다. 같은 책임과 동작이 반복되고 함께 변경될 필요가 있을 때 공통 기반으로 옮긴다.
3. 변경의 영향은 가능한 한 해당 기능 안에 머물게 한다. 다른 기능이 내부 구현을 직접 알아야 하는 의존은 줄이고, 필요한 기능과 결과를 명확한 메서드·이벤트로 제공한다.
4. 플레이어와 적이 공유하는 Unit·체력·피해 계산·경직·타격 정지는 `04.Core/Character`에 모으고 `Core` 이름공간을 사용한다. 공통 코드는 개별 엔티티의 입력·AI·공격 선택을 소유하지 않으며 각 엔티티가 필요한 설정과 상태를 전달한다.

## 시작 데이터와 기능 경계

1. Boots는 게임 객체 생성 전에 ScriptableObject 설정을 PlayerDataContainer·EnemyDataContainer·ItemDataContainer·QuestDataContainer에 각각 전달한다. 각 컨테이너는 자기 종류의 전역 데이터 보관·조회만 맡으며 게임 기능을 실행하거나 맵별 배치를 결정하지 않는다.
2. 10번 이전의 게임 흐름 코드가 컨테이너에서 필요한 설정을 읽어 엔티티의 초기화·연결 지점에 전달한다. 10번 이상 기능은 다른 기능의 구현을 직접 참조하지 않고 Core의 데이터·인터페이스로 요청과 결과를 주고받는다. 기존 UI·Runtime·Editor 폴더는 현재 위치를 유지한다.
3. ScriptableObject는 설정 원본이다. 현재 체력·가방·퀘스트 진행은 실행 객체가 보관한다. 지금은 저장·네트워크 데이터 로딩을 추가하지 않는다.

## 전역 접근과 객체 소유

1. 사용자 결정에 따라 PlayerDataContainer·EnemyDataContainer·ItemDataContainer·QuestDataContainer·SceneLoader·MapManager·AddressableManager·EnemyContainer·ItemContainer·QuestContainer·HudContainer는 Singleton<T> 기반 싱글톤으로 공유한다. 생성자는 private이며 Unity 메인 스레드에서 사용한다. SceneLoader는 Boots의 Create(ConstantValid.StartSceneName)로 준비·등록하고 Instance는 조회만 한다. 전체 Release 완료·Quit에서 자신의 등록을 해제하며 개별 HUD·플레이어 반환은 등록을 유지한다. Map·Addressable은 Instance에서 준비하고, Enemy·Item은 Create(scene), Quest는 Create(player, questData), HUD는 Create(player) 후 Instance를 사용한다. 다른 클래스까지 자동으로 싱글톤으로 만들지 않는다.
2. 사용자 결정에 따라 `PlayerController`는 기존 초기화를 마친 플레이어 한 명을 Instance로 제공한다. MonoBehaviour이므로 일반 C#용 Singleton<T> 상속 대신 자체 등록·해제를 사용한다. Instance 접근은 로드·생성하지 않으며, Disable은 등록을 유지하고 Release는 자신의 등록만 지운다. 플레이어 상태·인벤토리·강화 기록은 해당 인스턴스가 소유한다. 퀘스트 진행은 `QuestContainer`, HUD 연결은 `HudContainer`가 맡고 연결받은 플레이어 참조로 구독·해제를 처리한다. PlayerSpawnManager·HudSpawnManager가 각각 Addressables 원본 요청·생성 객체를 소유하고 객체 파괴 완료 후 원본을 반환한다. 맵 객체 정리는 각 씬 코드가, 유지 객체 정리는 StartScene이 처리한다. 객체 수명 변경과 내부 게임 로직 변경은 범위를 구분한다. 재생성을 추가할 때는 유지할 기록과 새로 초기화할 상태를 구분한다.
3. `02.Manager/Resources/MapManager.Instance`는 맵 요청을 AddressableManager.Instance에 전달한다. 씬 핸들과 사용 횟수는 AddressableManager의 loadedScenes에만 보관한다. MapManager의 GetRefCount·IsBusy는 조회를 위임한다. 일반 에셋의 loadedAssets와 씬의 loadedScenes는 해제 API가 달라 구분하며, 공통 IsBusy로 로딩·정리 중 변경 요청을 거절한다. SceneLoader가 씬 전환을 순서대로 await하며 리소스 관리자에 맵 선택·조명 선택·엔티티 실행을 넣지 않는다.
4. 상수, 변경하지 않는 값과 객체 상태를 저장하지 않는 계산 함수는 `static`을 허용한다. 위 싱글톤 외에 변경 가능한 전역 저장소를 임의로 추가하지 않는다. `static readonly` 배열·컬렉션도 내용이 바뀔 수 있다.
5. 공유 설정·프리팹·아이템 정의는 에셋 참조로 공유하고, 현재 체력·획득 여부 같은 진행 상태는 인스턴스에 둔다. 일반 에셋과 씬의 Addressables 핸들은 AddressableManager만 보관한다. 각 호출자는 자신이 성공한 로드 횟수만큼만 Release하며 다른 호출자의 요청까지 반환하지 않는다.
6. `ZombieConfig.playSession`은 설정 캐시 갱신용으로만 유지한다. `PlayerStatUpgradeSession`은 `PlayerController`가 소유하는 인스턴스 기록이며 전역 static으로 되돌리지 않는다. 재생성 시 기록을 전달한다면 보관자와 전달 시점을 명시한다.
7. 소유 범위와 기존 코드의 이전 상태는 `Assets/_Project/01.Boot/README.md`를 따른다. 게임 시작·지역 이동·새 게임·종료에서 누가 생성하고 해제하는지 명시한다.
8. Enemy·Item은 각각 하나의 소속 씬에 묶인다. 씬을 바꾸면 Enemy·Item을 정리한다. Quest·HUD의 플레이어를 바꾸려면 기존 컨테이너를 Dispose한 뒤 Create한다. Quest를 다시 Create하면 진행 기록도 새로 시작한다. Play 시작 시 싱글톤 참조를 초기화하되, 이것을 게임 중 자원 반환이나 Dispose의 대체로 사용하지 않는다.

## 책임 배치와 확장 기준

`ObjectLifecycle`은 엔티티 객체 자체의 생명주기 상태와 동작을 표현한다. 몬스터 컨테이너는 맵에서 소환되는 몬스터의 목록·풀·등록·반환을 관리하고 각 엔티티의 생명주기 메서드를 호출한다. 컨테이너나 컨테이너 관리 클래스에 `ObjectLifecycle`을 상속하거나 별도 생명주기 객체를 붙이지 않는다.

싱글톤의 보관·조회·해제는 `04.Core/Singleton<T>`에 모은다. 구체 클래스는 private 생성자와 필요한 Create 입력 검사를 담당한다. Play 초기화 콜백은 각 구체 타입에 둔다.

Boots는 GameData 입력 목록에서 종류별 데이터 컨테이너를 준비하고 StartScene에 카메라·시작 입력을 전달한다. 게임 객체 반환 뒤 데이터 컨테이너를 정리한다. SceneLoader는 현재 씬 정리 → 씬 요청·사용하지 않는 리소스 반환 → 다음 씬 로드 → 씬 실행 순서를 맡는다. GameScene의 오버라이드로 씬별 생성·활성화·정리를 호출하며 몬스터 종류·위치·아이템 설정은 조회하지 않는다.

StartScene은 상주 씬에 배치하며 PlayerSpawnManager·HudSpawnManager·AudioListenerManager와 퀘스트 기록을 소유한다. 현재 맵 이동에서는 같은 플레이어·HUD·진행 기록을 유지한다. 다른 씬의 유지 정책이 필요하면 그 요청 범위에서 변경한다. MapPlayScene은 각 맵에 배치하고 전역 데이터와 기존 배치를 읽어 객체·기능을 연결하며 이탈 전에 정리한다. SceneLoader는 상주 씬 이름을 매개변수로 받고 씬 요청·로드·활성 씬 지정·언로드를 맡는다. 매 프레임 갱신은 각 맵의 Update에서 처리한다. 입력·전투·표시 상세 규칙은 각 기능에 둔다.

생성한 객체는 필수 참조 연결과 초기화를 마친 뒤 활성화한다. 반환은 갱신·입력·구독을 먼저 중지하고 객체 파괴가 완료된 뒤 원본 에셋 요청을 반환한다. 로드 성공 여부는 호출자가 기록하며 부분 실패에서도 자신이 확보한 요청만 정리한다. Play 종료와 게임 중 명시적 반환은 구분한다.

| 대상 | 맡길 책임 |
| --- | --- |
| Player·Enemy·Item·QuestDataContainer | Boots가 받은 자기 종류의 ScriptableObject 설정만 보관·조회 |
| SceneLoader | 현재 씬 정리·리소스 반환·다음 씬 로드·실행 |
| StartScene | 상주 플레이어·HUD 생성 담당과 리스너·퀘스트 기록 소유 |
| GameScene·MapPlayScene | 씬별 실행 오버라이드, 전역 데이터 조회와 현재 맵의 객체·연결 정리 |
| PlayerSpawnManager | 플레이어 원본 요청·생성 객체 소유, 카메라 구성 확인·초기화·반환 |
| HudSpawnManager | HUD 원본 요청·생성 객체 소유, 표시 구성 확인·연결·반환 |
| MapEnemySpawner | 한 맵의 소환 구역 연결과 EnemyContainer 생성·갱신·반환, 종류·위치·재진입 조건은 Zone 설정에 유지 |
| AudioListenerManager | 전달받은 카메라의 리스너 참조·활성 상태 관리, 종료 시 비활성화·참조 정리; 컴포넌트는 씬 소유 |
| MapScene | 시작 위치·환경음 참조 제공, 환경음 재생·정지 |
| PlayerController | 플레이어 상태·인벤토리·강화 기록 소유, Unity 활성·비활성 전달과 공통 관리자 등록·반환 |
| QuestContainer | Scene의 게임 흐름에서 퀘스트 진행 기록과 판정 컴포넌트 연결 |
| HudContainer | 플레이어 표시 계약 IPlayerHudSource와 적 목록 계약 IEnemyHudSource를 HUD에 전달 |
| MapManager | 맵 로딩·반환·정리와 상태 조회를 AddressableManager에 위임 |

게임별 구현 설명은 [진입점](../../Assets/_Project/01.Boot/README.md), [플레이어](../../Assets/_Project/10.Player/README.md), [맵 컨테이너](../../Assets/_Project/03.Loading/README.md)를 참고한다.

실제 씬 배치·호출 순서·연결 완료 여부·검증·남은 작업은 각 기능 README에서 관리하고 변경 시 즉시 갱신한다. 이 문서에는 기능별 진행 상태를 중복 기록하지 않는다.

기능을 추가하기 전에 호출자, 상태 소유자, 갱신자, 생성·반환 책임을 먼저 정한다. 기존 클래스 안에서 책임을 명확히 표현할 수 있으면 함께 둔다. 독립적으로 바뀌는 책임이나 반복 중복이 확인될 때만 분리한다. 초기화 때 전달받을 수 있는 참조를 전역 검색이나 새로운 싱글톤으로 대체하지 않는다.

## 맵 씬의 사용 횟수

MapManager.LoadAsync → AddressableManager.LoadSceneAsync → 최초 요청만 Additive 씬 로딩 → loadedScenes 보관 또는 재사용 → refCount 증가.
MapManager.Release → AddressableManager.ReleaseScene → refCount 감소 → CleanUpAsync가 CleanUpUnusedScenesAsync에 위임 → 0인 씬 해제와 loadedScenes 제거.

refCount는 이 컨테이너의 사용 요청 수이며 Addressables 내부 참조 횟수와 구분한다.

현재 구현은 비동기 작업 중 다음 변경 요청을 거절한다. 완료를 기다린 뒤 요청하며, 외부 SceneManager로 임의 해제하지 않는다. 마지막 사용자가 반환한 뒤 정리 완료를 기다린다. 싱글톤 접근 자체는 로드 요청이나 사용 횟수를 늘리지 않는다.

씬 이름은 ConstantValid의 상수로 전달하며 필요 없는 Inspector 속성과 상태 복사본을 추가하지 않는다. Task는 실제 로드·언로드·파괴 완료 대기에 사용하고 동기식 정리는 void로 둔다. 중복 반환 방지에 필요한 진행 작업만 보관하며 시작·대상별 Task 필드를 반복 추가하지 않는다.
