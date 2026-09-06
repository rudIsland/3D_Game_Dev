# 프로젝트 엔티티 안내

플레이어, 적, 아이템과 월드 객체의 코드·설정·모델을 찾기 위한 안내다. 엔티티별 설명은 각 폴더의 README에서 확인한다.

## 엔티티와 공통 기능

| 폴더 | 역할 |
| --- | --- |
| [00.Core/Characters](00.Core/Characters/README.md) | 캐릭터 체력, 생명주기, 피해와 피격의 공통 규칙 |
| [01.Player](01.Player/README.md) | 플레이어 입력, 이동, 전투, 락온, 가방과 능력치 |
| [02.Enemy](02.Enemy/README.md) | 몬스터별 코드와 리소스 목록 |
| [02.Enemy/Shared](02.Enemy/Shared/README.md) | 적의 공통 전투 정보, 공격 데이터와 길 찾기 |
| [03.Item](03.Item/README.md) | 아이템 정의, 배치와 획득 |
| [00.Core/WorldObjects](00.Core/WorldObjects/README.md) | 씬 로딩·해제 컨테이너, 객체 생성·갱신·회수와 프리팹 풀 |
| [05.Interaction](05.Interaction/Code) | 석상 강화·아이템 교환과 상호작용 계약 |
| [07.Quest](07.Quest/Code) | 지상 퀘스트 진행 |
| [11.Zone](11.Zone/Code) | 적 구역·생성 지점과 미니맵 영역·층 정보 |
| [12.Physics](12.Physics) | 계단 보행용 충돌 데이터 |

## 전체 동작 흐름

1. 씬의 Controller와 설정 에셋이 실행에 필요한 객체와 참조를 준비한다.
2. `WorldObjectManager`가 등록된 활성 객체의 `Tick`을 호출한다.
3. 플레이어는 입력을, 적은 대상과 전투 상황을 읽어 행동을 결정한다.
4. 이동·충돌·공격 판정 결과를 Transform, Animator, 체력과 인벤토리에 반영한다.
5. UI는 연결된 객체의 정보와 변경 알림을 받아 화면에 표시한다.

아이템 생성과 일부 씬 기능은 MonoBehaviour에서 직접 동작한다. 모든 오브젝트가 월드 객체 풀을 사용하는 것은 아니다.

## 함께 확인할 폴더

- `Runtime/Characters`: 플레이어 배치용 `PlayerRoot.prefab`.
- `0_Scenes`: 게임 씬, 지하 씬과 개발용 씬·배치 설정.
- `12.Physics`: 계단 보행용 충돌 데이터.
- `UI`: 전투 HUD, 머리 위 체력바와 미니맵 코드.
- `10_GUI`: UI 이미지와 표시용 리소스.
- `90.Editor`: 플레이어 이동, 미니맵 조정과 에셋 구성 도구.

설명을 갱신할 때는 실제 호출 지점과 Inspector 연결을 함께 확인한다. 리소스가 있다는 이유만으로 해당 몬스터의 AI가 구현되었다고 판단하지 않는다.
