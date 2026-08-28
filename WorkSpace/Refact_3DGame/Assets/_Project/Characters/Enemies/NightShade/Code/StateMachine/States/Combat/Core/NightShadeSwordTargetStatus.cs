// 한 Tick에 한 번 대상 상태를 계산해 State와 Action이 공유한다.
using UnityEngine;

namespace Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordTargetStatus
    {
        private readonly Transform target;
        private readonly IUnitDeathState targetDeathState;
        private readonly INightShadeSwordMovement movement;
        private readonly float findRangeSquared;
        private readonly float attackRangeSquared;
        private readonly float attackRange;
        private readonly float walkStartRangeSquared;
        private readonly float runStartRangeSquared;
        private readonly float attackFacingDot;

        internal bool IsAlive { get; private set; }
        internal bool IsDetected { get; private set; }
        internal bool IsInsideAttackRange { get; private set; }
        internal bool IsFacingAttackDirection { get; private set; }
        internal Vector3 TargetPosition { get; private set; }
        internal float AttackDistanceRatio { get; private set; }
        internal bool ShouldSwitchToWalk { get; private set; }
        internal bool ShouldSwitchToChase { get; private set; }

        internal NightShadeSwordTargetStatus(
            Transform target,
            IUnitDeathState targetDeathState,
            INightShadeSwordMovement movement,
            NightShadeSwordTargetRuntimeConfig settings)
        {
            this.target = target;
            this.targetDeathState = targetDeathState;
            this.movement = movement;
            findRangeSquared = settings.FindRangeSquared;
            attackRangeSquared = settings.AttackRangeSquared;
            attackRange = settings.AttackRange;
            walkStartRangeSquared = settings.WalkStartRangeSquared;
            runStartRangeSquared = settings.RunStartRangeSquared;
            attackFacingDot = settings.AttackFacingDot;
            AttackDistanceRatio = 1f;
        }

        internal void Refresh()
        {
            IsAlive = target != null &&
                target.gameObject.activeInHierarchy &&
                (targetDeathState == null || !targetDeathState.IsDead);
            if (!IsAlive)
            {
                ClearMissingTarget();
                return;
            }

            TargetPosition = target.position;
            Vector3 direction = TargetPosition - movement.Position;
            direction.y = 0f;
            float distanceSquared = direction.sqrMagnitude;
            IsDetected = distanceSquared <= findRangeSquared;
            IsInsideAttackRange = distanceSquared <= attackRangeSquared;
            ShouldSwitchToWalk = distanceSquared <= walkStartRangeSquared;
            ShouldSwitchToChase = distanceSquared >= runStartRangeSquared;
            AttackDistanceRatio = Mathf.Clamp01(
                Mathf.Sqrt(distanceSquared) / attackRange);

            float facingDot;
            if (distanceSquared <= 0.000001f)
            {
                facingDot = 1f;
            }
            else
            {
                direction *= 1f / Mathf.Sqrt(distanceSquared);
                facingDot = Vector3.Dot(movement.Forward, direction);
            }

            IsFacingAttackDirection = facingDot >= attackFacingDot;
        }

        private void ClearMissingTarget()
        {
            IsDetected = false;
            IsInsideAttackRange = false;
            IsFacingAttackDirection = false;
            ShouldSwitchToWalk = false;
            ShouldSwitchToChase = false;
            AttackDistanceRatio = 1f;
        }
    }
}
