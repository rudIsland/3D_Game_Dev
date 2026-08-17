using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Combat.AttackData;
using rudIsland.RPG3D.Player.Runtime.Hit;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 공격 구간 동안 RustySword 검날과 프레임 사이 이동 경로를 검사한다.
    internal sealed class NightShadeSwordAttackRangeDetector
    {
        private const int MaximumDetectedColliderCount = 16;
        private const float MinimumSweepDistance = 0.0001f;

        private readonly Transform attackerRoot;
        private readonly LayerMask targetLayers;
        private readonly NightShadeSwordHitShape swordHitShape;
        private readonly CombatHitStop attackerHitStop;
        private readonly CombatHitEffectPlayer hitEffectPlayer;
        private readonly Collider[] detectedColliders =
            new Collider[MaximumDetectedColliderCount];
        private readonly RaycastHit[] sweepHits =
            new RaycastHit[MaximumDetectedColliderCount];

        private AttackDamage attackDamage;
        private bool isWindowOpen;
        private bool hasHitTarget;
        private bool hasPreviousShapePositions;
        private Vector3 previousStartPosition;
        private Vector3 previousMiddlePosition;
        private Vector3 previousEndPosition;
        private PendingPlayerContact pendingBodyContact;
        private PendingPlayerContact pendingGuardContact;

        internal NightShadeSwordAttackRangeDetector(
            Transform attackerRoot,
            LayerMask targetLayers,
            NightShadeSwordHitShape swordHitShape,
            CombatHitStop attackerHitStop,
            CombatHitEffectPlayer hitEffectPlayer)
        {
            this.attackerRoot = attackerRoot;
            this.targetLayers = targetLayers;
            this.swordHitShape = swordHitShape;
            this.attackerHitStop = attackerHitStop;
            this.hitEffectPlayer = hitEffectPlayer;
        }

        internal void Open(AttackDamage damage)
        {
            attackDamage = damage;
            isWindowOpen =
                damage != null && swordHitShape != null && swordHitShape.IsReady;
            hasHitTarget = false;
            SaveCurrentShapePositions();
        }

        internal void Tick()
        {
            if (!isWindowOpen ||
                hasHitTarget ||
                attackerRoot == null ||
                swordHitShape == null ||
                !swordHitShape.IsReady ||
                targetLayers.value == 0 ||
                attackDamage == null)
            {
                return;
            }

            Vector3 currentStartPosition =
                swordHitShape.StartPoint.position;
            Vector3 currentEndPosition =
                swordHitShape.EndPoint.position;
            Vector3 currentMiddlePosition =
                (currentStartPosition + currentEndPosition) * 0.5f;

            int detectedCount = Physics.OverlapCapsuleNonAlloc(
                currentStartPosition,
                currentEndPosition,
                swordHitShape.Radius,
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

        internal void Close()
        {
            isWindowOpen = false;
            hasHitTarget = false;
            hasPreviousShapePositions = false;
            attackDamage = null;
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
                CollectContact(
                    detectedCollider,
                    detectedCollider.ClosestPoint(shapePoint),
                    1f);
            }
        }

        private void CollectMovedPoint(
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
            int detectedCount = Physics.SphereCastNonAlloc(
                previousPosition,
                swordHitShape.Radius,
                movement / distance,
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

            if (detectedCollider.GetComponent<PlayerGuardHitBox>() != null)
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
                pendingBodyContact.Set(target, hitPosition, sweepProgress);
            }
        }

        private bool TryApplyPendingContact()
        {
            bool hasSameTargetGuardContact =
                pendingGuardContact.IsValid &&
                pendingBodyContact.IsValid &&
                ReferenceEquals(
                    pendingGuardContact.Target,
                    pendingBodyContact.Target);
            bool tryGuardFirst = hasSameTargetGuardContact ||
                (pendingGuardContact.IsValid &&
                 (!pendingBodyContact.IsValid ||
                  pendingGuardContact.SweepProgress <=
                  pendingBodyContact.SweepProgress));

            if (tryGuardFirst)
            {
                return TryApplyContact(
                        in pendingGuardContact,
                        PlayerHitSurface.Guard) ||
                    TryApplyContact(
                        in pendingBodyContact,
                        PlayerHitSurface.Body);
            }

            return TryApplyContact(
                    in pendingBodyContact,
                    PlayerHitSurface.Body) ||
                TryApplyContact(
                    in pendingGuardContact,
                    PlayerHitSurface.Guard);
        }

        private bool TryApplyContact(
            in PendingPlayerContact contact,
            PlayerHitSurface hitSurface)
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
                hitEffectPlayer?.PlayGuardHit(
                    hitRequest.HitPosition,
                    hitRequest.PushDirection);
            }
            else if (hitResult != PlayerHitResult.Avoided)
            {
                attackerHitStop?.Request(attackDamage.HitStopDuration);
                hitEffectPlayer?.PlayBodyHit(
                    hitRequest.HitPosition,
                    hitRequest.PushDirection);
            }

            hasHitTarget = true;
            return true;
        }

        private void SaveCurrentShapePositions()
        {
            if (swordHitShape == null || !swordHitShape.IsReady)
            {
                hasPreviousShapePositions = false;
                return;
            }

            Vector3 startPosition = swordHitShape.StartPoint.position;
            Vector3 endPosition = swordHitShape.EndPoint.position;
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

        private void ClearPendingContacts()
        {
            pendingBodyContact.Clear();
            pendingGuardContact.Clear();
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

        private struct PendingPlayerContact
        {
            internal IPlayerDamageReceiver Target { get; private set; }
            internal Vector3 HitPosition { get; private set; }
            internal float SweepProgress { get; private set; }
            internal bool IsValid => Target != null;

            internal void Set(
                IPlayerDamageReceiver target,
                Vector3 hitPosition,
                float sweepProgress)
            {
                Target = target;
                HitPosition = hitPosition;
                SweepProgress = sweepProgress;
            }

            internal void Clear()
            {
                Target = null;
                HitPosition = Vector3.zero;
                SweepProgress = 0f;
            }
        }
    }
}
