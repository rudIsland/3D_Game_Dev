using UnityEngine;
using rudIsland.RPG3D.Combat.Attack;
using rudIsland.RPG3D.Combat.Detection;

namespace rudIsland.RPG3D.Combat.Result
{
    public enum HitReactionDirection
    {
        Front = 0,
        Back = 1,
        Left = 2,
        Right = 3
    }

    public enum HitStrength
    {
        Light = 0,
        Heavy = 1,
        Knockdown = 2
    }

    // 피해자의 방향축을 기준으로 최종 피격 반응을 한 값으로 보관한다.
    public readonly struct HitReaction
    {
        private const float MinimumDirectionLength = 0.0001f;

        public HitReactionDirection Direction { get; }
        public HitStrength Strength { get; }
        public HitBodyPart BodyPart { get; }
        public Vector3 PushDirection { get; }
        public float PushDistance { get; }

        private HitReaction(
            HitReactionDirection direction,
            HitStrength strength,
            HitBodyPart bodyPart,
            Vector3 pushDirection,
            float pushDistance)
        {
            Direction = direction;
            Strength = IsValidStrength(strength)
                ? strength
                : HitStrength.Light;
            BodyPart = bodyPart;
            PushDirection = pushDirection;
            PushDistance = pushDistance;
        }

        // 공격이 밀어내는 방향의 반대쪽을 공격이 들어온 위치로 판단한다.
        public static HitReaction Create(
            in AttackHitInput hit,
            Vector3 victimForward,
            Vector3 victimRight)
        {
            Vector3 pushDirection =
                GetHorizontalDirection(hit.HitDirection);
            HitReactionDirection direction =
                GetReactionDirection(
                    pushDirection,
                    victimForward,
                    victimRight);

            return new HitReaction(
                direction,
                hit.Strength,
                hit.HitBodyPart,
                pushDirection,
                hit.PushDistance);
        }

        private static HitReactionDirection GetReactionDirection(
            Vector3 pushDirection,
            Vector3 victimForward,
            Vector3 victimRight)
        {
            if (pushDirection == Vector3.zero)
            {
                return HitReactionDirection.Front;
            }

            Vector3 safeForward =
                GetHorizontalDirection(victimForward);
            if (safeForward == Vector3.zero)
            {
                safeForward = Vector3.forward;
            }

            Vector3 safeRight =
                GetHorizontalDirection(victimRight);
            if (safeRight == Vector3.zero)
            {
                safeRight = Vector3.right;
            }

            Vector3 attackSourceDirection = -pushDirection;
            float frontAmount = Vector3.Dot(
                safeForward,
                attackSourceDirection);
            float rightAmount = Vector3.Dot(
                safeRight,
                attackSourceDirection);

            if (Mathf.Abs(frontAmount) >= Mathf.Abs(rightAmount))
            {
                return frontAmount >= 0f
                    ? HitReactionDirection.Front
                    : HitReactionDirection.Back;
            }

            return rightAmount >= 0f
                ? HitReactionDirection.Right
                : HitReactionDirection.Left;
        }

        private static Vector3 GetHorizontalDirection(
            Vector3 direction)
        {
            direction.y = 0f;
            if (!IsFinite(direction.x) ||
                !IsFinite(direction.z) ||
                direction.sqrMagnitude <=
                    MinimumDirectionLength * MinimumDirectionLength)
            {
                return Vector3.zero;
            }

            return direction.normalized;
        }

        private static bool IsValidStrength(HitStrength strength)
        {
            return strength >= HitStrength.Light &&
                strength <= HitStrength.Knockdown;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}
