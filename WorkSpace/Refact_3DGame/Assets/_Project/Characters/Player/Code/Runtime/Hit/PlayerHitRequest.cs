using rudIsland.RPG3D.Characters.Combat.AttackData;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Runtime.Hit
{
    public enum PlayerHitSurface
    {
        Body = 0,   //몸
        Guard = 1   //방어
    }

    public enum PlayerHitResult
    {
        Ignored = 0,    //예외
        Avoided = 1,    //회피
        Blocked = 2,    //막기
        Damaged = 3,    //피해
        GuardBroken = 4 //가드 파괴
    }

    // 플레이어에게 전달할 피해와 접촉 방향을 한 번의 요청으로 보관한다.
    public readonly struct PlayerHitRequest
    {
        private const float DirectionThreshold = 0.000001f;

        public AttackDamage Damage { get; }
        public Vector3 HitPosition { get; }
        public Vector3 PushDirection { get; }
        public PlayerHitSurface HitSurface { get; }
        public float PushDistance => Damage != null ? Damage.PushDistance : 0f;

        public PlayerHitRequest(
            AttackDamage damage,
            Vector3 hitPosition,
            Vector3 pushDirection,
            PlayerHitSurface hitSurface = PlayerHitSurface.Body)
        {
            pushDirection.y = 0f;

            Damage = damage;
            HitPosition = hitPosition;
            HitSurface = hitSurface;
            PushDirection =
                pushDirection.sqrMagnitude > DirectionThreshold
                    ? pushDirection.normalized
                    : Vector3.zero;
        }
    }
}
