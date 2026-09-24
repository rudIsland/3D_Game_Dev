# 게임 시작과 반환

일반 C# 클래스 [GameManager](GameManager.cs)가 첫 Ground, PlayerRoot, CombatHud의 준비 순서와 생성 객체·성공한 로드 요청을 소유한다. 싱글톤이 아니며 Start 씬의 Boots가 하나를 생성한다. 플레이어 Init에 이동 카메라를 전달하고 Tick에서 PlayerController.Tick을 호출한다. 별도 유닛 생명주기 관리자는 없다. 이 폴더는 Game.Boot.asmref로 Game.Boot에 속한다.

PlayerController는 Init 성공 후 준비된 한 명을 Instance로 제공한다. GameManager.Start는 이미 준비된 플레이어가 있으면 추가 로딩 전에 거절한다. Instance는 원본 핸들을 소유하거나 자동 생성하지 않으며, GameManager는 자신이 생성한 플레이어 참조로 갱신·반환한다. 플레이어 Release로 등록을 지운 뒤 객체 파괴 완료를 기다리고 원본 요청을 반환한다.

입력: `Init(Camera, Scene)` → `Start()` → 처리: Ground를 로드하고 맵 내부의 MapScene 하나를 확인, 활성 씬 지정, Start 소속 비활성 GameObjects 아래에 플레이어와 HUD를 순서대로 생성·연결 → 출력: 부모 활성화, 환경음 시작, IsReady=true. Boots.Update가 Tick(deltaTime)을 호출한다.

`Release()`는 갱신 중지 → 플레이어 비활성화 → HUD 구독 해제 → 플레이어 입력 해제 → 객체 파괴 완료 대기 → HUD·플레이어 원본 요청 반환 → 환경음 정지 → Ground 요청 반환·씬 정리 순서다. 로딩 중 반환은 진행 중인 작업이 완료된 뒤 성공한 요청만 반환한다. 중복 Start·Release는 같은 Task를 반환하고, 반환한 관리자는 재시작하지 않는다. 새 실행에는 새 GameManager를 만든다.

`Quit()`는 Play 종료를 기록하고 입력·HUD·환경음을 정지한다. 종료 중에는 새 비동기 씬 해제를 시작하지 않으며 Unity의 Play 종료 정리를 따른다. 시작 실패는 단계와 대상을 예외로 알리고 부분 생성 객체와 완료된 요청을 정리한다.

맵 요청은 [MapManager](../04.Loading/MapManager.cs), 핸들·사용 횟수는 [AddressableManager](../04.Loading/AddressableManager.cs)가 담당한다. 공통 IsBusy 계약 때문에 Ground → PlayerRoot → CombatHud를 순서대로 await한다. 맵 교체·적 실행·퀘스트·미니맵·BGM은 연결하지 않는다.

설정과 Play 확인 방법은 [진입점 안내](../01.Boot/README.md)를 따른다.

## 확인과 남은 작업

Play에서 준비 후 에셋 2·씬 1, 중복 Start·Release의 동일 Task 반환, 부분 초기화 실패·명시적 반환·로딩 직후 반환·Boot 파괴 후 객체와 요청·캐시 0을 확인했다. Domain Reload 비활성 재시작도 확인했다. 모든 외부 로딩 실패 조합과 실제 번들 빌드는 미검증이며 자세한 결과는 진입점 안내에 둔다.
