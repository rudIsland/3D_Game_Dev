# 맵 교체 관리

현재 MapManager는 제거했다. 05.Manager 폴더는 추후 실행 순서를 연결할 자리로 유지한다.

맵 씬은 [MapContainer](../04.Loading/MapContainer.cs)가 cache와 refCount로 관리한다. 자동 교체·시간 정지·첫 맵 요청은 없다. 호출 방식은 [04.Loading 안내](../04.Loading/README.md)를 따른다.
