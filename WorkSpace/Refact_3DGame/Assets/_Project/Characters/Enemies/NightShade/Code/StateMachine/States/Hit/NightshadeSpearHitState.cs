
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 방향별 피격 애니메이션을 재생하고, 종료 후 추적으로 복귀한다.
    internal sealed class NightshadeSpearHitState : INightshadeSpearState
    {
        private readonly NightshadeSpearStateMachine stateMachine;
        private bool hasEnteredHit;
        private Vector3 hitPosition;

        public string Name => nameof(NightshadeSpearHitState);

        internal NightshadeSpearHitState(
            NightshadeSpearStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        internal void Restart()
        {
            stateMachine.Animation.SetMovement(0f, 0f);
            hasEnteredHit = false;
            NightshadeSpearHitDirection hitDirection = GetHitDirection();

#if UNITY_EDITOR
            Debug.DrawLine(
                stateMachine.Movement.Position,
                hitPosition,
                Color.red,
                1.5f);
            Debug.DrawRay(
                hitPosition,
                Vector3.up * 0.5f,
                Color.yellow,
                1.5f);
            Debug.Log(
                $"Nightshade 피격 위치: {hitPosition}, 방향: {hitDirection}");
#endif

            stateMachine.Animation.PlayHit(hitDirection);
        }

        internal void SetHitPosition(Vector3 hitPosition)
        {
            this.hitPosition = hitPosition;
        }

        public void Enter()
        {
            Restart();
        }

        public void Update(float deltaTime)
        {
            bool hasActionTime = stateMachine.TryGetCurrentActionTime(
                out float normalizedTime);
            if (hasActionTime)
            {
                hasEnteredHit = true;
            }

            if (hasEnteredHit &&
                !stateMachine.IsActionTransitioning() &&
                hasActionTime &&
                normalizedTime >= 1f)
            {
                stateMachine.ChangeToAliveState();
            }
        }

        private NightshadeSpearHitDirection GetHitDirection()
        {
            Vector3 direction =
                hitPosition - stateMachine.Movement.Position;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return NightshadeSpearHitDirection.Forward;
            }

            direction.Normalize();

            float forwardDot =
                Vector3.Dot(stateMachine.Movement.Forward, direction);

            float rightDot =
                Vector3.Dot(stateMachine.Movement.Right, direction);

            if (Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot))
            {
                return forwardDot >= 0f
                    ? NightshadeSpearHitDirection.Forward
                    : NightshadeSpearHitDirection.Backward;
            }

            return rightDot >= 0f
                ? NightshadeSpearHitDirection.Right
                : NightshadeSpearHitDirection.Left;
        }

        public void Exit()
        {
            stateMachine.Animation.ResetActionSpeed();
        }

    }
}
