# 아이템

아이템 정의, 씬 배치와 플레이어의 획득 요청을 처리한다. 가방 슬롯과 보관 규칙은 `01.Player/Code/Inventory`가 담당한다.

## 폴더 구성과 주요 파일

| 위치 | 설명 |
| --- | --- |
| `Code/ItemDefinition.cs` | 표시 이름, 아이콘과 최대 묶음 수를 가진 ScriptableObject |
| `Code/ItemType.cs`, `Code/ItemCatalog.cs` | 아이템 종류와 정의·월드 프리팹의 연결 |
| `Code/ItemSpawnPoint.cs` | 해당 위치에 생성할 아이템 종류 |
| `Code/ItemSpawnManager.cs` | 시작 시 자식 소환 지점을 읽어 프리팹 생성 |
| `Code/WorldItemPickup.cs` | 획득 가능 여부와 상호작용 처리 |
| `Code/InventoryItem.cs` | 별도로 존재하는 획득용 컴포넌트. 가방 자료구조가 아님 |
| `Prefab` | Book·Scroll 프리팹, 아이템 정의와 ItemCatalog 에셋 |

## 생성과 획득 흐름

1. `ItemSpawnManager.Awake`가 `SpawnItems`를 호출한다.
2. 소환 지점의 ItemType으로 Catalog에서 정의와 프리팹을 찾는다.
3. 소환 지점의 자식으로 프리팹을 생성하고 `WorldItemPickup`에 아이템 정의를 전달한다.
4. 플레이어 상호작용이 보관 가능 여부를 확인하고 인벤토리 추가를 요청한다.
5. 추가에 성공하면 획득 오브젝트를 비활성화한다. 가방이 가득 차면 획득하지 않는다. 씬을 다시 불러오면 원래 배치대로 생성한다.

현재 ItemSpawnManager는 `Instantiate`를 직접 사용한다. WorldObjectManager 풀을 사용하거나 시간에 따라 재생성하는 구조는 아니다.

## Unity에서 확인

- ItemSpawnManager의 Catalog와 자식 SpawnPoint의 ItemType을 확인한다.
- Catalog의 ItemDefinition과 WorldItemPrefab 연결을 확인한다.
- 획득 대상 Collider·레이어와 플레이어 상호작용 탐지 설정을 확인한다.
- 가방에 여유가 있을 때와 가득 찼을 때 안내·획득 결과를 확인한다.
- `InventoryItem`의 직렬화 필드 `count`는 현재 TryInteract의 보관 요청에 사용되지 않는다. 수량 획득이 구현되어 있다고 가정하지 않는다.

관련 문서: [플레이어](../01.Player/README.md), [교환·상호작용](../00.Core/WorldObjects/README.md).
