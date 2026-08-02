using System;
using UnityEngine;

namespace rudIsland.RPG3D.Combat
{
    public enum AttackShapeType
    {
        Capsule = 0,
        Sphere = 1,
        Box = 2
    }

    [Serializable]
    // 공격 부위가 실제로 검사할 위치와 크기를 보관한다.
    public struct AttackShape
    {
        [SerializeField] private AttackShapeType shapeType; // Inspector 설정 값
        [SerializeField] private Transform startPoint; // 씬 또는 시스템 참조
        [SerializeField] private Transform endPoint; // 씬 또는 시스템 참조
        [SerializeField, Min(0.01f)] private float radius; // 내부에서 사용하는 값
        [SerializeField] private Vector3 boxSize; // 개수 또는 크기

        internal AttackShapeType Type => shapeType; // 외부에 제공하는 읽기 값
        internal Vector3 StartPosition => // 이동 정보
            startPoint != null ? startPoint.position : Vector3.zero;
        internal Vector3 EndPosition => // 이동 정보
            endPoint != null ? endPoint.position : StartPosition;
        internal Vector3 MiddlePosition => // 이동 정보
            (StartPosition + EndPosition) * 0.5f;
        internal Quaternion Rotation => // 이동 정보
            startPoint != null
                ? startPoint.rotation
                : Quaternion.identity;
        internal float Radius => IsValidLength(radius) ? radius : 0f; // 외부에 제공하는 읽기 값
        internal Vector3 BoxSize => new Vector3( // 개수 또는 크기
            GetValidLength(boxSize.x),
            GetValidLength(boxSize.y),
            GetValidLength(boxSize.z));
        internal Vector3 BoxHalfSize => BoxSize * 0.5f; // 개수 또는 크기

        internal bool IsReady
        {
            get
            {
                switch (shapeType)
                {
                    case AttackShapeType.Capsule:
                        return startPoint != null &&
                            endPoint != null &&
                            Radius > 0f;
                    case AttackShapeType.Sphere:
                        return startPoint != null && Radius > 0f;
                    case AttackShapeType.Box:
                        Vector3 size = BoxSize;
                        return startPoint != null &&
                            size.x > 0f &&
                            size.y > 0f &&
                            size.z > 0f;
                    default:
                        return false;
                }
            }
        }

        public AttackShape(
            AttackShapeType shapeType,
            Transform startPoint,
            Transform endPoint,
            float radius,
            Vector3 boxSize)
        {
            this.shapeType = shapeType;
            this.startPoint = startPoint;
            this.endPoint = endPoint;
            this.radius = radius;
            this.boxSize = boxSize;
        }

        private static float GetValidLength(float length)
        {
            return IsValidLength(length) ? length : 0f;
        }

        private static bool IsValidLength(float length)
        {
            return length > 0f &&
                !float.IsNaN(length) &&
                !float.IsInfinity(length);
        }
    }
}
