# Cinemachine 카메라 구현 기록

- 작업 날짜: 2026-07-25
- 구현 기능: 플레이어 자유 시점 카메라와 카메라 충돌
- 개발 씬: `Assets/_Project/Scenes/Dev/CharacterTestScene.unity`

## 1. 구현 목표

플레이어 전신과 전방 전투 공간이 함께 보이는 3인칭 자유 시점 카메라를 구성한다. 벽이나 장애물이 플레이어와 카메라 사이에 들어오면 카메라 위치가 자동으로 보정되도록 한다.

## 2. 카메라 입력과 화면 출력 흐름

```text
마우스 입력
→ PlayerInput의 Look Action
→ Cinemachine Input Provider
→ Cinemachine FreeLook
→ CinemachineBrain
→ MainCamera 화면 출력
```

카메라는 `PlayerRoot/CameraTarget`을 따라가고 바라보도록 설정했다.

## 3. 자유 시점 카메라 설정

| 설정 | 값 |
|---|---:|
| CameraTarget 높이 | `1.5` |
| Field of View | `52` → `48` |
| 위쪽 궤도 높이 | `2.6` → `2.4` |
| 위쪽 궤도 거리 | `4.0` → `3.2` |
| 가운데 궤도 높이 | `1.4` → `1.25` |
| 가운데 궤도 거리 | `4.5` → `3.4` |
| 아래쪽 궤도 높이 | `0.5` → `0.45` |
| 아래쪽 궤도 거리 | `3.8` → `2.9` |
| 기본 Y축 위치 | `0.50` → `0.55` |
| 카메라 충돌 반지름 | `0.25` |
| 충돌 복귀 Damping | `0.15` |

## 4. 카메라 충돌

`Cinemachine Collider`를 연결하여 카메라와 플레이어 사이의 장애물을 확인하도록 구성했다.

```text
카메라와 플레이어 사이에 장애물 진입
→ Cinemachine Collider가 가림 상태 확인
→ 카메라를 플레이어 방향으로 이동
→ 장애물이 사라지면 원래 거리로 복귀
```

카메라가 벽이나 장애물 내부로 들어가는 현상을 줄이고, 추적 대상이 가려질 때 카메라 위치가 자동으로 보정되도록 했다.

## 5. 세로 시점 조정 검토

마우스를 위로 움직일 때 주요 시점이 지나치게 높아지는 경향을 확인했다.

설정별 역할:

- `Y Axis Max Speed`: 카메라가 위아래로 움직이는 속도
- `Top Rig Height`: 카메라가 도달하는 최고 시점
- `Y Axis Recentering`: 세로 입력이 없을 때 기본 시점으로 돌아오는 기능

검토한 변경값:

| 설정 | 초기값 → 검토값 |
|---|---:|
| Y Axis Max Speed | `1.1` → `0.65` |
| Top Rig Height | `2.4` → `1.7` |
| Top Rig Radius | `3.2` 유지 |
| Middle Rig Height | `1.25` → `0.8` |
| Middle Rig Radius | `3.4` 유지 |
| Bottom Rig Height | `0.45` → `0.2` |
| Bottom Rig Radius | `2.9` 유지 |
| Y Axis Recentering | 사용 안 함 → 사용 |
| Recentering Wait Time | 사용 안 함 → `0.5` |
| Recentering Time | 사용 안 함 → `1.2` |

한 번에 여러 값을 변경하면 원인을 구분하기 어렵기 때문에 다음 순서로 조정한다.

```text
Y Axis Max Speed를 0.65로 조정
→ 세로 반응 속도 확인
→ 시점이 여전히 높으면 궤도 높이 조정
→ 입력이 없을 때 복귀가 필요하면 Recentering 적용
```

## 6. Input Actions와 Cinemachine을 사용한 이유

### Input Actions

Input Actions는 키보드, 마우스와 게임패드 같은 물리 입력을 게임 안의 행동과 연결한다. Input Actions Editor에서 입력을 추가하거나 변경할 수 있어 카메라 코드가 특정 키나 장치에 직접 의존하지 않는다.

### Cinemachine Camera

Cinemachine은 부드러운 카메라 추적과 여러 대상 사이의 카메라 전환 기능을 제공한다. `Look` Action을 FreeLook Camera의 `Cinemachine Input Provider`에 있는 `XY Axis`에 연결하여 마우스 이동을 카메라 회전에 사용했다.

자유 시점, 타겟팅 시점 전환과 카메라 충돌을 직접 구현하면 입력, 회전과 충돌 보정 코드를 각각 관리해야 한다. Cinemachine을 사용하면 구현 범위를 줄일 수 있고, 이후 보스 등장이나 스토리 장면의 카메라 연출에도 활용할 수 있다.

## 7. 결과

- 플레이어 전신과 전방 전투 공간이 함께 보이는 가까운 3인칭 시점 구성
- 벽과 장애물 사이에서 카메라가 플레이어 쪽으로 당겨지도록 충돌 처리
- Unity 콘솔 오류와 씬의 누락 스크립트 없음

## 8. 다음 작업

1. 걷기와 달리기 입력 구현
2. 카메라 방향 기준 이동 구현
3. 이동 방향 회전과 중력 구현
