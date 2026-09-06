# Mummy Warrior 리소스

미라 전사 모델과 애니메이션을 보관한다. 현재 이 폴더에는 전용 C# 코드가 없다.

## 폴더 구성

| 폴더 | 설명 |
| --- | --- |
| `Models/Prefabs` | `MummyWarrior.prefab` |
| `Models/Animations/Clips`, `Models/Animations/Controllers` | 동작 클립과 Animator Controller |
| `Models/Animations/Sources` | 원본 애니메이션 |
| `Models/Animations/Archive/notUse` | 이전·미사용 동작 보관 |
| `Models/Meshes`, `Models/Materials`, `Models/Textures` | 모델과 외형 리소스 |

## 사용 흐름과 확인

프리팹·동작 선택 → Animator·Avatar 연결 확인 → 씬에서 외형과 동작 확인 순서다. Archive의 동작을 현재 전투에서 사용한다고 가정하지 않는다.

`Undead/Models/Prefabs`에도 `MummyWarriorRoot.prefab`이 있다. 이름이 비슷하므로 실제로 배치할 프리팹의 컴포넌트와 참조를 Unity Inspector에서 확인한다. 전투 AI와 소환 연결은 별도 작업이다.

관련 문서: [Undead](../Undead/README.md), [적 목록](../README.md).
