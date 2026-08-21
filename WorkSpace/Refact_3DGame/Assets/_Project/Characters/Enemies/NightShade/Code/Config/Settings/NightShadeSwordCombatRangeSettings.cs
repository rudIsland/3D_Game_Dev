using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 타겟 감지와 위치 Action 전환에 사용하는 거리 기준이다.
    [Serializable]
    internal sealed class NightShadeSwordCombatRangeSettings
    {
        [Header("대상 범위")]
        [SerializeField] private LayerMask targetLayers = 1 << 17;
        [SerializeField, Min(0.1f)] private float findRange = 24f;
        [SerializeField, Min(0.1f)] private float attackRange = 2.4f;
        [SerializeField, Min(0.1f)] private float walkStartRange = 5f;
        [SerializeField, Min(0.1f)] private float runStartRange = 6f;
        [SerializeField, Range(0f, 180f)] private float attackFacingAngle = 14f;

        internal LayerMask TargetLayers => targetLayers;
        internal float FindRange => findRange;
        internal float AttackRange => attackRange;
        internal float WalkStartRange => walkStartRange;
        internal float RunStartRange => runStartRange;
        internal float AttackFacingAngle => attackFacingAngle;

        internal NightShadeSwordCombatRangeSettings()
        {
        }

        internal NightShadeSwordCombatRangeSettings(
            LayerMask targetLayers,
            float findRange,
            float attackRange,
            float walkStartRange,
            float runStartRange,
            float attackFacingAngle)
        {
            this.targetLayers = targetLayers;
            this.findRange = findRange;
            this.attackRange = attackRange;
            this.walkStartRange = walkStartRange;
            this.runStartRange = runStartRange;
            this.attackFacingAngle = attackFacingAngle;
        }

        internal void Validate()
        {
            findRange = Mathf.Max(0.1f, findRange);
            attackRange = Mathf.Clamp(attackRange, 0.1f, findRange);
            walkStartRange = Mathf.Clamp(
                walkStartRange,
                attackRange,
                findRange);
            runStartRange = Mathf.Clamp(
                runStartRange,
                walkStartRange,
                findRange);
            attackFacingAngle = Mathf.Clamp(attackFacingAngle, 0f, 180f);
        }
    }
}
