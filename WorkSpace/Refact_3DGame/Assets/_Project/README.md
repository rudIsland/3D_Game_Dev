# 프로젝트 엔티티 안내

플레이어, 적, 아이템과 월드 객체의 코드·설정·모델을 찾기 위한 안내다. 엔티티별 설명은 각 폴더의 README에서 확인한다.

## 엔티티와 공통 기능

진입점과 생성 순서는 [01.Boot 안내](01.Boot/README.md)에서 확인한다. Project_P의 진입점·씬 로딩·기능별 파일 구분을 참고했다. Start의 Boots → GameManager가 Ground·플레이어·HUD를 순서대로 준비하고 이동·카메라 추적·체력과 스태미나 표시를 시작한다. 적·퀘스트·미니맵·맵 교체는 이후 단계다.

| 폴더 | 역할 |
| --- | --- |
| [00.Scene](00.Scene/README.md) | Start·Ground·UnderGround 씬과 지하 조명 복원 |
| [01.Boot](01.Boot/README.md) | Boots 진입점, 컨테이너 소유 관계와 이전 순서의 틀 |
| [02.Core](02.Core/README.md) | 공통 객체 생명주기, 싱글톤 기반과 공통 상수 |
| [03.Settings](03.Settings) | 공통 렌더링 설정 |
| [04.Loading](04.Loading/README.md) | MapManager의 맵 요청 전달, AddressableManager의 에셋·씬 핸들·사용 횟수 관리 |
| [05.Manager](05.Manager/README.md) | GameManager의 맵·플레이어·HUD 준비와 반환 순서 |
| [10.Player](10.Player/README.md) | 플레이어 입력, 이동, 전투, 락온, 가방과 능력치 |
| [11.Enemy](11.Enemy/README.md) | 몬스터별 코드와 리소스 목록 |
| [11.Enemy/Shared](11.Enemy/Shared/README.md) | 적의 공통 전투 정보, 공격 데이터와 길 찾기 |
| [12.Item](12.Item/README.md) | 아이템 정의, 배치와 획득 |
| [13.Characters](13.Characters/README.md) | 캐릭터 체력, 생명주기, 피해와 피격의 공통 규칙 |
| [14.Interaction](14.Interaction) | 석상 강화·아이템 교환과 상호작용 계약 |
| [15.Effects](15.Effects) | 타격 파티클 재생 코드와 효과 리소스 |
| [16.Quest](16.Quest) | 지상 퀘스트 진행 |
| [18.GUI](18.GUI) | UI 이미지와 표시용 리소스 |
| [19.Zone](19.Zone) | 적 구역·생성 지점과 미니맵 영역·층 정보 |
| [20.Physics](20.Physics) | 계단 보행용 충돌 데이터 |
| [90.Editor](90.Editor) | 개발용 Editor 창과 Addressables 치트 창 ([사용법](04.Loading/README.md#addressables-cheat-창에서-확인)) |

번호는 폴더 정렬용이다. Scene을 맨 앞에 두고, 엔티티·게임 기능은 Player의 10번부터 시작한다. 별도 Code 폴더 없이 각 기능 폴더에 클래스를 두며 Lifecycle, Combat 같은 역할별 하위 폴더는 유지한다.

00.Scene → 01.Boot → 02.Core → 03.Settings → 04.Loading → 05.Manager 순으로 시작·기반·설정·로딩·관리 위치를 찾는다. 폴더 번호가 Unity 콜백 실행 순서를 결정하지는 않는다. 현재 Start → Boots.Start → GameManager.Start → MapManager.LoadAsync → AddressableManager.LoadSceneAsync → Ground 추가 로딩 → 플레이어·HUD 생성·활성화 순서로 동작한다.

## 실행 흐름

`02.Core`에는 특정 캐릭터나 게임 규칙을 모르는 객체 실행 기반을 둔다. 캐릭터 공통 규칙은 `13.Characters`, 플레이어 전용 규칙은 `10.Player`, 적 공통 규칙은 `11.Enemy/Shared`에 둔다. 폴더가 달라도 엔티티 실행 코드는 기존 `Game.Runtime` 어셈블리를 공유하며, 각 기능 폴더의 `Game.Runtime.asmref`로 연결한다. Boots는 `Game.Boot`, UI는 `Game.UI`를 사용한다.

1. 씬의 Controller와 설정 에셋이 실행에 필요한 객체와 참조를 준비한다.
2. Boots.Update → GameManager.Tick → PlayerController.Tick → PlayerWorldUnit.Tick으로 플레이어를 갱신한다. 다른 엔티티 컨테이너의 실행 연결은 다음 단계다.
3. 플레이어는 입력을, 적은 대상과 전투 상황을 읽어 행동을 결정한다.
4. 이동·충돌·공격 판정 결과를 Transform, Animator, 체력과 인벤토리에 반영한다.
5. UI는 연결된 객체의 정보와 변경 알림을 받아 화면에 표시한다.

아이템 생성과 일부 씬 기능은 MonoBehaviour에서 직접 동작한다. 모든 오브젝트가 월드 객체 풀을 사용하는 것은 아니다.

## 함께 확인할 폴더

- `Runtime/Characters`: 플레이어 배치용 `PlayerRoot.prefab`.
- `00.Scene`: 게임 씬, 지하 씬과 개발용 씬·배치 설정.
- `20.Physics`: 계단 보행용 충돌 데이터.
- `UI`: 전투 HUD, 머리 위 체력바와 미니맵 코드.
- `18.GUI`: UI 이미지와 표시용 리소스.
- `90.Editor`: `Tools > Cheat > 치트 창`의 왼쪽 Addressable·플레이어 탭과 미니맵 조정·에셋 구성 도구를 제공하며, 플레이어 탭은 기존 목적지 검색·바닥 확인·이동 처리를 PlayerMovePanel에서 실행한다.

설명을 갱신할 때는 실제 호출 지점과 Inspector 연결을 함께 확인한다. 리소스가 있다는 이유만으로 해당 몬스터의 AI가 구현되었다고 판단하지 않는다.
