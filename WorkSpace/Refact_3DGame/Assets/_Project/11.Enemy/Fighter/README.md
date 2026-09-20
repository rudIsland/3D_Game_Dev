# Fighter 리소스

Fighter 모델과 애니메이션을 보관한다. 현재 `Code` 폴더에는 C# 파일이 없으며, 이 폴더의 전용 탐지·추격·공격 로직은 작성되어 있지 않다.

## 폴더 구성

| 폴더 | 설명 |
| --- | --- |
| `Code` | 전용 코드가 없는 빈 폴더 |
| `Models/Prefabs` | `Fighter.prefab` |
| `Models/Animations/Clips` | 공격·등장·대기·이동·사망 동작 |
| `Models/Animations/Controllers`, `Models/Animations/Sources` | Animator Controller와 원본 애니메이션 |
| `Models/Meshes`, `Models/Materials`, `Models/Textures` | 모델과 외형 리소스 |

## 사용 흐름과 확인

모델 프리팹 선택 → Animator·Avatar와 동작 연결 확인 → 씬에서 외형과 재생 확인 순서로 사용한다. 프리팹 배치만으로 전투 AI가 만들어지지는 않는다.

전투용으로 연결할 때는 Controller, 런타임 유닛, 공격 판정과 소환 설정을 준비하고 [적 공통 구조](../README.md)를 확인한다.
