# 공통 기반

`ObjectLifecycle`은 일반 C# 객체 하나의 생명주기 순서와 중복 호출을 처리한다. 싱글톤·등록 목록·자동 갱신 기능은 없으며 소유자가 직접 호출한다. `Singleton<T>`와 공통 상수는 기존 역할을 유지한다.

## 호출 흐름

Init → Create → Enable → Tick → Disable → Release의 여섯 단계다. Init은 외부 참조·설정 준비, Create는 내부 객체·자원 생성, Enable은 활성화, Tick은 갱신, Disable은 비활성화, Release는 자원 정리와 사용 종료다. 필요한 OnInit·OnCreate·OnEnable·OnTick·OnDisable·OnRelease만 구현한다. 프레임 처리가 없는 객체는 Tick을 호출할 필요가 없다.

각 단계는 자신의 On 콜백만 실행하며 다른 단계를 자동 호출하지 않는다. 활성 상태의 Release는 거절하므로 소유자가 Disable → Release를 명시한다. Release 후 재초기화·재생성·재활성화는 거절한다. 재사용은 Disable → Enable로 처리한다. Unity GameObject 파괴와 Addressables 반환은 소유자가 담당한다.

중복 호출은 무시하고 Init 전 Create, Create 전 Enable, 해제 후 재실행은 거절한다. 재진입 방지를 위해 상태는 콜백 전에 기록한다. 콜백 실패는 호출자에게 전달하며 자동 정리를 실행하지 않는다. 호출자는 실패한 객체를 계속 사용하지 않고 필요한 정리를 요청해야 하며 정리 콜백은 부분 초기화 상태를 처리해야 한다.

## 현재 연결

Unit이 ObjectLifecycle을 상속한다. 참조는 기존 생성자로 전달받고, 소유자인 PlayerController·EnemyContainer가 Init → Create를 명시한다. 내부 생성은 OnUnitCreate, 자원 정리는 OnUnitRelease로 전달한다. 종료는 소유자가 Disable → Release를 요청한다. Unit.Dispose는 IDisposable 호환용으로만 Disable → Release를 연결한다. 퀘스트와 다른 객체는 이번 변경에서 이전하지 않는다.

컴파일·실행 확인 결과는 [캐릭터 안내](../13.Characters/README.md#변경-후-확인)를 따른다.
