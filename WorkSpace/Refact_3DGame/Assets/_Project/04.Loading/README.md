# 맵 리소스 로딩

[MapContainer](MapContainer.cs)는 Ground·UnderGround 씬의 핸들과 사용 횟수만 보관하는 일반 C# 클래스다. World.Loading 이름공간과 Game.Runtime 어셈블리를 사용한다. Project_P의 cache/refCount 방식을 참고하되 전역 static과 자동 실행 연결은 넣지 않았다.

| 항목 | 의미 |
| --- | --- |
| cache | Address별 씬 로딩 핸들과 refCount |
| ResourceCount | 보관 중인 씬 수. 씬 내부 오브젝트·텍스처 수가 아니다 |
| GetRefCount(address) | 해당 씬의 사용 요청 횟수 |
| IsBusy | 로딩·정리 진행 여부 |

입력: LoadAsync(Address) → 최초 요청은 Additive 로딩, 재요청은 기존 씬 반환 → refCount 증가.
Release(Address)는 사용 횟수만 줄이고 0 아래로 내려가지 않는다. CleanUpAsync는 refCount가 0인 씬을 해제하고 cache에서 제거한다. refCount는 컨테이너의 사용 횟수이며 Addressables 내부 참조 수와 별개다.

## 호출 예시 — 실행 연결은 나중에

```csharp
var maps = new MapContainer();
await maps.LoadAsync(ConstantValid.GroundMapAddress); // 씬 1개, 참조 1
await maps.LoadAsync(ConstantValid.GroundMapAddress); // 씬 1개, 참조 2
maps.Release(ConstantValid.GroundMapAddress);         // 참조 1
await maps.CleanUpAsync();                           // 사용 중이므로 유지
maps.Release(ConstantValid.GroundMapAddress);         // 참조 0
await maps.CleanUpAsync();                           // 씬 해제, 씬 0개
```

호출자는 World.Loading과 Core.ConstantValid를 사용한다. Unity 메인 스레드에서 작업 완료를 await한 뒤 다음 요청을 보낸다. 작업 중 다른 요청은 예외로 거절한다. 로딩 실패 시 핸들을 반환하고 캐시에 추가하지 않는다.

같은 씬은 하나의 컨테이너를 통해 관리하고 외부 SceneManager로 임의 해제하지 않는다. 컨테이너를 버리기 전에 성공한 LoadAsync마다 Release를 맞추고 CleanUpAsync 완료를 기다린다. 자동 맵 교체·조명 선택·시간 정지·플레이어 배치·종료 연결은 없다.

## Unity에서 확인

Start의 MapLoading 오브젝트와 기존 MapManager·MapLoader는 제거했다. Boots도 빈 컴포넌트이므로 Play만으로 맵이 로딩되지 않는다. MapContainer는 Inspector에 붙이지 않는다.

Ground·UnderGround의 Addressables 등록은 유지한다. 실행 연결 후 위 순서에 따라 ResourceCount와 GetRefCount를 확인한다. 새 테스트 코드는 만들지 않으며 현재 Unity 컴파일·실행 검증은 보류한다.
