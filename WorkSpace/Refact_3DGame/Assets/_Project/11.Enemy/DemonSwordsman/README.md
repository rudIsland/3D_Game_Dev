# DemonSwordsman 리소스

검 형태와 야수 형태의 모델·애니메이션 리소스를 보관한다. 현재 이 폴더에는 전용 C# 행동 코드가 없다. `PhaseChange` 동작 파일이 있다는 것만으로 전투 중 형태 전환 로직이 구현된 것은 아니다.

## 폴더 구성

| 폴더 | 설명 |
| --- | --- |
| `Models/Prefabs` | `DemonSwordsmanBoss.prefab` 모델 프리팹 |
| `Models/Animations/Clips/Sword` | 검 형태 공격·피격·이동·사망·형태 전환 동작 |
| `Models/Animations/Clips/Beast` | 야수 형태 공격·피격·이동·사망·형태 전환 동작 |
| `Models/Animations/Controllers` | Animator Controller 리소스 |
| `Models/Animations/Sources` | 검·야수 형태 애니메이션 원본 |
| `Models/Meshes`, `Models/Materials`, `Models/Textures` | 외형 리소스 |

## 사용 흐름과 확인

프리팹·동작 선택 → 씬 또는 캐릭터 구성에 연결 → 외형과 애니메이션 확인 순서로 사용한다. 전투에 투입하려면 행동 코드, 피해 판정과 소환 설정의 실제 연결을 별도로 확인해야 한다.

개발용 `DemonSwordsmanTestEnemy.prefab`은 `00.Scene/Dev/CharacterTest/Prefabs`에 있다. 파일 이름만 보고 이 폴더에 독립된 AI가 있다고 판단하지 않는다. Unity에서 두 형태의 Animator, Avatar, 무기와 동작 전환을 확인한다.

관련 문서: [적 목록](../README.md).
