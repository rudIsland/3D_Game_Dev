using UnityEngine;
using System.Collections.Generic;
using Characters;
using Characters.Combat;

namespace Characters.Player.Combat.Attack
{
    // 공격 Window 동안 검의 현재 Capsule과 프레임 사이 궤적을 검사한다.
    internal sealed class PlayerAttackRangeDetector
    {
        private const int MaximumDetectedColliderCount = 32;
        private const float MinimumSweepDistance = 0.0001f;

        private readonly Transform attackerRoot;
        private readonly Transform weaponHitStart;
        private readonly Transform weaponHitEnd;
        private readonly LayerMask enemyLayers;
        private readonly float weaponHitRadius;
        private readonly CombatHitStop attackerHitStop;
        private readonly CombatHitEffectPlayer hitEffectPlayer;
        private readonly PlayerAttackEffectPlayer attackEffectPlayer;
        private readonly Collider[] detectedColliders = new Collider[MaximumDetectedColliderCount];
        private readonly RaycastHit[] sweepHits = new RaycastHit[MaximumDetectedColliderCount];
        private readonly HashSet<IEnemyDamageReceiver> hitTargets = new HashSet<IEnemyDamageReceiver>(16);

        private bool isWindowOpen;
        private bool hasPreviousWeaponPositions;
        private float attackDamage;
        private float attackStaggerDamage;
        private AttackStrength attackStrength;
        private float attackPushDistance;
        private float attackHitStopDuration;
        private Vector3 previousStartPosition;
        private Vector3 previousMiddlePosition;
        private Vector3 previousEndPosition;

        public PlayerAttackRangeDetector(
            Transform attackerRoot,
            Transform weaponHitStart,
            Transform weaponHitEnd,
            LayerMask enemyLayers,
            float weaponHitRadius,
            CombatHitStop attackerHitStop,
            CombatHitEffectPlayer hitEffectPlayer,
            PlayerAttackEffectPlayer attackEffectPlayer)
        {
            this.attackerRoot = attackerRoot;
            this.weaponHitStart = weaponHitStart;
            this.weaponHitEnd = weaponHitEnd;
            this.enemyLayers = enemyLayers;
            this.weaponHitRadius = Mathf.Max(0f, weaponHitRadius);
            this.attackerHitStop = attackerHitStop;
            this.hitEffectPlayer = hitEffectPlayer;
            this.attackEffectPlayer = attackEffectPlayer;
        }

        public void Open(
            float damage,
            float staggerDamage,
            AttackStrength strength,
            float pushDistance,
            float hitStopDuration)
        {
            isWindowOpen = true;
            attackDamage = Mathf.Max(0f, damage);
            attackStaggerDamage = Mathf.Max(0f, staggerDamage);
            attackStrength = strength;
            attackPushDistance = Mathf.Max(0f, pushDistance);
            attackHitStopDuration = Mathf.Max(0f, hitStopDuration);
            hitTargets.Clear();
            SaveCurrentWeaponPositions();
        }

        public void Tick()
        {
            if (!CanDetectHit())
            {
                return;
            }

            Vector3 currentStartPosition = weaponHitStart.position;
            Vector3 currentEndPosition = weaponHitEnd.position;
            Vector3 currentMiddlePosition = (currentStartPosition + currentEndPosition) * 0.5f;

            DetectCurrentWeapon(currentStartPosition, currentEndPosition);

            if (hasPreviousWeaponPositions)
            {
                DetectMovedPoint(previousStartPosition, currentStartPosition);
                DetectMovedPoint(previousMiddlePosition, currentMiddlePosition);
                DetectMovedPoint(previousEndPosition, currentEndPosition);
            }

            SaveWeaponPositions(
                currentStartPosition,
                currentMiddlePosition,
                currentEndPosition);
        }

        public void Close()
        {
            isWindowOpen = false;
            hasPreviousWeaponPositions = false;
            attackDamage = 0f;
            attackStaggerDamage = 0f;
            attackStrength = AttackStrength.Light;
            attackPushDistance = 0f;
            attackHitStopDuration = 0f;
            hitTargets.Clear();
        }

        private bool CanDetectHit()
        {
            return isWindowOpen &&
                attackerRoot != null &&
                weaponHitStart != null &&
                weaponHitEnd != null &&
                enemyLayers.value != 0 &&
                weaponHitRadius > 0f;
        }

        private void DetectCurrentWeapon(Vector3 startPosition, Vector3 endPosition)
        {
            int detectedCount = Physics.OverlapCapsuleNonAlloc(
                startPosition,
                endPosition,
                weaponHitRadius,
                detectedColliders,
                enemyLayers,
                QueryTriggerInteraction.Collide);

            ApplyCurrentHits(
                detectedCount,
                startPosition,
                endPosition);
        }

        private void ApplyCurrentHits(
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
                Vector3 hitPosition = detectedCollider.ClosestPoint(shapePoint);
                TryApplyHit(detectedCollider, hitPosition);
            }
        }

        private void DetectMovedPoint(Vector3 previousPosition, Vector3 currentPosition)
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
                weaponHitRadius,
                direction,
                sweepHits,
                distance,
                enemyLayers,
                QueryTriggerInteraction.Collide);

            for (int index = 0; index < detectedCount; index++)
            {
                RaycastHit sweepHit = sweepHits[index];
                if (sweepHit.collider == null)
                {
                    continue;
                }

                TryApplyHit(sweepHit.collider, sweepHit.point);
            }
        }

        private void TryApplyHit(Collider detectedCollider, Vector3 hitPosition)
        {
            IEnemyDamageReceiver target = detectedCollider.GetComponentInParent<IEnemyDamageReceiver>();
            if (target == null || !hitTargets.Add(target))
            {
                return;
            }

            Vector3 hitDirection = hitPosition - attackerRoot.position;
            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude <= 0.000001f)
            {
                hitDirection = attackerRoot.forward;
            }

            Component targetComponent = target as Component;
            Vector3 targetPosition = targetComponent != null
                ? targetComponent.transform.position
                : detectedCollider.transform.position;
            Vector3 pushDirection = targetPosition - attackerRoot.position;
            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude <= 0.000001f)
            {
                pushDirection = attackerRoot.forward;
            }

            var hitRequest = new EnemyHitRequest(
                attackDamage,
                attackStaggerDamage,
                attackStrength,
                hitPosition,
                hitDirection,
                pushDirection,
                attackPushDistance,
                attackHitStopDuration);
            EnemyHitResult hitResult = target.TakeHit(in hitRequest);
            if (hitResult.HasDamageFeedback)
            {
                attackerHitStop?.Request(hitRequest.HitStopDuration);
                hitEffectPlayer?.PlayBodyHit(
                    hitRequest.HitPosition,
                    hitRequest.HitDirection);
                attackEffectPlayer?.PlayConfirmedHit(in hitResult);
            }
        }

        private void SaveCurrentWeaponPositions()
        {
            if (weaponHitStart == null || weaponHitEnd == null)
            {
                hasPreviousWeaponPositions = false;
                return;
            }

            Vector3 startPosition = weaponHitStart.position;
            Vector3 endPosition = weaponHitEnd.position;
            SaveWeaponPositions(
                startPosition,
                (startPosition + endPosition) * 0.5f,
                endPosition);
        }

        private void SaveWeaponPositions(
            Vector3 startPosition,
            Vector3 middlePosition,
            Vector3 endPosition)
        {
            previousStartPosition = startPosition;
            previousMiddlePosition = middlePosition;
            previousEndPosition = endPosition;
            hasPreviousWeaponPositions = true;
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
    }
}
