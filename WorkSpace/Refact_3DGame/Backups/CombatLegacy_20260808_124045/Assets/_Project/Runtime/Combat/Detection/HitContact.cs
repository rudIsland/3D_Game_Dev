using UnityEngine;

namespace rudIsland.RPG3D.Combat.Detection
{
    public enum HitBodyPart
    {
        Unknown = 0,
        Head = 1,
        Body = 2,
        Arm = 3,
        Leg = 4
    }

    // 무기와 피격 몸체가 실제로 닿은 결과만 보관한다.
    public readonly struct HitContact
    {
        public Vector3 HitPoint { get; } // 피격 또는 피해 관련 값
        public Vector3 HitNormal { get; } // 피격 또는 피해 관련 값
        public Vector3 HitDirection { get; } // 피격 또는 피해 관련 값
        public HitBodyPart BodyPart { get; } // 외부에 제공하는 읽기 값
        public float HitSpeed { get; } // 피격 또는 피해 관련 값

        public HitContact(
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 hitDirection,
            HitBodyPart bodyPart,
            float hitSpeed = 0f)
        {
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            HitDirection = hitDirection;
            BodyPart = bodyPart;
            HitSpeed =
                hitSpeed > 0f &&
                !float.IsNaN(hitSpeed) &&
                !float.IsInfinity(hitSpeed)
                    ? hitSpeed
                    : 0f;
        }
    }
}
