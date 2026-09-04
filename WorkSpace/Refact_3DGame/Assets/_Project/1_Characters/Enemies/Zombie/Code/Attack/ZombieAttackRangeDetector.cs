using Characters.Combat.AttackData;
using Characters.Combat;
using Characters.Player.Combat.Hit;
using UnityEngine;

namespace Characters.Enemies.Zombie
{
    // 공격창 동안 선택된 손·발 Capsule과 프레임 사이 궤적을 검사한다.
    internal sealed class ZombieAttackRangeDetector
    {
        private const int MaximumDetectedColliderCount = 16;
        private const float MinimumSweepDistance = 0.0001f;

        private readonly Transform attackerRoot;
        private readonly LayerMask targetLayers;
        private readonly ZombieAttackHitShape swingHitShape;
        private readonly ZombieAttackHitShape kickHitShape;
        private readonly ZombieAttackHitShape upDownHitShape;
        private readonly CombatHitStop attackerHitStop;
        private readonly CombatHitEffectPlayer hitEffectPlayer;
        private readonly Collider[] detectedColliders =
            new Collider[MaximumDetectedColliderCount];
        private readonly RaycastHit[] sweepHits =
            new RaycastHit[MaximumDetectedColliderCount];

        private AttackDamage attackDamage;
        private ZombieAttackHitShape currentHitShape;
        private bool isWindowOpen;
        private bool hasHitTarget;
        private bool hasPreviousShapePositions;
        private Vector3 previousStartPosition;
        private Vector3 previousMiddlePosition;
        private Vector3 previousEndPosition;
        private PendingPlayerContact pendingBodyContact;
        private PendingPlayerContact pendingGuardContact;

        public ZombieAttackRangeDetector(
            Transform attackerRoot,
            LayerMask targetLayers,
            ZombieAttackHitShape swingHitShape,
            ZombieAttackHitShape kickHitShape,
            ZombieAttackHitShape upDownHitShape,
            CombatHitStop attackerHitStop,
            CombatHitEffectPlayer hitEffectPlayer)
        {
            this.attackerRoot = attackerRoot;
            this.targetLayers = targetLayers;
            this.swingHitShape = swingHitShape;
            this.kickHitShape = kickHitShape;
            this.upDownHitShape = upDownHitShape;
            this.attackerHitStop = attackerHitStop;
            this.hitEffectPlayer = hitEffectPlayer;
        }

        public void Open(int attackNumber, AttackDamage damage)
        {
            attackDamage = damage;
            currentHitShape = GetHitShape(attackNumber);
            isWindowOpen =
                damage != null &&
                currentHitShape != null &&
                currentHitShape.IsReady;
            hasHitTarget = false;
            SaveCurrentShapePositions();
        }

        public void Tick()
        {
            if (!isWindowOpen ||
                hasHitTarget ||
                attackerRoot == null ||
                currentHitShape == null ||
                !currentHitShape.IsReady ||
                targetLayers.value == 0 ||
                attackDamage == null)
            {
                return;
            }

            Vector3 currentStartPosition =
                currentHitShape.StartPoint.position;
            Vector3 currentEndPosition =
                currentHitShape.EndPoint.position;
            Vector3 currentMiddlePosition =
                (currentStartPosition + currentEndPosition) * 0.5f;

            int detectedCount = Physics.OverlapCapsuleNonAlloc(
                currentStartPosition,
                currentEndPosition,
                currentHitShape.Radius,
                detectedColliders,
                targetLayers,
                QueryTriggerInteraction.Collide);

            ClearPendingContacts();
            CollectCurrentHits(
                detectedCount,
                currentStartPosition,
                currentEndPosition);

            if (hasPreviousShapePositions)
            {
                CollectMovedPoint(previousStartPosition, currentStartPosition);
                CollectMovedPoint(previousMiddlePosition, currentMiddlePosition);
                CollectMovedPoint(previousEndPosition, currentEndPosition);
            }

            if (TryApplyPendingContact())
            {
                return;
            }

            SaveShapePositions(
                currentStartPosition,
                currentMiddlePosition,
                currentEndPosition);
        }

        public void Close()
        {
            isWindowOpen = false;
            hasHitTarget = false;
            hasPreviousShapePositions = false;
            attackDamage = null;
            currentHitShape = null;
            ClearPendingContacts();
        }

        private void CollectCurrentHits(
            int detectedCount,
            Vector3 startPosition,
            Vector3 endPosition)
        {
            for (int index = 0; index < detectedCount; index++)
            {
                Collider detectedCollider = detectedColliders[index];
                if (detectedCollider == null)
                {
                    continue;
                }

                Vector3 shapePoint = GetClosestPointOnLine(
                    startPosition,
                    endPosition,
                    detectedCollider.bounds.center);
                Vector3 hitPosition =
                    detectedCollider.ClosestPoint(shapePoint);
                CollectContact(
                    detectedCollider,
                    hitPosition,
                    1f);
            }
        }

        private void CollectMovedPoint(Vector3 previousPosition, Vector3 currentPosition)
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
            int detectedCount = Physics.SphereCastNonAlloc(
                previousPosition,
                currentHitShape.Radius,
                direction,
                sweepHits,
                distance,
                targetLayers,
                QueryTriggerInteraction.Collide);

            for (int index = 0; index < detectedCount; index++)
            {
                RaycastHit sweepHit = sweepHits[index];
                if (sweepHit.collider == null)
                {
                    continue;
                }

                CollectContact(
                    sweepHit.collider,
                    sweepHit.point,
                    Mathf.Clamp01(sweepHit.distance / distance));
            }
        }

        private void CollectContact(
            Collider detectedCollider,
            Vector3 hitPosition,
            float sweepProgress)
        {
            IPlayerDamageReceiver target =
                detectedCollider.GetComponentInParent<IPlayerDamageReceiver>();
            if (target == null)
            {
                return;
            }

            if (detectedCollider.GetComponent<
                    PlayerGuardHitBox>() != null)
            {
                if (!pendingGuardContact.IsValid ||
                    sweepProgress < pendingGuardContact.SweepProgress)
                {
                    pendingGuardContact.Set(
                        target,
                        hitPosition,
                        sweepProgress);
                }

                return;
            }

            if (!pendingBodyContact.IsValid ||
                sweepProgress < pendingBodyContact.SweepProgress)
            {
                pendingBodyContact.Set(
                    target,
                    hitPosition,
                    sweepProgress);
            }
        }

        private bool TryApplyPendingContact()
        {
            bool hasSameTargetGuardContact =
                pendingGuardContact.IsValid &&
                pendingBodyContact.IsValid &&
                ReferenceEquals(pendingGuardContact.Target, pendingBodyContact.Target);
            bool tryGuardFirst = hasSameTargetGuardContact ||
                (pendingGuardContact.IsValid &&
                (!pendingBodyContact.IsValid ||
                 pendingGuardContact.SweepProgress <=
                 pendingBodyContact.SweepProgress));

            if (tryGuardFirst)
            {
                return TryApplyContact(in pendingGuardContact, PlayerHitSurface.Guard) ||
                    TryApplyContact(in pendingBodyContact, PlayerHitSurface.Body);
            }

            return TryApplyContact(in pendingBodyContact, PlayerHitSurface.Body) ||
                TryApplyContact(in pendingGuardContact, PlayerHitSurface.Guard);
        }

        private bool TryApplyContact(in PendingPlayerContact contact, PlayerHitSurface hitSurface)
        {
            if (!contact.IsValid)
            {
                return false;
            }

            Vector3 pushDirection =
                contact.HitPosition - attackerRoot.position;
            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude <= 0.000001f)
            {
                pushDirection = attackerRoot.forward;
            }

            var hitRequest = new PlayerHitRequest(
                attackDamage,
                contact.HitPosition,
                pushDirection,
                hitSurface);
            PlayerHitResult hitResult =
                contact.Target.TryTakeHit(in hitRequest);
            if (hitResult == PlayerHitResult.Ignored)
            {
                return false;
            }

            if (hitResult == PlayerHitResult.Blocked)
            {
                attackerHitStop?.Request(CombatHitStop.GuardDuration);
                hitEffectPlayer?.PlayGuardHit(hitRequest.HitPosition, hitRequest.PushDirection);
            }
            else if (hitResult != PlayerHitResult.Avoided)
            {
                attackerHitStop?.Request(attackDamage.HitStopDuration);
                hitEffectPlayer?.PlayBodyHit(hitRequest.HitPosition, hitRequest.PushDirection);
            }

            hasHitTarget = true;
            return true;
        }

        private void ClearPendingContacts()
        {
            pendingBodyContact.Clear();
            pendingGuardContact.Clear();
        }

        private ZombieAttackHitShape GetHitShape(int attackNumber)
        {
            switch (attackNumber)
            {
                case 1:
                    return swingHitShape;
                case 2:
                    return kickHitShape;
                case 3:
                    return upDownHitShape;
                default:
                    return null;
            }
        }

        private void SaveCurrentShapePositions()
        {
            if (currentHitShape == null || !currentHitShape.IsReady)
            {
                hasPreviousShapePositions = false;
                return;
            }

            Vector3 startPosition =
                currentHitShape.StartPoint.position;
            Vector3 endPosition =
                currentHitShape.EndPoint.position;
            SaveShapePositions(
                startPosition,
                (startPosition + endPosition) * 0.5f,
                endPosition);
        }

        private void SaveShapePositions(
            Vector3 startPosition,
            Vector3 middlePosition,
            Vector3 endPosition)
        {
            previousStartPosition = startPosition;
            previousMiddlePosition = middlePosition;
            previousEndPosition = endPosition;
            hasPreviousShapePositions = true;
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

            float distanceRate = Mathf.Clamp01(Vector3.Dot(targetPosition - lineStart, line) / lineLengthSqr);
            return lineStart + line * distanceRate;
        }

        private struct PendingPlayerContact
        {
            public IPlayerDamageReceiver Target { get; private set; }
            public Vector3 HitPosition { get; private set; }
            public float SweepProgress { get; private set; }
            public bool IsValid => Target != null;

            public void Set(
                IPlayerDamageReceiver target,
                Vector3 hitPosition,
                float sweepProgress)
            {
                Target = target;
                HitPosition = hitPosition;
                SweepProgress = sweepProgress;
            }

            public void Clear()
            {
                Target = null;
                HitPosition = Vector3.zero;
                SweepProgress = 0f;
            }
        }
    }
}
