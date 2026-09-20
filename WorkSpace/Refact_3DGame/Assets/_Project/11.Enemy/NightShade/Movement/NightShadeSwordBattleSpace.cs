using UnityEngine;
using UnityEngine.AI;

namespace Characters.Enemies.NightShade
{
    // 전투 구역, 시야와 이동할 바닥을 검사한다. 실제 이동은 Movement가 담당한다.
    internal sealed class NightShadeSwordBattleSpace
    {
        private readonly Transform owner;
        private readonly CharacterController body;
        private readonly BoxCollider battleArea;
        private readonly LayerMask obstacles;
        private readonly float homeRadius;
        private readonly float maximumHeightDifference;
        private readonly NavMeshPath path = new NavMeshPath();
        private readonly Vector3[] corners = new Vector3[32];
        private Vector3 homePosition;
        private float pathWait;
        private int cornerCount;
        private int cornerIndex;

        internal NightShadeSwordBattleSpace(Transform owner, CharacterController body,
            BoxCollider battleArea, LayerMask obstacles, float homeRadius,
            float maximumHeightDifference)
        {
            this.owner = owner;
            this.body = body;
            this.battleArea = battleArea;
            this.obstacles = obstacles;
            this.homeRadius = Mathf.Max(1f, homeRadius);
            this.maximumHeightDifference = Mathf.Max(0.1f, maximumHeightDifference);
        }

        internal void Begin()
        {
            homePosition = owner.position;
            ResetPath();
        }

        internal void ResetPath()
        {
            pathWait = 0f;
            cornerCount = 0;
            cornerIndex = 0;
            path.ClearCorners();
        }

        internal bool Contains(Vector3 position)
        {
            if (battleArea != null)
            {
                Vector3 local = battleArea.transform.InverseTransformPoint(position) - battleArea.center;
                Vector3 half = battleArea.size * 0.5f;
                return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y &&
                    Mathf.Abs(local.z) <= half.z;
            }

            Vector3 offset = position - homePosition;
            if (Mathf.Abs(offset.y) > maximumHeightDifference)
                return false;
            offset.y = 0f;
            return offset.sqrMagnitude <= homeRadius * homeRadius;
        }

        internal bool HasAttackHeight(Vector3 targetPosition)
        {
            return Mathf.Abs(targetPosition.y - owner.position.y) <= maximumHeightDifference;
        }

        internal bool CanSee(Vector3 targetPosition)
        {
            return !Physics.Linecast(owner.position + Vector3.up,
                targetPosition + Vector3.up, obstacles, QueryTriggerInteraction.Ignore);
        }

        internal bool CanMove(Vector3 direction, float distance)
        {
            if (distance <= 0f || direction.sqrMagnitude < 0.0001f)
                return true;

            direction.y = 0f;
            direction.Normalize();
            Vector3 end = owner.position + direction * distance;
            if (!Contains(end))
                return false;

            Bounds bounds = body.bounds;
            float radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f);
            float halfLine = Mathf.Max(0f, bounds.extents.y - radius - 0.05f);
            Vector3 lower = bounds.center - Vector3.up * halfLine;
            lower.y += Mathf.Min(body.stepOffset, halfLine);
            if (Physics.CapsuleCast(bounds.center + Vector3.up * halfLine,
                    lower, radius, direction,
                    distance + 0.05f, obstacles, QueryTriggerInteraction.Ignore))
                return false;

            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(distance / 0.25f));
            if (sampleCount > 32)
                return false;
            for (int index = 1; index <= sampleCount; index++)
            {
                Vector3 floorProbe = owner.position + direction * (distance * index / sampleCount);
                floorProbe.y = bounds.min.y + body.stepOffset + 0.1f;
                if (!Physics.Raycast(floorProbe, Vector3.down, out RaycastHit floor,
                        body.stepOffset + 0.6f, obstacles, QueryTriggerInteraction.Ignore) ||
                    Vector3.Angle(floor.normal, Vector3.up) > body.slopeLimit)
                    return false;
            }
            return true;
        }

        internal bool TryGetDirection(Vector3 targetPosition, float deltaTime, out Vector3 direction)
        {
            pathWait -= Mathf.Max(0f, deltaTime);
            if (pathWait <= 0f)
            {
                // 실패한 경로도 매 프레임 다시 계산하지 않는다.
                pathWait = 0.3f;
                cornerCount = 0;
                cornerIndex = 1;
                if (NavMesh.SamplePosition(owner.position, out NavMeshHit start, 0.5f, NavMesh.AllAreas) &&
                    NavMesh.SamplePosition(targetPosition, out NavMeshHit end, 0.5f, NavMesh.AllAreas) &&
                    NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    cornerCount = path.GetCornersNonAlloc(corners);
                    if (cornerCount >= corners.Length)
                        cornerCount = 0;
                }
            }

            while (cornerIndex < cornerCount)
            {
                direction = corners[cornerIndex] - owner.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.04f)
                {
                    direction.Normalize();
                    return true;
                }
                cornerIndex++;
            }

            // NavMesh가 없는 실습 씬에서는 눈앞의 안전한 바닥에서만 직진한다.
            direction = targetPosition - owner.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f || !CanSee(targetPosition))
                return false;
            direction.Normalize();
            return CanMove(direction, 0.5f);
        }
    }
}
