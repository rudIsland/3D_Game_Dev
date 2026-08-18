using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 한 Tick에서 사용할 대상 상태와 거리를 한 번만 읽는다.
    internal sealed class NightShadeSwordTargetReader
    {
        private readonly Transform target;
        private readonly IUnitDeathState targetDeathState;
        private readonly INightShadeSwordMovement movement;

        internal bool IsAlive { get; private set; }
        internal Vector3 Position { get; private set; }
        internal float DistanceSquared { get; private set; }

        internal NightShadeSwordTargetReader(
            Transform target,
            IUnitDeathState targetDeathState,
            INightShadeSwordMovement movement)
        {
            this.target = target;
            this.targetDeathState = targetDeathState;
            this.movement = movement;
            DistanceSquared = float.PositiveInfinity;
        }

        internal void Refresh()
        {
            IsAlive = target != null &&
                target.gameObject.activeInHierarchy &&
                (targetDeathState == null || !targetDeathState.IsDead);
            if (!IsAlive)
            {
                DistanceSquared = float.PositiveInfinity;
                return;
            }

            Position = target.position;
            Vector3 difference = Position - movement.Position;
            difference.y = 0f;
            DistanceSquared = difference.sqrMagnitude;
        }

        internal bool IsFound(float findRangeSquared)
        {
            return IsAlive && DistanceSquared <= findRangeSquared;
        }
    }
}
