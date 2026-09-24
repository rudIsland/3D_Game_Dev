# 맵 리소스 로딩

[AddressableManager.Instance](AddressableManager.cs)는 일반 에셋과 씬의 실제 로딩·핸들·사용 횟수·해제를 관리하는 싱글톤이다. 프리팹 원본, Sprite, AudioClip, ScriptableObject 등 `UnityEngine.Object` 에셋이 대상이며, 생성된 GameObject 인스턴스의 수명은 호출자가 관리한다. 씬 로딩은 Boots → GameManager → MapManager → AddressableManager 순서다. GameManager가 PlayerRoot와 CombatHud 원본을 순서대로 로드·생성한다.

입력: `LoadAssetAsync<T>(key)` → 처음 요청이면 Addressables로 로드하고 핸들을 보관, 같은 키를 다시 요청하면 캐시된 에셋을 반환 → 요청마다 사용 횟수 증가. `Release(key)`는 횟수만 줄이고, `CleanUpUnusedAssets()`가 0인 핸들을 Addressables에 반환한다. `AssetCount`는 캐시된 에셋 수이며 메모리 사용량이 아니다. 호출자는 성공한 로드마다 Release를 한 번 맞추고, 생성한 인스턴스를 정리한 뒤 원본 프리팹을 반환한다.

현재는 로딩·정리 작업 중 다른 요청을 거절한다. 작업 완료를 `await`한 뒤 다음 요청을 보낸다. 로딩 실패 시 핸들을 반환하고 캐시에 넣지 않으며, 같은 키를 다른 타입으로 요청하면 오류를 낸다. 키 자동 변환과 씬 전환 시 자동 정리는 없다.


## 플레이어와 전투 HUD 프리팹

두 프리팹은 각각 별도 Addressables 그룹에 등록되어 있다. 주소는 프리팹 이름과 같게 유지한다.

| 그룹 | 주소 | 프리팹 |
| --- | --- | --- |
| Player | `PlayerRoot` | [PlayerRoot.prefab](../Runtime/Characters/PlayerRoot.prefab) |
| CombatHud | `CombatHud` | [CombatHud.prefab](../18.GUI/CombatHud/CombatHud.prefab) |

각 그룹은 기존 Common의 번들·콘텐츠 갱신 설정만 복사해 만들었다. `LoadAssetAsync<GameObject>(주소)`로 원본을 로드하고, 사용이 끝나면 `Release(주소)`와 `CleanUpUnusedAssets()`로 반환한다. GameManager가 비활성 부모 아래에서 생성·플레이어 초기화·HUD 연결을 마친 뒤 활성화한다. 반환 시 객체 파괴 완료를 기다린 뒤 원본 핸들을 정리한다. 실제 번들 빌드는 별도 검증이다.


Unity 컴파일, 단일 창의 탭 전환, Play에서 목록 선택·프리팹 생성·삭제와 현황 갱신을 확인했다. 기존 게임 요청 유지와 창 닫기 후 자체 요청 정리도 확인했다. 실제 번들·Player 빌드는 미실행이다.

[MapManager.Instance](MapManager.cs)는 맵 요청을 AddressableManager에 전달하는 일반 C# 싱글톤이다. LoadAsync → LoadSceneAsync, Release → ReleaseScene, CleanUpAsync → CleanUpUnusedScenesAsync로 위임한다. 핸들과 사용 횟수를 중복 보관하지 않는다. World.Loading 이름공간과 Game.Runtime 어셈블리를 사용한다. 두 로딩 객체의 생성자는 private이며, 각 호출자는 자신이 요청한 사용 횟수만 반환한다. Cheat 창도 자신이 성공한 로드 횟수를 별도로 기록한다.

맵 전체를 불러오는 짧은 키는 [MapConstant.cs](../02.Core/Constant/MapConstant.cs)에 이미 정의되어 있다. `ConstantValid.GroundMapAddress`는 `Ground`, `ConstantValid.UnderGroundMapAddress`는 `UnderGround`이며 각 키는 해당 씬 전체를 가리킨다. 같은 키로 `LoadAsync` → `Release` → `CleanUpAsync`를 호출한다. 씬 로딩은 그 씬이 참조하는 리소스를 함께 로드하며, 씬 해제 후에도 다른 사용자가 참조하는 공용 리소스는 유지될 수 있다. 씬 밖에서 별도로 로드한 에셋은 해당 호출자가 반환한다.

| 항목 | 의미 |
| --- | --- |
| AddressableManager.loadedScenes | Address별 씬 로딩 핸들과 refCount |
| ResourceCount | 보관 중인 씬 수. 씬 내부 오브젝트·텍스처 수가 아니다 |
| GetRefCount(address) | 해당 씬의 사용 요청 횟수 |
| IsBusy | AddressableManager의 일반 에셋·씬 로딩·정리 공통 진행 상태 |

입력: LoadAsync(Address) → 최초 요청은 Additive 로딩, 재요청은 기존 씬 반환 → refCount 증가.
Release(Address)는 사용 횟수만 줄이고 0 아래로 내려가지 않는다. CleanUpAsync는 AddressableManager.CleanUpUnusedScenesAsync로 위임해 사용 0인 씬을 해제하고 loadedScenes에서 제거한다. refCount는 사용 요청 횟수이며 Addressables 내부 참조 수와 별개다.

## 호출 예시

맵 씬 이름과 Addressables 주소는 같게 유지한다. 편집기 조명 도구도 `GroundMapAddress`·`UnderGroundMapAddress`로 열린 씬을 조회하며, 별도의 맵 이름 상수는 두지 않는다.

```csharp
var maps = MapManager.Instance;
await maps.LoadAsync(ConstantValid.GroundMapAddress); // 씬 1개, 참조 1
await maps.LoadAsync(ConstantValid.GroundMapAddress); // 씬 1개, 참조 2
maps.Release(ConstantValid.GroundMapAddress);         // 참조 1
await maps.CleanUpAsync();                           // 사용 중이므로 유지
maps.Release(ConstantValid.GroundMapAddress);         // 참조 0
await maps.CleanUpAsync();                           // 씬 해제, 씬 0개
```

호출자는 World.Loading과 Core.ConstantValid를 사용한다. Unity 메인 스레드에서 작업 완료를 await한 뒤 다음 요청을 보낸다. 작업 중 다른 요청은 예외로 거절한다. 로딩 실패 시 핸들을 반환하고 캐시에 추가하지 않는다.

같은 씬은 MapManager.Instance를 통해 관리하고 외부 SceneManager로 임의 해제하지 않는다. 사용을 끝낼 때 성공한 LoadAsync마다 Release를 맞추고 CleanUpAsync 완료를 기다린다. Domain Reload를 꺼도 Play 시작마다 싱글톤 참조는 초기화된다. 첫 맵 활성 씬 지정·플레이어 배치·게임 반환은 GameManager가 맡는다. 로더 자체의 자동 맵 교체·조명 선택·시간 정지는 없다.
