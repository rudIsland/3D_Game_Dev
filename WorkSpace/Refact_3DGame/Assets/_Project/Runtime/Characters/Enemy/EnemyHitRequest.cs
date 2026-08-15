using UnityEngine;
using rudIsland.RPG3D.Characters.Combat;

namespace rudIsland.RPG3D.Characters
{
    public enum EnemyHitResult
    {
        Ignored = 0,
        Damaged = 1,
        Staggered = 2
    }

    // 적에게 전달할 체력·경직 피해와 넉백 결과를 한 번의 요청으로 보관한다.
    public readonly struct EnemyHitRequest
    {
        private const float DirectionThreshold = 0.000001f;

        public float Damage { get; }
        public float StaggerDamage { get; }
        public Vector3 HitPosition { get; }
        public Vector3 PushDirection { get; }
        public float PushDistance { get; }
        public float HitStopDuration { get; }

        public EnemyHitRequest(
            float damage,
            float staggerDamage,
            Vector3 hitPosition,
            Vector3 pushDirection,
            float pushDistance,
            float hitStopDuration =
                CombatHitStop.DefaultDamageDuration)
        {
            pushDirection.y = 0f;

            Damage = Mathf.Max(0f, damage);
            StaggerDamage = Mathf.Max(0f, staggerDamage);
            HitPosition = hitPosition;
            PushDirection =
                pushDirection.sqrMagnitude > DirectionThreshold
                    ? pushDirection.normalized
                    : Vector3.zero;
            PushDistance = Mathf.Max(0f, pushDistance);
            HitStopDuration = Mathf.Max(0f, hitStopDuration);
        }
    }
}
