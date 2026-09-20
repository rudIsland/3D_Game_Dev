# 게임 진입점과 기능별 컨테이너

## 현재 상태

Boots는 실행 로직 없는 빈 MonoBehaviour다. 기존 컨테이너 생성·연결·갱신·해제와 씬 이벤트 구독은 제거했다. 다른 엔티티 코드의 기능은 유지하지만 Boots에서는 호출하지 않는다. 아래 소유·생명주기는 추후 연결할 설계이며 현재 실행 흐름이 아니다.

참고한 로컬 리포는 D:/GitRepo/Project_P, HEAD b37aee0이다. 해당 리포의 Assets/01.Script/01.Manager/InitManager.cs, 02.Scene/SceneLoader.cs, Docs/overview.md와 Docs/architecture.md에서 진입점·씬 로딩·기능별 폴더·README 구분을 참고했다. Project_P가 엔티티별 컨테이너 구조라는 뜻은 아니다.

폴더는 00.Scene부터 시작하고, 엔티티·게임 기능은 10.Player부터 번호를 붙인다. Code 폴더 없이 클래스와 역할별 하위 폴더를 각 기능 폴더 바로 아래에 둔다. 기존 이름공간과 어셈블리 이름은 유지한다. 폴더 번호가 Unity 실행 순서를 강제하지는 않는다. Project_P의 정적 전역 접근, UniTask, UI 베이스와 외부 패키지는 가져오지 않았다.

## 파일별 책임과 소유자

| 파일 | 맡을 역할 | 소유자 |
| --- | --- | --- |
| [Boots.cs](Boots.cs) | 실행 로직 없는 진입점 틀 | 기존 씬 컴포넌트 유지 |
| [PlayerController.cs](../10.Player/Lifecycle/PlayerController.cs) | 명시적 Create·Enable·Tick·Disable, 인벤토리·강화 기록 소유 | 생성·갱신 호출자 연결 예정 |
| [QuestContainer.cs](../16.Quest/QuestContainer.cs) | 퀘스트 진행 상태와 이벤트 연결 | Boots의 quests 필드 |
| [HudContainer.cs](../UI/HudContainer.cs) | 기존 HUD와 게임 상태 연결 | Boots의 hud 필드 |
| [MapContainer.cs](../04.Loading/MapContainer.cs) | 씬 캐시·참조 횟수 관리 | 호출자 연결 예정 |

Boots는 아무 컨테이너도 생성하지 않는다. 기존 자동 맵 로딩을 제거하고 MapContainer의 LoadAsync → Release → CleanUpAsync만 제공한다. 실행 연결은 컨테이너 정리 후 진행한다.

## 전역으로 둘 것과 인스턴스로 둘 것

게임 전체에서 사용하는 수명과 어디서든 접근하는 static은 구분한다. 추후 Boots가 소유할 컨테이너는 private 필드에 둔다. 다른 기능은 Boots 전체를 받아서 컨테이너를 찾아 쓰지 않고, 생성·연결 시 필요한 대상만 전달받는다. 아래 플레이어·퀘스트·HUD 소유 구조는 이전 제안이며 이번 맵 작업에서 구현하지 않는다.

| 대상 | 접근·소유 방식 | 유지·해제 시점 |
| --- | --- | --- |
| 고정 배율, Animator 해시, 고정 색상 | 각 기능 안의 const 또는 static readonly 값 | 타입 수명 동안 사용 |
| 거리·피격 판정 등 상태를 저장하지 않는 함수 | 각 기능 안의 static 함수 허용 | 호출한 입력으로 계산하고 결과 반환 |
| ZombieConfig.playSession | private static 유지. 설정 캐시 갱신 번호로만 사용 | Play 시작마다 갱신 |
| 플레이어 객체·입력·체력·인벤토리·강화 기록 | PlayerController 인스턴스 | 같은 인스턴스가 유지되는 동안 보관. 재생성 시 기록 전달은 추후 연결 |
| 퀘스트 진행 기록 | Boots → QuestContainer가 소유할 인스턴스 | 지역 이동에도 유지, 새 게임에서는 초기화 |
| HUD 연결 | Boots → HudContainer가 소유할 인스턴스 | 지역 이동 시 지역 대상 구독을 교체, 게임 종료 시 해제 |
| 적·적 풀·바닥 아이템 | 각 컨테이너가 소유. 맵 컨테이너는 씬 핸들만 관리 | 갱신·해제 호출은 진입점 연결 단계에서 정함 |
| 지역 씬 로딩 핸들 | MapLoader 내부의 맵별 기록 | 해당 씬 해제 완료까지 보관 |
| 설정·프리팹·아이템 정의 | 에셋 참조 공유. 진행 상태를 에셋에 저장하지 않음 | Inspector 직접 참조인지 Addressables 로딩인지에 따라 수명 관리 |
| 공용 에셋의 Addressables 핸들 | 로딩을 요청한 기능이 소유. 게임 수명 리소스는 게임 수명의 소유자가 관리 | 각 소유자가 자신의 로딩 요청에 대응하는 해제 담당 |

Common 그룹에 들어 있다는 이유로 에셋 인스턴스나 핸들을 static으로 만들지 않는다. Common은 공유 리소스 분류이고, 객체와 핸들의 수명은 사용처가 정한다. 여러 지역이 같은 프리팹을 사용해도 생성된 적과 풀은 지역별이다. 설정 참조 공유와 변경 불가능함도 같지 않다. 배열·곡선·ScriptableObject는 공유 후 임의로 변경하지 않는다.

### 기존 코드에서 아직 이전하지 않은 부분

- PlayerStatUpgradeSession은 PlayerController가 소유하는 인스턴스 강화 기록이다. Create가 배율을 읽고 PlayerStatUpgrade.TryApply가 전달받은 기록을 바꾼다.
- 현재 인벤토리와 강화 기록은 플레이어 인스턴스 수명에 따른다. 플레이어를 재생성해도 기록을 이어 주는 기능과 새 게임 초기화는 이후 실행 연결 단계에서 정한다.
- ZombieConfig.runtimeSettings는 설정 에셋별 캐시다. static playSession은 Play 재시작 시 캐시가 이전 실행에서 재사용되지 않도록 하는 번호이므로 유지한다. 적 체력이나 공격 대상은 여기에 넣지 않는다.
- GroundQuestController와 HUD는 전달받은 PlayerController의 활성화·비활성화 알림을 구독한다. Boots는 이 연결이나 갱신을 아직 호출하지 않는다.

### 연결 후 유지할 흐름

1. Start 씬 실행 → Boots가 게임 수명의 컨테이너를 준비하고 필요한 참조를 연결 → 준비 완료 후 게임 활성화.
2. 맵 로딩 요청 → MapContainer가 Addressables 씬을 로딩 → 핸들과 refCount 보관. 환경 적용은 별도 연결한다. 다른 기능의 연결은 이 로더에서 처리하지 않는다.
3. 맵 해제 요청 → Release로 사용 횟수 감소 → CleanUpAsync로 0인 씬 해제와 cache 기록 제거. 다른 맵이 사용하는 공용 리소스는 그 참조가 유지한다.
4. 새 게임·종료 → HUD·퀘스트 구독 해제 → 지역 정리 → 플레이어와 진행 기록 정리. 새 게임은 새 기록으로 준비.

현재 Start 씬 유지나 새 게임 재시작 기능을 Boots가 제어하지 않는다. 실제 연결 시 기존 additive 씬 흐름에서 Start 씬을 유지하고, Start 씬까지 교체하는 흐름이 생기면 Boots 수명도 함께 설계한다. DontDestroyOnLoad는 중복 방지와 종료 처리가 필요할 때 도입하며 static 접근을 뜻하지 않는다.

## 구현할 호출 순서

입력: Start 씬 실행 → 처리: Boots가 플레이어 준비, 퀘스트·HUD 연결, 지역 로딩과 배치 연결 → 출력: 준비 성공 후 입력과 게임 갱신 시작.

- 일반 C# 컨테이너: Create → Enable → 필요한 경우 Tick → Disable → Dispose.
- ItemContainer와 HudContainer는 매 프레임 갱신 메서드를 만들지 않았다.
- 지역 해제: 해당 지역 생성·갱신 중지 → HUD·퀘스트의 지역 참조 해제 → 적·아이템 정리 → 씬과 로딩 핸들 정리.
- 게임 종료: 구독자 연결 해제 → 모든 지역 정리 → 플레이어 정리.
- 중복 시작, 준비 전 갱신, 로딩 도중 종료에 대한 방어는 실제 기능과 함께 구현한다. 지금은 상태 관리나 구독·로딩 자체가 없다.

## 어셈블리

Boots는 Game.Boot에 속하며 Game.Runtime과 Game.UI를 참조한다. Game.UI는 기존대로 Game.Runtime을 참조한다. 반대 방향의 참조를 추가하지 않는다. 엔티티 컨테이너는 기존 Game.Runtime, HudContainer는 기존 Game.UI에서 컴파일한다.

## 다음 실습과 확인

1. Unity의 컴파일 오류가 없는지 확인한다. 지금은 새 컴포넌트를 Inspector에 연결할 필요가 없다.
2. PlayerController의 Create → Enable → Tick → Disable 호출 순서를 정한다. Create는 프리팹 생성이 아니라 이미 존재하는 컴포넌트의 초기화다.
3. 기존 시작 경로와 중복되지 않도록 한 뒤 Start 씬에 Boots를 연결한다. Boots의 구체적인 Inspector 필드는 그때 추가한다.
4. 적·아이템·퀘스트·HUD 순서로 연결을 옮긴다. 각 객체의 Tick과 이벤트 구독이 한 번만 실행되는지 확인한다.
5. 맵 로딩·해제와 검증 방법은 [04.Loading 안내](../04.Loading/README.md)를 따른다. 다른 컨테이너 이전과 묶어서 진행하지 않는다.

맵 리소스는 MapContainer에서 관리한다. Boots와의 실행 연결은 이후 단계에서 정한다. 이동·전투·보상 같은 상세 규칙은 각 기존 기능 클래스에 유지한다.
