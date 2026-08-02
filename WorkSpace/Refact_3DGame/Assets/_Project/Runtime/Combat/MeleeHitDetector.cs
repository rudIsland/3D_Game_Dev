using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace rudIsland.RPG3D.Combat
{
    [DisallowMultipleComponent]
    // 공격이 활성화된 동안 현재 검날 범위에서 피격 대상을 찾는다.
    public sealed class MeleeHitDetector : MonoBehaviour
    {
        private const int MaxDetectedColliderCount = 32;
        private const float MinimumSweepDistance = 0.0001f;

        [Header("검 판정 위치")]
        [SerializeField] private Transform hitStart;
        [SerializeField] private Transform hitEnd;
        [SerializeField, Min(0.01f)] private float hitRadius = 0.12f;

        [Header("피격 대상")]
        [SerializeField] private LayerMask targetLayers;

        private readonly Collider[] detectedColliders =
            new Collider[MaxDetectedColliderCount];
        private readonly RaycastHit[] sweepHits =
            new RaycastHit[MaxDetectedColliderCount];
        private readonly HashSet<IAttackHitReceiver> hitTargets =
            new HashSet<IAttackHitReceiver>(16);
        private readonly Dictionary<Collider, IAttackHitReceiver> receiverCache =
            new Dictionary<Collider, IAttackHitReceiver>(32);

        private PhysicsScene physicsScene;
        private AttackHitData currentHit;
        private Vector3 previousHitStartPosition;
        private Vector3 previousHitMiddlePosition;
        private Vector3 previousHitEndPosition;
        private bool isHitActive;
        private bool hasPreviousBladePositions;

        internal bool IsHitActive => isHitActive;

        private void Awake()
        {
            physicsScene = gameObject.scene.GetPhysicsScene();
        }

        // 새로운 공격의 실제 타격 구간을 연다.
        public void StartHit(in AttackHitData hit)
        {
            if (!hit.Damage.IsValid)
            {
                EndHit();
                return;
            }

            if (hitStart == null || hitEnd == null)
            {
                Debug.LogError(
                    "MeleeHitDetector에 Hit Start와 Hit End가 필요합니다.",
                    this);
                return;
            }

            currentHit = hit;
            hitTargets.Clear();
            isHitActive = true;
            SaveCurrentBladePositions();

            DetectActiveHit();
        }

        // 현재 공격의 타격 구간을 닫는다.
        public void EndHit()
        {
            isHitActive = false;
            currentHit = default;
            previousHitStartPosition = default;
            previousHitMiddlePosition = default;
            previousHitEndPosition = default;
            hitTargets.Clear();
            hasPreviousBladePositions = false;
        }

        private void LateUpdate()
        {
            DetectActiveHit();
        }

        private void OnDisable()
        {
            EndHit();
        }

        // Unity 생명주기와 실제 검색을 분리하여 같은 흐름을 테스트할 수 있게 한다.
        internal void DetectActiveHit()
        {
            if (!isHitActive)
            {
                return;
            }

            DetectCurrentBlade();
            DetectMovedBlade();
        }

        private void DetectCurrentBlade()
        {
            int detectedCount = physicsScene.OverlapCapsule(
                hitStart.position,
                hitEnd.position,
                hitRadius,
                detectedColliders,
                targetLayers.value,
                QueryTriggerInteraction.Ignore);

            for (int index = 0; index < detectedCount; index++)
            {
                TryApplyHit(detectedColliders[index]);
            }
        }

        // 빠르게 움직인 검날의 시작·중간·끝이 지나간 범위를 찾는다.
        private void DetectMovedBlade()
        {
            Vector3 currentHitStartPosition = hitStart.position;
            Vector3 currentHitEndPosition = hitEnd.position;
            Vector3 currentHitMiddlePosition =
                GetMiddlePosition(
                    currentHitStartPosition,
                    currentHitEndPosition);

            if (!hasPreviousBladePositions)
            {
                SaveBladePositions(
                    currentHitStartPosition,
                    currentHitMiddlePosition,
                    currentHitEndPosition);
                return;
            }

            DetectMovedPoint(
                previousHitStartPosition,
                currentHitStartPosition);
            DetectMovedPoint(
                previousHitMiddlePosition,
                currentHitMiddlePosition);
            DetectMovedPoint(
                previousHitEndPosition,
                currentHitEndPosition);

            SaveBladePositions(
                currentHitStartPosition,
                currentHitMiddlePosition,
                currentHitEndPosition);
        }

        private void DetectMovedPoint(
            Vector3 previousPosition,
            Vector3 currentPosition)
        {
            Vector3 movement = currentPosition - previousPosition;
            float movementSqrMagnitude = movement.sqrMagnitude;

            if (movementSqrMagnitude <=
                MinimumSweepDistance * MinimumSweepDistance)
            {
                return;
            }

            float distance = Mathf.Sqrt(movementSqrMagnitude);
            Vector3 direction = movement / distance;
            int sweepHitCount = physicsScene.SphereCast(
                previousPosition,
                hitRadius,
                direction,
                sweepHits,
                distance,
                targetLayers.value,
                QueryTriggerInteraction.Ignore);

            for (int index = 0; index < sweepHitCount; index++)
            {
                TryApplyHit(sweepHits[index].collider);
            }
        }

        private void SaveCurrentBladePositions()
        {
            Vector3 currentHitStartPosition = hitStart.position;
            Vector3 currentHitEndPosition = hitEnd.position;

            SaveBladePositions(
                currentHitStartPosition,
                GetMiddlePosition(
                    currentHitStartPosition,
                    currentHitEndPosition),
                currentHitEndPosition);
        }

        private void SaveBladePositions(
            Vector3 startPosition,
            Vector3 middlePosition,
            Vector3 endPosition)
        {
            previousHitStartPosition = startPosition;
            previousHitMiddlePosition = middlePosition;
            previousHitEndPosition = endPosition;
            hasPreviousBladePositions = true;
        }

        private static Vector3 GetMiddlePosition(
            Vector3 startPosition,
            Vector3 endPosition)
        {
            return (startPosition + endPosition) * 0.5f;
        }

        private void TryApplyHit(Collider detectedCollider)
        {
            if (detectedCollider == null)
            {
                return;
            }

            IAttackHitReceiver receiver =
                FindHitReceiver(detectedCollider);

            if (receiver == null || !hitTargets.Add(receiver))
            {
                return;
            }

            receiver.ReceiveHit(in currentHit);
        }

        private IAttackHitReceiver FindHitReceiver(
            Collider detectedCollider)
        {
            if (receiverCache.TryGetValue(
                    detectedCollider,
                    out IAttackHitReceiver receiver))
            {
                return receiver;
            }

            receiver =
                detectedCollider.GetComponentInParent<IAttackHitReceiver>();
            receiverCache.Add(detectedCollider, receiver);
            return receiver;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            hitRadius = Mathf.Max(0.01f, hitRadius);
        }

        private void OnDrawGizmosSelected()
        {
            if (hitStart == null || hitEnd == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hitStart.position, hitRadius);
            Gizmos.DrawWireSphere(hitEnd.position, hitRadius);
            Gizmos.DrawLine(hitStart.position, hitEnd.position);
        }
#endif
    }
}
