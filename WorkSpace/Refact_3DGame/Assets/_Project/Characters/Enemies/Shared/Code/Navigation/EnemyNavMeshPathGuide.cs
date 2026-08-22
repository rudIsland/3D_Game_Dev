using UnityEngine;
using UnityEngine.AI;

namespace rudIsland.RPG3D.Characters.Enemies.Navigation
{
    // NavMeshAgent는 경로와 회피 방향만 계산하고 Transform은 직접 움직이지 않는다.
    internal sealed class EnemyNavMeshPathGuide : IEnemyPathGuide
    {
        private const float DestinationUpdateInterval = 0.2f;
        private const float TargetMoveDistance = 0.5f;
        private const float TargetSampleRadius = 1f;
        private const float MinimumDirectionSqrMagnitude = 0.0001f;

        private readonly Transform enemyTransform;
        private readonly NavMeshAgent navMeshAgent;

        private Vector3 lastTargetPosition;
        private float remainingDestinationUpdateTime;
        private bool hasDestination;

        internal EnemyNavMeshPathGuide(
            Transform enemyTransform,
            NavMeshAgent navMeshAgent)
        {
            this.enemyTransform = enemyTransform;
            this.navMeshAgent = navMeshAgent;
        }

        public bool TryGetMoveDirection(
            Vector3 targetPosition,
            float deltaTime,
            out Vector3 moveDirection)
        {
            moveDirection = Vector3.zero;

            if (!CanUseNavMesh())
            {
                Stop();
                return false;
            }

            // CharacterController가 옮긴 실제 위치를 Agent의 경로 계산 위치와 맞춘다.
            navMeshAgent.nextPosition = enemyTransform.position;
            remainingDestinationUpdateTime -= Mathf.Max(0f, deltaTime);

            if (NeedsDestinationUpdate(targetPosition) &&
                !TryUpdateDestination(targetPosition))
            {
                Stop();
                return false;
            }

            if (!hasDestination || navMeshAgent.pathPending)
            {
                return false;
            }

            if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Stop();
                return false;
            }

            Vector3 direction = navMeshAgent.desiredVelocity;
            direction.y = 0f;

            if (direction.sqrMagnitude <= MinimumDirectionSqrMagnitude)
            {
                direction =
                    navMeshAgent.steeringTarget - enemyTransform.position;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= MinimumDirectionSqrMagnitude)
            {
                return false;
            }

            moveDirection = direction.normalized;
            return true;
        }

        public void Stop()
        {
            hasDestination = false;
            remainingDestinationUpdateTime = 0f;

            if (!CanUseNavMesh())
            {
                return;
            }

            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
            navMeshAgent.nextPosition = enemyTransform.position;
        }

        public void Reset()
        {
            Stop();
            lastTargetPosition = Vector3.zero;
        }

        private bool NeedsDestinationUpdate(Vector3 targetPosition)
        {
            if (!hasDestination || remainingDestinationUpdateTime <= 0f)
            {
                return true;
            }

            Vector3 targetMovement = targetPosition - lastTargetPosition;
            targetMovement.y = 0f;
            return targetMovement.sqrMagnitude >=
                TargetMoveDistance * TargetMoveDistance;
        }

        private bool TryUpdateDestination(Vector3 targetPosition)
        {
            if (!NavMesh.SamplePosition(
                    targetPosition,
                    out NavMeshHit navMeshHit,
                    TargetSampleRadius,
                    navMeshAgent.areaMask))
            {
                return false;
            }

            navMeshAgent.isStopped = false;
            if (!navMeshAgent.SetDestination(navMeshHit.position))
            {
                return false;
            }

            lastTargetPosition = targetPosition;
            remainingDestinationUpdateTime =
                DestinationUpdateInterval;
            hasDestination = true;
            return true;
        }

        private bool CanUseNavMesh()
        {
            return navMeshAgent != null &&
                navMeshAgent.enabled &&
                navMeshAgent.gameObject.activeInHierarchy &&
                navMeshAgent.isOnNavMesh;
        }
    }
}
