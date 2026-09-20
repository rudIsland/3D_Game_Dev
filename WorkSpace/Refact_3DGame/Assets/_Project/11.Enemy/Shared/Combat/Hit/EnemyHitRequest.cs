using UnityEngine;
using Characters.Combat;

namespace Characters
{
    // 적에게 전달할 체력·경직 피해와 타격 방향을 한 번의 요청으로 보관한다.
    public readonly struct EnemyHitRequest
    {
        private const float DirectionThreshold = 0.000001f;

        public float Damage { get; }
        public float StaggerDamage { get; }
        public AttackStrength Strength { get; }
        public Vector3 HitPosition { get; }
        public Vector3 HitDirection { get; }
        public Vector3 PushDirection { get; }
        public float PushDistance { get; }
        public float HitStopDuration { get; }

        public EnemyHitRequest(
            float damage,
            float staggerDamage,
            AttackStrength strength,
            Vector3 hitPosition,
            Vector3 hitDirection,
            Vector3 pushDirection,
            float pushDistance,
            float hitStopDuration =
                CombatHitStop.DefaultDamageDuration)
        {
            hitDirection.y = 0f;
            pushDirection.y = 0f;

            Damage = Mathf.Max(0f, damage);
            StaggerDamage = Mathf.Max(0f, staggerDamage);
            Strength = strength;
            HitPosition = hitPosition;
            HitDirection = NormalizeDirection(hitDirection);
            PushDirection = NormalizeDirection(pushDirection);
            PushDistance = Mathf.Max(0f, pushDistance);
            HitStopDuration = Mathf.Max(0f, hitStopDuration);
        }

        // 기존 호출부가 공격 세기를 지정하지 않으면 현재와 같은 Light 타격으로 처리한다.
        public EnemyHitRequest(
            float damage,
            float staggerDamage,
            Vector3 hitPosition,
            Vector3 pushDirection,
            float pushDistance,
            float hitStopDuration =
                CombatHitStop.DefaultDamageDuration)
            : this(
                damage,
                staggerDamage,
                AttackStrength.Light,
                hitPosition,
                pushDirection,
                pushDirection,
                pushDistance,
                hitStopDuration)
        {
        }

        private static Vector3 NormalizeDirection(Vector3 direction)
        {
            return direction.sqrMagnitude > DirectionThreshold
                ? direction.normalized
                : Vector3.zero;
        }
    }
}
