// 한 Tick에 한 번 대상 상황을 계산해 Action들이 공유한다.
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 한 Tick에서 필요한 대상 상황을 한 번만 계산해 Action들이 함께 사용한다.
    internal sealed class NightShadeSwordSituationReader
    {
        private readonly Transform target;
        private readonly IUnitDeathState targetDeathState;
        private readonly INightShadeSwordMovement movement;
        private readonly float findRangeSquared;
        private readonly float attackRangeSquared;
        private readonly float attackRange;
        private readonly float attackFacingDot;

        internal bool IsTargetAlive { get; private set; }
        internal bool IsTargetDetected { get; private set; }
        internal bool IsInsideAttackRange { get; private set; }
        internal bool IsFacingAttackDirection { get; private set; }
        internal Vector3 TargetPosition { get; private set; }
        internal float DistanceSquared { get; private set; }
        internal float AttackDistanceRatio { get; private set; }
        internal float FacingDot { get; private set; }

        internal NightShadeSwordSituationReader(
            Transform target,
            IUnitDeathState targetDeathState,
            INightShadeSwordMovement movement,
            NightShadeSwordSettings settings)
        {
            this.target = target;
            this.targetDeathState = targetDeathState;
            this.movement = movement;
            findRangeSquared = settings.FindRangeSquared;
            attackRangeSquared = settings.AttackRangeSquared;
            attackRange = settings.AttackRange;
            attackFacingDot = settings.AttackFacingDot;
            DistanceSquared = float.PositiveInfinity;
            AttackDistanceRatio = 1f;
            FacingDot = -1f;
        }

        internal void Refresh()
        {
            IsTargetAlive = target != null &&
                target.gameObject.activeInHierarchy &&
                (targetDeathState == null || !targetDeathState.IsDead);
            if (!IsTargetAlive)
            {
                ClearMissingTarget();
                return;
            }

            TargetPosition = target.position;
            Vector3 direction = TargetPosition - movement.Position;
            direction.y = 0f;
            DistanceSquared = direction.sqrMagnitude;
            IsTargetDetected = DistanceSquared <= findRangeSquared;
            IsInsideAttackRange = DistanceSquared <= attackRangeSquared;
            AttackDistanceRatio = Mathf.Clamp01(
                Mathf.Sqrt(DistanceSquared) / attackRange);

            if (DistanceSquared <= 0.000001f)
            {
                FacingDot = 1f;
            }
            else
            {
                direction *= 1f / Mathf.Sqrt(DistanceSquared);
                FacingDot = Vector3.Dot(movement.Forward, direction);
            }

            IsFacingAttackDirection = FacingDot >= attackFacingDot;
        }

        private void ClearMissingTarget()
        {
            IsTargetDetected = false;
            IsInsideAttackRange = false;
            IsFacingAttackDirection = false;
            DistanceSquared = float.PositiveInfinity;
            AttackDistanceRatio = 1f;
            FacingDot = -1f;
        }
    }
}
