# 폴더 규칙

## 기본 배치

1. 프로젝트 코드·설정·전용 에셋은 Assets/_Project의 기능별 폴더를 우선한다.
2. 실행 시작 흐름에 맞춰 씬은 00.Scene, 진입점은 01.Boot, 매니저는 02.Manager, 로더는 03.Loading에 둔다. 게임 데이터 목록과 전역 보관은 06.Data에 둔다. 10번 이전은 게임 흐름·공통 기반, 10번부터는 기능별 엔티티 구현이며 기능 간 연결 코드는 10번 이전에 둔다.
3. 별도 Code 폴더를 만들지 않는다. 클래스는 기능 폴더에 두고 필요하면 Lifecycle, Combat 등 역할별 하위 폴더를 사용한다.
4. 번호는 탐색·정렬을 돕는 표기다. Unity 콜백 순서나 의존성을 자동으로 결정하지 않는다.
5. 관련 없는 기존 폴더를 함께 이동하거나 번호를 일괄 변경하지 않는다.

## 현재 폴더 역할

| 위치 | 역할 |
| --- | --- |
| Assets/_Project/00.Scene | Start·Ground·UnderGround와 개발 씬; GameScene은 Game.Runtime, Play의 StartScene·MapPlayScene은 Game.Boot |
| Assets/_Project/01.Boot | 실행 진입점. 전역 데이터 준비·첫 씬 로드·종료 |
| Assets/_Project/02.Manager | 플레이어·HUD 생성 담당과 AudioListenerManager; Resources에는 MapManager·AddressableManager. 생성 담당은 Game.Boot, Resources는 Game.Runtime |
| Assets/_Project/03.Loading | SceneLoader의 씬 전환·요청 반환. Loading 이름공간·Game.Runtime |
| Assets/_Project/04.Core | 공통 생명주기·싱글톤·상수와 Character의 캐릭터 공통 코드 |
| Assets/_Project/04.Core/Character | 캐릭터 공통 체력·전투·생명주기, 이름공간은 Core |
| Assets/_Project/05.Settings | 공통 설정 에셋 |
| Assets/_Project/06.Data | 게임 시작 입력 목록·종류별 전역 데이터 컨테이너 |
| Assets/_Project/10.Player | 플레이어 코드·설정·리소스 |
| Assets/_Project/11.Enemy | 적 코드·설정·리소스, Shared는 적끼리 공유 |
| Assets/_Project/12.Item | 아이템 |
| Assets/_Project/14.Interaction | 상호작용 |
| Assets/_Project/15.Effects | 효과 |
| Assets/_Project/16.Quest | 퀘스트 |
| Assets/_Project/18.GUI | UI 이미지·표시 리소스 |
| Assets/_Project/19.Zone | 맵 영역·미니맵 층 정보; 소환 연결 코드는 00.Scene |
| Assets/_Project/20.Physics | 물리 관련 코드·데이터 |
| Assets/_Project/90.Editor | 편집기 전용 도구와 기능별 Cheat 창 |
| Assets/_Project/UI | 기존 HUD·미니맵 코드와 에셋 |
| Assets/_Project/Runtime | 기존 실행용 프리팹 |

기존 UI·Runtime 위치는 실제 참조를 따라 유지한다. 이 표를 이유로 에셋 이동을 자동 수행하지 않는다. 여러 캐릭터가 공유하는 체력·전투·생명주기는 Core/Character에 두고, 퀘스트 진행·플레이어 입력·적 AI 같은 전용 규칙은 각 기능 폴더에 둔다.
EditMode·PlayMode 테스트를 위한 `91.Tests` 폴더와 테스트 어셈블리는 만들지 않는다. 실제 동작을 조작할 기능별 Cheat 창은 `90.Editor`에 둔다.

엔티티 자체의 치트 전용 코드·필드·메뉴는 대상 클래스와 같은 폴더의 `엔티티이름Cheat.cs`에 둔다. 공용 Editor 창과 구분하며 분리 방식은 [검증 규칙](work-rules.md#검증-방식)을 따른다.

## 어셈블리와 에셋 이동

1. 기존 Game.Runtime, Game.UI, Game.Boot, Game.Editor의 참조 방향을 먼저 확인한다. 폴더 추가만으로 asmdef를 늘리지 않는다.
2. Runtime에서 Editor 코드를 참조하지 않는다. UI와 Boot가 필요한 실행 코드를 참조하도록 하며 순환 참조를 만들지 않는다.
3. 엔티티 폴더와 그 하위 코드의 이름공간은 번호·구분자를 뺀 엔티티 폴더 이름으로 통일한다. 상세 기준과 예시는 [코드 작성 규칙](coding-style.md#이름공간)을 따른다.
4. 에셋 이동은 .meta와 함께 처리하고 GUID를 보존한다. 이동 후 씬·프리팹·설정·문서의 참조를 확인한다.
5. Addressables 그룹은 번들 구성이고 폴더는 제작 위치다. Common 그룹에 있다는 이유로 전역 인스턴스나 복제 리소스를 만들지 않는다.

## 문서 위치

| 종류 | 위치 |
| --- | --- |
| 에이전트의 규칙 진입점 | 루트 AGENTS.md |
| 공통 규칙 원문 | Docs/Rules |
| 마지막 세션과 작업 기록 | Docs/Work/last-session.md 및 Docs/Work |
| 기능별 역할·API·설정·현재 구현·검증·남은 작업 | 해당 기능 폴더 README.md |
| 프로젝트 폴더 안내 | Assets/_Project/README.md |

새 규칙은 Docs/Rules에, 새 작업 기록은 Docs/Work에 둔다. 기존 Docs의 기획·작업 문서는 별도 요청 없이 옮기지 않는다. 규칙과 세션 기록은 Assets 밖에 두므로 Unity .meta를 만들지 않는다.

기능 변경 시 해당 README도 같은 단계에서 즉시 갱신한다. 문서 선택·여러 폴더에 걸친 변경·README가 없는 경우는 [작업 규칙](work-rules.md#기능-폴더의-현재-상태-갱신)을 따른다. 이 문서의 폴더 표는 책임 배치 안내이며 기능별 진행 상태는 각 README에서 관리한다.
