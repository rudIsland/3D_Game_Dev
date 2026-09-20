# 폴더 규칙

## 기본 배치

1. 프로젝트 코드·설정·전용 에셋은 Assets/_Project의 기능별 폴더를 우선한다.
2. 씬은 00.Scene을 맨 앞에 두고, 플레이어부터 엔티티·게임 기능은 10번대에서 시작한다.
3. 별도 Code 폴더를 만들지 않는다. 클래스는 기능 폴더에 두고 필요하면 Lifecycle, Combat 등 역할별 하위 폴더를 사용한다.
4. 번호는 탐색·정렬을 돕는 표기다. Unity 콜백 순서나 의존성을 자동으로 결정하지 않는다.
5. 관련 없는 기존 폴더를 함께 이동하거나 번호를 일괄 변경하지 않는다.

## 현재 폴더 역할

| 위치 | 역할 |
| --- | --- |
| Assets/_Project/00.Scene | Start·Ground·UnderGround와 개발 씬 |
| Assets/_Project/01.Boot | 실행 진입점. 현재 자동 실행 연결 없음 |
| Assets/_Project/02.Core | 기능별 게임 규칙에 종속되지 않는 기반과 Constant |
| Assets/_Project/03.Settings | 공통 설정 에셋 |
| Assets/_Project/04.Loading | MapContainer 등 리소스 로딩·반환 코드 |
| Assets/_Project/05.Manager | 실행 관리 코드 자리. 필요가 생길 때 구현 |
| Assets/_Project/10.Player | 플레이어 코드·설정·리소스 |
| Assets/_Project/11.Enemy | 적 코드·설정·리소스, Shared는 적끼리 공유 |
| Assets/_Project/12.Item | 아이템 |
| Assets/_Project/13.Characters | 캐릭터 공통 체력·전투·생명주기 |
| Assets/_Project/14.Interaction | 상호작용 |
| Assets/_Project/15.Effects | 효과 |
| Assets/_Project/16.Quest | 퀘스트 |
| Assets/_Project/18.GUI | UI 이미지·표시 리소스 |
| Assets/_Project/19.Zone | 구역·생성 지점·맵 영역 |
| Assets/_Project/20.Physics | 물리 관련 코드·데이터 |
| Assets/_Project/90.Editor | 편집기 전용 도구 |
| Assets/_Project/91.Tests | 기존 테스트. 새 테스트 생성·케이스 추가 금지 |
| Assets/_Project/UI | 기존 HUD·미니맵 코드와 에셋 |
| Assets/_Project/Runtime | 기존 실행용 프리팹 |

기존 UI·Runtime 위치는 실제 참조를 따라 유지한다. 이 표를 이유로 에셋 이동을 자동 수행하지 않는다. Core에 전투·퀘스트·플레이어 전용 규칙을 모으지 않는다.

## 어셈블리와 에셋 이동

1. 기존 Game.Runtime, Game.UI, Game.Boot, Game.Editor의 참조 방향을 먼저 확인한다. 폴더 추가만으로 asmdef를 늘리지 않는다.
2. Runtime에서 Editor 코드를 참조하지 않는다. UI와 Boot가 필요한 실행 코드를 참조하도록 하며 순환 참조를 만들지 않는다.
3. 코드의 이름공간은 기존 영역과 대소문자를 유지한다. 상세 기준은 [코드 작성 규칙](coding-style.md)을 따른다.
4. 에셋 이동은 .meta와 함께 처리하고 GUID를 보존한다. 이동 후 씬·프리팹·설정·문서의 참조를 확인한다.
5. Addressables 그룹은 번들 구성이고 폴더는 제작 위치다. Common 그룹에 있다는 이유로 전역 인스턴스나 복제 리소스를 만들지 않는다.

## 문서 위치

| 종류 | 위치 |
| --- | --- |
| 에이전트의 규칙 진입점 | 루트 AGENTS.md |
| 공통 규칙 원문 | Docs/Rules |
| 마지막 세션과 작업 기록 | Docs/Work/last-session.md 및 Docs/Work |
| 기능별 역할·API·설정·확인 방법 | 해당 기능 폴더 README.md |
| 프로젝트 폴더 안내 | Assets/_Project/README.md |

새 규칙은 Docs/Rules에, 새 작업 기록은 Docs/Work에 둔다. 기존 Docs의 기획·작업 문서는 별도 요청 없이 옮기지 않는다. 규칙과 세션 기록은 Assets 밖에 두므로 Unity .meta를 만들지 않는다.
