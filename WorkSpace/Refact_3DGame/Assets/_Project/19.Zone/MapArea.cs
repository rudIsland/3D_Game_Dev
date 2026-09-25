using UnityEngine;

namespace Zone
{
    // 오브젝트 이름과 별개로 지도에 표시할 이름, 실제 영역, 미니맵 도형을 연결한다.
    [DisallowMultipleComponent]
    public sealed class MapArea : MonoBehaviour
    {
        [InspectorName("표시 이름")]
        [SerializeField] private string displayName;
        [InspectorName("영역 콜라이더")]
        [SerializeField] private BoxCollider areaCollider;
        [InspectorName("미니맵 도형")]
        [SerializeField] private MeshFilter minimapShape;

        [Header("석상 근처 표시")]
        [InspectorName("석상 위치")]
        [SerializeField] private Transform statuePosition;
        [InspectorName("석상 표시 이름")]
        [SerializeField] private string statueDisplayName;
        [InspectorName("표시 반경 (m)"), Min(0f)]
        [SerializeField] private float statueDisplayRadius = 5f;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? "이름 없는 구역" : displayName;
        public BoxCollider AreaCollider => areaCollider;
        public MeshFilter MinimapShape => minimapShape;
        public Transform StatuePosition => statuePosition;
        public string StatueDisplayName => statueDisplayName;
        public float StatueDisplayRadius => statueDisplayRadius;

        private void Reset()
        {
            areaCollider = GetComponent<BoxCollider>();
        }
    }
}
