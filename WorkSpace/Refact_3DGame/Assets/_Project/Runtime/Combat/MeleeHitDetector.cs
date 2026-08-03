using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace rudIsland.RPG3D.Combat
{
    [DisallowMultipleComponent]
    // 공격이 활성화된 동안 현재 공격 모양에서 피격 대상을 찾는다.
    public sealed class MeleeHitDetector : MonoBehaviour
    {
        private const int MaxDetectedColliderCount = 32; // 개수 또는 크기
        private const float MinimumSweepDistance = 0.0001f; // 거리 설정

        [Header("공격 판정 모양")]
        [SerializeField] private AttackShape attackShape; // 공격 관련 설정 또는 상태

        [Header("피격 대상")]
        [SerializeField] private LayerMask targetLayers; // 대상 참조

        private readonly Collider[] detectedColliders = // 씬 또는 시스템 참조
            new Collider[MaxDetectedColliderCount];
        private readonly RaycastHit[] sweepHits = // 피격 또는 피해 관련 값
            new RaycastHit[MaxDetectedColliderCount];
        private readonly HashSet<IAttackHitReceiver> hitTargets = // 대상 참조
            new HashSet<IAttackHitReceiver>(16);
        private readonly Dictionary<Collider, DetectedHitTarget> hitTargetCache = // 대상 참조
            new Dictionary<Collider, DetectedHitTarget>(32);

        private PhysicsScene physicsScene; // 내부에서 사용하는 값
        private CombatHitResolver hitResolver; // 피격 또는 피해 관련 값
        private AttackHitInput currentHit; // 피격 또는 피해 관련 값
        private Vector3 previousShapeStartPosition; // 이동 정보
        private Vector3 previousShapeMiddlePosition; // 이동 정보
        private Vector3 previousShapeEndPosition; // 이동 정보
        private Quaternion previousShapeRotation; // 이동 정보
        private bool isHitActive; // 기능 사용 여부
        private bool hasPreviousShapePositions; // 기능 사용 여부
        private int currentAttackSequence; // 공격 관련 설정 또는 상태

        internal bool IsHitActive => isHitActive; // 기능 사용 여부
        public event Action<AttackHitResult, AttackHitInput> HitResultReady; // 피격 또는 피해 관련 값

        private void Awake()
        {
            physicsScene = gameObject.scene.GetPhysicsScene();
            FindHitResolver();
        }

        private void OnEnable()
        {
            if (hitResolver == null)
            {
                FindHitResolver();
            }
        }

        // 새로운 공격의 실제 타격 구간을 연다.
        public void StartHit(in AttackHitInput hit)
        {
            if (!hit.Damage.IsValid)
            {
                EndHit();
                return;
            }

            if (!attackShape.IsReady)
            {
                EndHit();
                Debug.LogError(
                    "MeleeHitDetector에 사용할 AttackShape 설정이 필요합니다.",
                    this);
                return;
            }

            currentHit = hit;
            IncreaseAttackSequence();
            hitTargets.Clear();
            isHitActive = true;
            SaveCurrentShapePositions();

            DetectActiveHit();
        }

        // 현재 공격의 타격 구간을 닫는다.
        public void EndHit()
        {
            isHitActive = false;
            currentHit = default;
            previousShapeStartPosition = default;
            previousShapeMiddlePosition = default;
            previousShapeEndPosition = default;
            previousShapeRotation = default;
            hitTargets.Clear();
            hasPreviousShapePositions = false;
        }

        private void LateUpdate()
        {
            DetectActiveHit();
        }

        private void OnDisable()
        {
            EndHit();
            IncreaseAttackSequence();
        }

        // Unity 생명주기와 실제 검색을 분리하여 같은 흐름을 테스트할 수 있게 한다.
        internal void DetectActiveHit()
        {
            if (!isHitActive)
            {
                return;
            }

            DetectCurrentShape();
            DetectMovedShape();
        }

        private void DetectCurrentShape()
        {
            int detectedCount;

            switch (attackShape.Type)
            {
                case AttackShapeType.Capsule:
                    detectedCount = physicsScene.OverlapCapsule(
                        attackShape.StartPosition,
                        attackShape.EndPosition,
                        attackShape.Radius,
                        detectedColliders,
                        targetLayers.value,
                        QueryTriggerInteraction.Collide);
                    break;
                case AttackShapeType.Sphere:
                    detectedCount = physicsScene.OverlapSphere(
                        attackShape.StartPosition,
                        attackShape.Radius,
                        detectedColliders,
                        targetLayers.value,
                        QueryTriggerInteraction.Collide);
                    break;
                case AttackShapeType.Box:
                    detectedCount = physicsScene.OverlapBox(
                        attackShape.StartPosition,
                        attackShape.BoxHalfSize,
                        detectedColliders,
                        attackShape.Rotation,
                        targetLayers.value,
                        QueryTriggerInteraction.Collide);
                    break;
                default:
                    return;
            }

            for (int index = 0; index < detectedCount; index++)
            {
                TryApplyCurrentShapeHit(detectedColliders[index]);
            }
        }

        // 빠르게 움직인 공격 모양이 프레임 사이에 지나간 범위를 찾는다.
        private void DetectMovedShape()
        {
            Vector3 currentStartPosition =
                attackShape.StartPosition;
            Vector3 currentMiddlePosition =
                attackShape.MiddlePosition;
            Vector3 currentEndPosition =
                attackShape.EndPosition;

            if (!hasPreviousShapePositions)
            {
                SaveShapePositions(
                    currentStartPosition,
                    currentMiddlePosition,
                    currentEndPosition);
                return;
            }

            switch (attackShape.Type)
            {
                case AttackShapeType.Capsule:
                    DetectMovedPoint(
                        previousShapeStartPosition,
                        currentStartPosition,
                        attackShape.Radius);
                    DetectMovedPoint(
                        previousShapeMiddlePosition,
                        currentMiddlePosition,
                        attackShape.Radius);
                    DetectMovedPoint(
                        previousShapeEndPosition,
                        currentEndPosition,
                        attackShape.Radius);
                    break;
                case AttackShapeType.Sphere:
                    DetectMovedPoint(
                        previousShapeStartPosition,
                        currentStartPosition,
                        attackShape.Radius);
                    break;
                case AttackShapeType.Box:
                    DetectMovedBox(
                        previousShapeStartPosition,
                        currentStartPosition);
                    break;
            }

            SaveShapePositions(
                currentStartPosition,
                currentMiddlePosition,
                currentEndPosition);
        }

        private void DetectMovedPoint(
            Vector3 previousPosition,
            Vector3 currentPosition,
            float radius)
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
                radius,
                direction,
                sweepHits,
                distance,
                targetLayers.value,
                QueryTriggerInteraction.Collide);

            for (int index = 0; index < sweepHitCount; index++)
            {
                RaycastHit sweepHit = sweepHits[index];
                TryApplyHit(
                    sweepHit.collider,
                    sweepHit.point,
                    sweepHit.normal,
                    direction);
            }
        }

        private void DetectMovedBox(
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
            int sweepHitCount = physicsScene.BoxCast(
                previousPosition,
                attackShape.BoxHalfSize,
                direction,
                sweepHits,
                previousShapeRotation,
                distance,
                targetLayers.value,
                QueryTriggerInteraction.Collide);

            for (int index = 0; index < sweepHitCount; index++)
            {
                RaycastHit sweepHit = sweepHits[index];
                TryApplyHit(
                    sweepHit.collider,
                    sweepHit.point,
                    sweepHit.normal,
                    direction);
            }
        }

        private void SaveCurrentShapePositions()
        {
            Vector3 currentStartPosition =
                attackShape.StartPosition;
            Vector3 currentEndPosition =
                attackShape.EndPosition;

            SaveShapePositions(
                currentStartPosition,
                attackShape.MiddlePosition,
                currentEndPosition);
        }

        private void SaveShapePositions(
            Vector3 startPosition,
            Vector3 middlePosition,
            Vector3 endPosition)
        {
            previousShapeStartPosition = startPosition;
            previousShapeMiddlePosition = middlePosition;
            previousShapeEndPosition = endPosition;
            previousShapeRotation = attackShape.Rotation;
            hasPreviousShapePositions = true;
        }

        // 현재 공격 모양과 대상 사이에서 가장 가까운 접촉 정보를 계산한다.
        private void TryApplyCurrentShapeHit(Collider detectedCollider)
        {
            if (detectedCollider == null)
            {
                return;
            }

            Vector3 shapePoint = GetClosestPointOnShape(
                detectedCollider.bounds.center);
            Vector3 hitPoint = detectedCollider.ClosestPoint(shapePoint);
            Vector3 hitDirection = GetHitDirection(
                shapePoint,
                detectedCollider.bounds.center);

            TryApplyHit(
                detectedCollider,
                hitPoint,
                -hitDirection,
                hitDirection);
        }

        private Vector3 GetClosestPointOnShape(
            Vector3 targetPosition)
        {
            switch (attackShape.Type)
            {
                case AttackShapeType.Capsule:
                    return GetClosestPointOnLine(
                        attackShape.StartPosition,
                        attackShape.EndPosition,
                        targetPosition);
                case AttackShapeType.Box:
                    return GetClosestPointOnBox(
                        attackShape.StartPosition,
                        attackShape.Rotation,
                        attackShape.BoxHalfSize,
                        targetPosition);
                default:
                    return attackShape.StartPosition;
            }
        }

        private static Vector3 GetClosestPointOnLine(
            Vector3 lineStart,
            Vector3 lineEnd,
            Vector3 targetPosition)
        {
            Vector3 line = lineEnd - lineStart;
            float lineLengthSqr = line.sqrMagnitude;

            if (lineLengthSqr <=
                MinimumSweepDistance * MinimumSweepDistance)
            {
                return lineStart;
            }

            float distanceRate = Mathf.Clamp01(
                Vector3.Dot(targetPosition - lineStart, line) /
                lineLengthSqr);
            return lineStart + line * distanceRate;
        }

        private static Vector3 GetClosestPointOnBox(
            Vector3 boxCenter,
            Quaternion boxRotation,
            Vector3 boxHalfSize,
            Vector3 targetPosition)
        {
            Vector3 localTarget =
                Quaternion.Inverse(boxRotation) *
                (targetPosition - boxCenter);
            localTarget.x = Mathf.Clamp(
                localTarget.x,
                -boxHalfSize.x,
                boxHalfSize.x);
            localTarget.y = Mathf.Clamp(
                localTarget.y,
                -boxHalfSize.y,
                boxHalfSize.y);
            localTarget.z = Mathf.Clamp(
                localTarget.z,
                -boxHalfSize.z,
                boxHalfSize.z);

            return boxCenter + boxRotation * localTarget;
        }

        private Vector3 GetHitDirection(
            Vector3 shapePoint,
            Vector3 targetPosition)
        {
            Vector3 hitDirection = targetPosition - shapePoint;

            if (hitDirection.sqrMagnitude <=
                MinimumSweepDistance * MinimumSweepDistance)
            {
                return transform.forward;
            }

            return hitDirection.normalized;
        }

        private void TryApplyHit(
            Collider detectedCollider,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 hitDirection)
        {
            if (detectedCollider == null)
            {
                return;
            }

            DetectedHitTarget hitTarget =
                FindHitTarget(detectedCollider);

            if (!hitTarget.IsValid ||
                !hitTargets.Add(hitTarget.Receiver))
            {
                return;
            }

            var contact = new HitContact(
                hitPoint,
                hitNormal,
                hitDirection,
                hitTarget.BodyPart);
            AttackHitInput hitWithContact =
                currentHit.CreateWithHitContact(in contact);

            if (hitResolver == null)
            {
                FindHitResolver();
            }

            if (hitResolver == null ||
                !hitResolver.QueueHit(
                    this,
                    currentAttackSequence,
                    hitTarget.Receiver,
                    in hitWithContact))
            {
                hitTargets.Remove(hitTarget.Receiver);
            }
        }

        internal void NotifyHitResolved(
            AttackHitResult hitResult,
            in AttackHitInput hit)
        {
            HitResultReady?.Invoke(hitResult, hit);
        }

        internal bool MatchesAttackSequence(int attackSequence)
        {
            return currentAttackSequence == 0 ||
                currentAttackSequence == attackSequence;
        }

        private void IncreaseAttackSequence()
        {
            currentAttackSequence =
                currentAttackSequence == int.MaxValue
                    ? 1
                    : currentAttackSequence + 1;
        }

        private void FindHitResolver()
        {
            CombatHitResolver parentResolver =
                GetComponentInParent<CombatHitResolver>(true);
            if (parentResolver != null &&
                parentResolver.gameObject.scene == gameObject.scene)
            {
                hitResolver = parentResolver;
                return;
            }

            CombatHitResolver[] foundResolvers =
                FindObjectsByType<CombatHitResolver>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int index = 0;
                index < foundResolvers.Length;
                index++)
            {
                CombatHitResolver foundResolver =
                    foundResolvers[index];
                if (foundResolver.gameObject.scene == gameObject.scene)
                {
                    hitResolver = foundResolver;
                    return;
                }
            }

            hitResolver = null;

            if (hitResolver == null)
            {
                Debug.LogError(
                    "MeleeHitDetector와 같은 Scene에 CombatHitResolver가 필요합니다.",
                    this);
            }
        }

        private DetectedHitTarget FindHitTarget(
            Collider detectedCollider)
        {
            if (hitTargetCache.TryGetValue(
                    detectedCollider,
                    out DetectedHitTarget hitTarget))
            {
                return hitTarget;
            }

            UnitHitBox unitHitBox =
                detectedCollider.GetComponent<UnitHitBox>();
            if (unitHitBox != null &&
                unitHitBox.TryGetHitReceiver(
                    out IAttackHitReceiver hitBoxReceiver))
            {
                hitTarget = new DetectedHitTarget(
                    hitBoxReceiver,
                    unitHitBox.BodyPart);
                hitTargetCache.Add(detectedCollider, hitTarget);
                return hitTarget;
            }

            IAttackHitReceiver legacyReceiver =
                detectedCollider.GetComponentInParent<IAttackHitReceiver>();
            if (legacyReceiver == null ||
                HasUnitHitBox(legacyReceiver))
            {
                hitTargetCache.Add(
                    detectedCollider,
                    DetectedHitTarget.Invalid);
                return DetectedHitTarget.Invalid;
            }

            hitTarget = new DetectedHitTarget(
                legacyReceiver,
                HitBodyPart.Body);
            hitTargetCache.Add(detectedCollider, hitTarget);
            return hitTarget;
        }

        private static bool HasUnitHitBox(
            IAttackHitReceiver receiver)
        {
            Component receiverComponent = receiver as Component;
            return receiverComponent != null &&
                receiverComponent.GetComponentInChildren<UnitHitBox>(
                    true) != null;
        }

        private readonly struct DetectedHitTarget
        {
            internal static readonly DetectedHitTarget Invalid = default; // 내부에서 사용하는 값

            internal IAttackHitReceiver Receiver { get; } // 외부에 제공하는 읽기 값
            internal HitBodyPart BodyPart { get; } // 외부에 제공하는 읽기 값
            internal bool IsValid => Receiver != null; // 기능 사용 여부

            internal DetectedHitTarget(
                IAttackHitReceiver receiver,
                HitBodyPart bodyPart)
            {
                Receiver = receiver;
                BodyPart = bodyPart;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!attackShape.IsReady)
            {
                return;
            }

            Gizmos.color = Color.red;
            switch (attackShape.Type)
            {
                case AttackShapeType.Capsule:
                    Gizmos.DrawWireSphere(
                        attackShape.StartPosition,
                        attackShape.Radius);
                    Gizmos.DrawWireSphere(
                        attackShape.EndPosition,
                        attackShape.Radius);
                    Gizmos.DrawLine(
                        attackShape.StartPosition,
                        attackShape.EndPosition);
                    break;
                case AttackShapeType.Sphere:
                    Gizmos.DrawWireSphere(
                        attackShape.StartPosition,
                        attackShape.Radius);
                    break;
                case AttackShapeType.Box:
                    Matrix4x4 previousMatrix = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(
                        attackShape.StartPosition,
                        attackShape.Rotation,
                        Vector3.one);
                    Gizmos.DrawWireCube(
                        Vector3.zero,
                        attackShape.BoxSize);
                    Gizmos.matrix = previousMatrix;
                    break;
            }
        }
#endif
    }
}
