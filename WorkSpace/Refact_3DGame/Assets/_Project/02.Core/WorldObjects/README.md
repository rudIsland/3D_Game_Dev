# 월드 객체와 구역

씬과 배치 객체의 소유권, 게임 객체의 생성·활성화·갱신·회수를 관리한다. 퀘스트·상호작용·캐릭터의 행동 판단은 각 기능 폴더에서 담당한다.

## 폴더 구성

| 위치 | 설명 |
| --- | --- |
| `Code/IWorldObject.cs`, `Code/WorldObject.cs` | 일반 C# 객체의 생명주기 계약과 중복 호출 방지 |
| `Code/WorldObjectView.cs` | GameObject와 실행용 객체 연결 |
| `Code/WorldObjectManager.cs` | 등록·활성 목록, Tick과 생성·회수 요청 처리 |
| `Code/WorldObjectPool.cs`, `Code/SpawnSettings.cs` | 프리팹 풀과 생성 설정 |
| `../../11.Zone/Code` | Zone 범위, 적 소환 지점, 재진입 소환과 지도 구역 정보 |
| `../../05.Interaction/Code` | 석상 강화·아이템 교환, 상호작용 계약과 안내 |
| `../../07.Quest/Code` | 지상 행동과 출구 도착에 따른 퀘스트 진행 |
| `Code/WorldObjectContainer.cs` | 이름별 씬 로딩·해제, 사용할 지역 환경 선택, 중복 요청 차단·미사용 리소스 정리 |
| `Code/UndergroundLightmaps.cs` | 저장된 지하 라이트맵과 Renderer 좌표 연결 |

## 풀 객체의 동작 순서

1. 공통 Manager의 Awake가 관리 목록을 준비한다. Zone은 Start에서 자신의 SpawnSettings를 자기 씬의 풀로 등록하고 필요한 객체를 생성한다.
2. 새 View를 만들면 `Prepare`로 RuntimeObject를 연결하고, 등록 시 `Create`를 호출한다.
3. `TrySpawn`은 풀에서 View를 꺼내 위치·회전을 지정한 뒤 GameObject와 RuntimeObject를 활성화한다.
4. Manager는 활성 객체만 `Tick`한다. 순회 중 변경 요청은 순회가 끝난 뒤 적용한다.
5. 회수 시 RuntimeObject 비활성화 → View의 풀 반환 초기화 → GameObject 비활성화 → 풀 반환 순서로 처리한다.
6. 등록 해제·종료 시 `Dispose`로 최종 자원을 정리한다.

GameObject의 `SetActive`와 일반 C# 객체의 Enable·Disable은 별개다. 풀 객체의 생성·회수는 Manager를 통해 처리한다. 씬에 직접 놓인 플레이어는 자체 Controller가 Manager에 등록하고 활성 상태를 연결한다.

## Zone 소환 흐름

소환 지점과 NavMesh 확인 → 등록된 SpawnSettings로 소환 → `IZoneEnemy`에 소속 구역·시작 위치 전달 순서다. 시작 시 빈 소환 자리를 채우고, 이후 플레이어가 구역 밖에서 안으로 들어올 때 빈 자리를 다시 채운다. 자동 대기 시간으로 재소환하는 기능은 없다.

`MapArea`는 지도에 표시할 구역 정보를 담당한다. 적 소환을 담당하는 EnemyZoneController와 역할을 구분한다.

## 상호작용과 퀘스트 흐름

플레이어가 대상을 탐지 → `IPlayerInteractable`로 안내·실행 가능 여부 확인 → 강화·교환 실행 → 능력치·인벤토리 변경 순서다. 퀘스트는 인벤토리의 실제 행동 알림과 출구 도착을 진행 상태에 반영한다. 화면 표시는 UI 폴더에서 담당한다.

## Unity에서 확인

- 공통 WorldObjectManager의 SpawnSettings는 비워 둔다. Zone에 소환 설정을 연결하며, 직접 배치한 WorldObjectView는 Start From Scene을 켠다. 같은 씬의 Zone은 같은 풀을 공유한다. 서로 다른 씬의 풀은 별도 SpawnSettings를 사용한다.
- Zone의 BoxCollider, Player, EnemySpawnPoints와 NavMesh를 확인한다. 소환 프리팹에는 IZoneEnemy 구현이 필요하다.
- 석상의 물리 충돌용 Collider와 상호작용 감지용 Collider·레이어를 구분해 확인한다.
- Common을 시작 씬으로 두고 Ground·Underground를 빌드 씬 목록에 등록한다. Common의 WorldObjectContainer가 Load On Start의 씬만 불러온다. 자동 플레이어 이동이나 퀘스트 전환은 수행하지 않는다.
- 지역 환경은 각 씬 루트의 Setup&Lights에 둔다. 로딩 후 해당 지역을 활성 씬으로 지정하며, 동시에 열린 다른 지역의 Setup&Lights는 비활성화한다. 두 지역을 함께 유지할 때 `ApplyEnvironment(name)`으로 사용할 환경을 바꿀 수 있다.
- UndergroundLightmaps는 자식 프리팹을 자동 수집하는 배치 관리자가 아니다. 직렬화된 Renderer와 텍스처 참조로 조명을 복원하므로, 환경을 옮기거나 다시 베이크할 때 해당 참조도 확인한다.

관련 문서: [공통 캐릭터](../Characters/README.md), [좀비](../../02.Enemy/Zombie/README.md), [NightShade](../../02.Enemy/NightShade/README.md), [아이템](../../03.Item/README.md).

## 씬과 배치 컨테이너

`WorldObjectContainer` 하나가 `Dictionary<string, Scene>`로 로딩한 씬 정보만 보관한다. 모든 GameObject를 별도 목록에 등록하지 않는다. 퀘스트·인벤토리·미니맵·플레이어 이동을 참조하지 않는다.

```csharp
container.Load("Underground"); // 비동기 씬 로딩 요청. 중복·동시 작업은 false
// 이후 IsBusy가 false이고 IsLoaded("Underground")가 true이면 로딩 완료
// 두 지역을 함께 로딩한 상태에서 지상 환경으로 바꾸는 별도 요청 예:
container.ApplyEnvironment("Ground"); // Ground가 로딩되어 있어야 한다.
// 사용을 마치고 별도 해제 요청을 보내는 시점:
container.Unload("Underground"); // 씬 해제와 미사용 리소스 정리가 끝날 때까지 IsBusy 유지
```

Load는 기존 씬 배치를 비동기로 읽고 해당 지역의 환경을 적용한다. 개별 객체 조회·생성·제거 API와 전체 계층 등록 순회는 제거했다. 실행 중 생성이 필요한 적·아이템은 기존 생성기가 자기 씬에 배치한다. 씬 로딩 자체의 생성 비용은 남는다.

Unload는 Unity에 씬 전체 해제를 요청하고 씬 정보를 제거한다. 해당 씬의 풀은 WorldObjectManager가 정리한다. 남은 Renderer·Terrain의 라이트맵만 보존하고 UnloadUnusedAssets를 기다린다. 컨테이너를 비활성화할 때 진행 중인 로딩과 소유 씬도 정리한다.

공통 상주는 Common이 참조하고, 지상 전용은 Ground, 지하 전용은 Underground가 참조한다. 두 맵에서 사용하는 원본은 맵 공용이며 복제하지 않는다. 맵 공용 에셋을 Common에 추가 등록하지 않는다. 에셋의 폴더명보다 실제 참조 관계로 구분하며, 소속은 에디터 도구와 CSV로 확인한다.

씬이 해제되어도 다른 씬·정적 필드·외부 객체에서 참조하는 에셋은 남을 수 있다. 컨테이너는 모든 객체 참조를 보관하지 않으며, 실제 메모리 검증은 반복 로딩 전후의 메모리와 남은 참조를 비교한다. 소속표의 에셋 개수는 메모리 측정값이 아니다.

Play 중 `Tools > World > 리소스 소속` 상단의 불러오기·해제 버튼으로 조작할 수 있다. 입력 이동과 안전한 플레이어 위치는 호출자가 관리한다. 맵을 해제하면 그 맵의 충돌체도 제거된다.

Region의 출입구 자동 이동·퀘스트 연결·아이템 획득 및 보스 처치 기록은 제거했다. 맵을 다시 불러오면 해당 씬의 원래 아이템·보스 배치가 다시 생성된다. Common의 인벤토리·퀘스트 자체는 유지된다.
