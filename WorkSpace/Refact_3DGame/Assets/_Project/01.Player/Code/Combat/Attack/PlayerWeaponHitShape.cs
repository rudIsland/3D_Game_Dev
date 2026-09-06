using UnityEngine;

namespace Characters.Player.Combat.Attack
{
    [DisallowMultipleComponent]
    // 플레이어 검의 Capsule 판정점, 반지름과 공격 대상을 소유한다.
    public sealed class PlayerWeaponHitShape : MonoBehaviour
    {
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField, Min(0.01f)] private float radius = 0.12f;

        internal Transform StartPoint => startPoint;
        internal Transform EndPoint => endPoint;
        internal LayerMask TargetLayers => targetLayers;
        internal float Radius => radius;
        internal bool IsReady =>
            startPoint != null &&
            endPoint != null &&
            targetLayers.value != 0 &&
            radius > 0f;

#if UNITY_EDITOR
        internal void ConnectForEditor(
            Transform weaponStartPoint,
            Transform weaponEndPoint,
            LayerMask attackTargetLayers,
            float weaponRadius)
        {
            startPoint = weaponStartPoint;
            endPoint = weaponEndPoint;
            targetLayers = attackTargetLayers;
            radius = Mathf.Max(0.01f, weaponRadius);
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0.01f, radius);
        }

        private void OnDrawGizmosSelected()
        {
            if (startPoint == null || endPoint == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(startPoint.position, radius);
            Gizmos.DrawWireSphere(endPoint.position, radius);
            Gizmos.DrawLine(startPoint.position, endPoint.position);
        }
#endif
    }
}
