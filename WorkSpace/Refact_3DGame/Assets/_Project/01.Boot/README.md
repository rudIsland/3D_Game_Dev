# 게임 진입점과 기능별 컨테이너

## 현재 상태

Start 씬에는 Boot와 MainCamera를 둔다. Boots가 일반 C# GameManager 하나를 만들고 Inspector의 MainCamera와 Start 씬을 Init에 전달한다. Start → GameManager.Start → Ground 로딩 → 플레이어 생성·초기화 → HUD 생성·연결 → 활성화 순서로 실행한다. Ground는 활성 씬이 되고, 런타임 GameObjects·PlayerRoot·CombatHud는 Start에 소속된다. 미니맵·퀘스트·적 실행·맵 교체·BGM은 다음 단계다.

참고한 로컬 리포는 D:/GitRepo/Project_P, HEAD b37aee0이다. 해당 리포의 Assets/01.Script/01.Manager/InitManager.cs, 02.Scene/SceneLoader.cs, Docs/overview.md와 Docs/architecture.md에서 진입점·씬 로딩·기능별 폴더·README 구분을 참고했다. Project_P가 엔티티별 컨테이너 구조라는 뜻은 아니다.

폴더는 00.Scene부터 시작하고, 엔티티·게임 기능은 10.Player부터 번호를 붙인다. Code 폴더 없이 클래스와 역할별 하위 폴더를 각 기능 폴더 바로 아래에 둔다. 기존 이름공간과 어셈블리 이름은 유지한다. 폴더 번호가 Unity 실행 순서를 강제하지는 않는다. Project_P의 정적 전역 접근, UniTask, UI 베이스와 외부 패키지는 가져오지 않았다.

## 파일별 책임과 소유자

| 파일 | 맡을 역할 | 소유자 |
| --- | --- | --- |
| [Boots.cs](Boots.cs) | 카메라 전달, 시작·갱신·종료 요청 | Start 씬의 Boot |
| [GameManager.cs](../05.Manager/GameManager.cs) | 맵·플레이어·HUD 준비, 생성 객체와 로드 요청 반환 | Boots가 만든 일반 C# 인스턴스 |
| [MapScene.cs](../00.Scene/MapScene.cs) | 시작 위치·환경음 Inspector 참조와 재생·정지 | Ground 씬 |
| [PlayerController.cs](../10.Player/Lifecycle/PlayerController.cs) | Init 성공 후 Instance 등록, 입력·상태·인벤토리·강화 기록 소유 | 생성 객체·원본 요청은 GameManager |
| [QuestContainer.cs](../16.Quest/QuestContainer.cs) | 퀘스트 진행 상태와 이벤트 연결 | Create(player) 후 Instance |
| [HudContainer.cs](../UI/HudContainer.cs) | 기존 HUD와 게임 상태 연결 | Create(player) 후 Instance |
| [MapManager.cs](../04.Loading/MapManager.cs) | 맵 로딩·반환 요청을 AddressableManager로 전달 | Instance |

Boots는 GameManager 참조 하나와 Inspector의 MainCamera만 보관한다. GameManager가 컨테이너·로딩 작업·생성 객체를 보관하고, Boots는 시작·갱신·종료 요청과 예외 기록만 수행한다. 게임 중 반환은 Release, Play 종료는 Quit으로 구분한다.

## 전역으로 둘 것과 인스턴스로 둘 것

MapManager·AddressableManager·EnemyContainer·ItemContainer·QuestContainer·HudContainer는 [Singleton<T>](../02.Core/Singleton.cs)를 상속한다. Map·Addressable은 Instance로 바로 접근한다. Enemy·Item은 Create(scene), Quest·HUD는 Create(player)로 준비한 뒤 Instance에 접근한다. 같은 입력의 Create는 기존 객체를 반환하고, 다른 입력은 기존 객체를 Dispose하기 전까지 거절한다. Create가 필요한 타입의 Instance를 준비 전에 읽으면 예외가 발생한다. Enemy·Item은 각각 하나의 소속 씬에 묶인다. 다른 씬으로 옮기려면 먼저 Dispose한다. Quest를 Dispose하고 다시 Create하면 새 진행 기록으로 시작한다. 현재 GameManager는 맵과 HUD만 연결하며, Enemy·Item·Quest 자동 연결은 없다.

| 대상 | 접근·소유 방식 | 유지·해제 시점 |
| --- | --- | --- |
| 고정 배율, Animator 해시, 고정 색상 | 각 기능 안의 const 또는 static readonly 값 | 타입 수명 동안 사용 |
| 거리·피격 판정 등 상태를 저장하지 않는 함수 | 각 기능 안의 static 함수 허용 | 호출한 입력으로 계산하고 결과 반환 |
| ZombieConfig.playSession | private static 유지. 설정 캐시 갱신 번호로만 사용 | Play 시작마다 갱신 |
| 플레이어 객체·입력·체력·인벤토리·강화 기록 | PlayerController.Instance로 준비된 한 명에 접근 | Disable은 등록·기록 유지, Release는 자신의 등록 해제. 재생성 시 기록 전달은 추후 연결 |
| 퀘스트 진행 기록 | QuestContainer.Instance | 같은 인스턴스 유지 중 보관, Dispose 후 Create하면 새 기록 |
| HUD 연결 | HudContainer.Instance | 게임 종료 시 Dispose로 구독 해제 |
| 적·적 풀·바닥 아이템 | 각 컨테이너가 소유. 씬 핸들은 AddressableManager가 관리 | 갱신·해제 호출은 진입점 연결 단계에서 정함 |
| 지역 씬 로딩 핸들 | AddressableManager.Instance의 loadedScenes | 해당 씬 해제 완료까지 보관 |
| 설정·프리팹·아이템 정의 | 에셋 참조 공유. 진행 상태를 에셋에 저장하지 않음 | Inspector 직접 참조인지 Addressables 로딩인지에 따라 수명 관리 |
| 공용 에셋의 Addressables 핸들 | AddressableManager.Instance가 보관 | 각 호출자가 자신의 로딩 요청만 반환하고 사용 횟수 0일 때 정리 |

Common은 공유 리소스 분류이고, 싱글톤 접근 여부와 별개다. 객체와 핸들의 수명은 사용처의 반환·정리 호출로 관리한다. Enemy·Item 싱글톤은 현재 지정한 씬의 풀을 소유한다. 설정 참조 공유와 변경 불가능함도 같지 않다. 배열·곡선·ScriptableObject는 공유 후 임의로 변경하지 않는다.

### 기존 코드에서 아직 이전하지 않은 부분

- PlayerStatUpgradeSession은 PlayerController가 소유하는 인스턴스 강화 기록이다. Create가 배율을 읽고 PlayerStatUpgrade.TryApply가 전달받은 기록을 바꾼다.
- 현재 인벤토리와 강화 기록은 플레이어 인스턴스 수명에 따른다. 플레이어를 재생성해도 기록을 이어 주는 기능과 새 게임 초기화는 이후 실행 연결 단계에서 정한다.
- ZombieConfig.runtimeSettings는 설정 에셋별 캐시다. static playSession은 Play 재시작 시 캐시가 이전 실행에서 재사용되지 않도록 하는 번호이므로 유지한다. 적 체력이나 공격 대상은 여기에 넣지 않는다.
- HUD는 GameManager가 전달한 PlayerController의 활성화·비활성화 알림을 구독한다. GroundQuestController 연결은 다음 단계다.

### 현재 실행 흐름

1. Start 실행 → Boots가 GameManager.Init(MainCamera, Start)를 호출 → GameManager.Start가 Ground를 순서대로 로딩한다.
2. Ground 내부에서 MapScene을 한 번 확인하고 Ground를 활성 씬으로 지정한다. 기존 조명·URP DAY Volume을 사용한다.
3. Start 소속 비활성 GameObjects 아래에 PlayerRoot 원본을 로드·생성한다. MapScene.PlayerSpawnPoint의 위치·수평 방향을 적용하고 PlayerController.Init(MainCamera.transform)을 호출한다. 내부 Unit.Create가 성공하면 Instance를 등록한다. 비활성 부모가 조기 입력을 막으며 상호작용은 기존 Unity Start에서 준비한다. 기존 플레이어가 준비되어 있으면 GameManager.Start가 추가 로딩 전에 거절한다.
4. CombatHud 원본을 로드·생성하고 HudContainer.Create(player) → Add(view)로 연결한다. 모든 연결 후 부모를 활성화하고 환경음을 시작한다. Update → GameManager.Tick → PlayerController.Tick → PlayerWorldUnit.Tick이 입력·이동·스태미나를 갱신한다.

현재 Ground를 additive로 로딩하므로 Start 씬과 Boot가 유지된다. 새 게임 재시작과 Start 씬 교체는 미구현이다. DontDestroyOnLoad는 사용하지 않는다.

## 반환과 실패 처리

입력: GameManager.Release 또는 실행 중 Boot 파괴 → 처리: 준비·갱신 중지, 진행 중 로드 완료 대기 → 출력: 이번 실행의 생성 객체와 성공한 로드 요청 반환.

- 플레이어 비활성화 → HUD Dispose → 플레이어 Release → GameObjects 파괴 완료 대기 → HUD·플레이어 원본 요청 반환 → 환경음 정지 → Ground 요청 반환·CleanUpAsync 순서다.
- ItemContainer와 HudContainer는 매 프레임 갱신 메서드를 만들지 않았다.
- 시작·반환 중복 호출은 같은 작업을 반환한다. 준비 실패는 실패 단계와 대상을 기록하고 성공한 요청만 정리한다. 초기화에 실패한 플레이어 입력은 활성화하지 않는다.
- Play 종료는 Quit으로 입력·HUD·환경음을 정지하며 새로운 비동기 씬 해제를 시작하지 않는다. Domain Reload를 꺼도 새 GameManager와 초기화된 기존 싱글톤을 사용한다.

## 어셈블리

Boots와 05.Manager의 GameManager는 Game.Boot에 속하며 Game.Runtime·Game.UI를 참조한다. 카메라 필수 컴포넌트 검사를 위해 Cinemachine·URP Runtime도 참조한다. Game.UI → Game.Runtime 방향은 유지하며 순환 참조는 없다. MapScene과 엔티티는 Game.Runtime, HudContainer는 Game.UI에서 컴파일한다.

## 다음 실습과 확인

1. Start의 Boots.mainCamera와 MainCamera의 Camera·CinemachineBrain·AudioListener·URP 설정을 확인한다. 카메라는 이전 배치를 복원하고 기존 DAY Volume을 적용하도록 Post Processing을 켰다.
2. Ground의 MapScene.playerSpawnPoint는 기존 StartArrival, ambientSource는 같은 오브젝트의 AudioSource다. SwampForestDayLoop, loop=true, playOnAwake=false, 2D, volume=0.2이며 볼륨은 Inspector에서 조절한다. 데모 카메라는 비활성 상태를 유지한다.
3. Start 씬만 열고 Play한다. Ground·PlayerRoot·CombatHud 각각 하나와 활성 Camera·AudioListener 각각 하나를 확인한다. W/A/S/D 이동, Shift 달리기, Space 구르기, 마우스 시점과 체력·스태미나 HUD를 확인한다. exploration-info는 기본 숨김이다.
4. 명시적 Release 완료 뒤 PlayerRoot·CombatHud·Ground 요청 수가 0인지 확인한다. Play 종료·재시작 시 중복 생성이나 Console 오류가 없어야 한다. Ground를 편집기에서 미리 열어 둔 상태는 시작 경로 검증 조건이 아니다.
5. 맵 로딩·해제와 검증 방법은 [04.Loading 안내](../04.Loading/README.md)를 따른다. 다른 컨테이너 이전과 묶어서 진행하지 않는다.

Boots는 시작·갱신·종료 요청만 연결하고, 생성 상태·비동기 작업·반환할 요청은 GameManager가 소유한다. 이동·전투·보상 같은 상세 규칙은 각 기존 기능 클래스에 유지한다.

## 확인과 남은 작업

- Unity 6000.3.9f1 컴파일과 Start 단독 Play 확인. Ground·플레이어·HUD 각각 1개, 활성 Camera·AudioListener 각각 1개, Missing Script·Console 오류·경고 0이다.
- 이동·카메라 추적, 피해 후 체력 100 → 90 표시, 달리기·구르기 스태미나 소비와 회복을 확인했다. 분리한 Space 입력은 설정 비용 25를 소비했고 HUD 비율도 일치했다. 낮 환경 화면과 환경음 재생 상태, exploration-info 숨김을 확인했다.
- 중복 시작·반환, 부분 초기화 실패, 명시적 반환, 로딩 직후 반환, 실행 중 Boot 파괴 후 생성 객체·세 주소 요청·캐시 0을 확인했다. Domain Reload 비활성 재시작에서도 각 객체·요청 1과 초기 체력 100을 확인했으며 검증 후 설정을 원래대로 복원했다.
- 실제 Addressables 번들·Player 빌드, 모든 로딩 실패 조합, 실제 스피커 청음은 미검증이다. 맵 전환·적 실행·퀘스트·미니맵·BGM은 연결하지 않았다. URP 그림자 아틀라스 축소 정보 로그는 기존 렌더링 설정에 따라 남는다.
