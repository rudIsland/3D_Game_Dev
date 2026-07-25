# 캐릭터 개발 기록

캐릭터와 전투 기능의 작업 내용을 날짜별로 기록한다.  
같은 날짜의 작업은 해당 날짜 아래에 이어서 추가한다.

## 2026-07-25

### 인게임 맵 설정
- 캐릭터와 충돌되지 않는 오브젝트들에 대해 Collider를 AI MCP를 통해 삽입하여 반복 작업은 AI에게 수행

### 캐릭터 개발용 씬 구성

- 개발 씬: `Assets/_Project/Scenes/Dev/CharacterTestScene.unity`
- 플레이어 모델과 `CharacterController` 배치
- Zombie, Mutant, Fighter 모델과 `CapsuleCollider` 배치
- 이동 검증용 평지, 경사로, 계단, 점프 장애물, 충돌 통로 구성
- 기존 `GameScene`과 원본 맵은 수정하지 않음

### Cinemachine 자유 시점 카메라 구성

카메라 동작 흐름:

```text
마우스 입력
→ PlayerInput의 Look 액션
→ Cinemachine Input Provider
→ Cinemachine FreeLook
→ CinemachineBrain
→ MainCamera 화면 출력
```

카메라는 `PlayerRoot/CameraTarget`을 따라가고 바라보도록 설정했다.

| 설정 | 값 |
|---|---:|
| CameraTarget 높이 | 1.5 |
| Field of View | 52 → 48 |
| 위쪽 궤도 높이 | 2.6 → 2.4 |
| 위쪽 궤도 거리 | 4.0 → 3.2 |
| 가운데 궤도 높이 | 1.4 → 1.25 |
| 가운데 궤도 거리 | 4.5 → 3.4 |
| 아래쪽 궤도 높이 | 0.5 → 0.45 |
| 아래쪽 궤도 거리 | 3.8 → 2.9 |
| 기본 Y축 위치 | 0.50 → 0.55 |
| 카메라 충돌 반지름 | 0.25 |
| 충돌 복귀 Damping | 0.15 |

#### 결과

- 플레이어 전신과 전방 전투 공간이 함께 보이는 가까운 3인칭 시점 구성
- 벽과 장애물 사이에서 카메라가 플레이어 쪽으로 당겨지도록 충돌 처리
- Unity 콘솔 오류와 씬의 누락 스크립트 없음

### Cinemachine 세로 시점 조정 검토

마우스를 위로 움직일 때 주요 시점이 지나치게 높아지는 경향을 확인했다.

설정별 역할:

- `Y Axis Max Speed`: 카메라가 위아래로 움직이는 속도
- `Top Rig Height`: 카메라가 도달하는 최고 시점
- `Y Axis Recentering`: 세로 입력이 없을 때 기본 시점으로 돌아오는 기능

초기 설정값에서 변경값:

| 설정 | 초기설정값 → 변경값 |
|---|---:|
| Y Axis Max Speed | 1.1 → 0.65 |
| Top Rig Height | 2.4 → 1.7 |
| Top Rig Radius | 3.2 유지 |
| Middle Rig Height | 1.25 → 0.8 |
| Middle Rig Radius | 3.4 유지 |
| Bottom Rig Height | 0.45 → 0.2 |
| Bottom Rig Radius | 2.9 유지 |
| Y Axis Recentering | 사용 안 함 → 사용 |
| Recentering Wait Time | 사용 안 함 → 0.5 |
| Recentering Time | 사용 안 함 → 1.2 |

우선 `Y Axis Max Speed`만 `0.65`로 낮춰 반응 속도를 확인하고, 시점 자체가 여전히 높으면 궤도 높이와 Recentering을 순서대로 조정한다.

### Cinemachine Camera와 Input Actions를 사용한 이유

1. Input Actions는 키보드, 마우스, 게임패드 같은 물리 입력을 게임 안의 행동과 연결한다. 전용 Input Actions Editor에서 GUI로 설정할 수 있어 사용하기 쉽고, 키를 추가하거나 변경하기도 편하다.

2. Cinemachine Camera는 부드러운 카메라 움직임과 여러 대상 사이의 카메라 전환 기능을 제공한다. Input Actions에서 만든 `Look` 액션을 FreeLook Camera의 `Cinemachine Input Provider`에 있는 `XY Axis`에 연결하면 마우스 움직임을 카메라 회전으로 바로 사용할 수 있다. 두 기능을 연결하는 과정도 간단하다.

3. `Cinemachine Collider`를 사용하면 카메라가 벽이나 장애물 안으로 들어가는 현상을 막을 수 있다. 추적 대상이 장애물에 가려졌을 때 카메라 위치를 보정하는 기능도 제공하며, 필요한 기능은 확장 컴포넌트로 추가할 수 있다.

4. 자유 시점, 타겟팅 시점 전환, 카메라 충돌을 직접 구현하려면 입력과 회전, 충돌 보정 코드를 각각 만들어야 한다. Input Actions와 Cinemachine을 사용하면 이 작업 시간을 줄일 수 있다. 이후 보스 몬스터 등장이나 스토리 장면처럼 카메라 연출이 필요한 기능에도 활용하기 좋다고 판단했다.

#### 다음 작업

- 걷기와 달리기 입력
- 카메라 방향 기준 이동
- 회전, 점프, 중력 구현