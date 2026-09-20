# Mutant 리소스

Mutant 모델과 애니메이션을 보관한다. 현재 전용 C# 파일이 없어 이 폴더의 전용 행동 로직은 없다.

## 폴더 구성

| 폴더 | 설명 |
| --- | --- |
| `Models/Prefabs` | `Mutant.prefab` |
| `Models/Animations/Clips` | 공격·대기·이동·피격·사망 동작 |
| `Models/Animations/Controllers`, `Models/Animations/Sources` | Animator Controller와 애니메이션 원본 |
| `Models/Meshes`, `Models/Materials`, `Models/Textures` | 모델과 외형 리소스 |

## 사용 흐름과 확인

프리팹과 클립 선택 → Animator·Avatar 연결 → 씬에서 외형·동작 확인 순서로 사용한다. 공격 클립의 존재와 실제 피해 판정 구현은 구분한다.

전투에 투입할 때는 Controller, 런타임 유닛, 공격 이벤트·판정과 소환 설정을 연결한다. Unity에서 몸체 Collider와 공격 범위가 모델 크기에 맞는지도 확인한다.

관련 문서: [적 목록](../README.md).
