using rudIsland.RPG3D.Characters.Enemies.AttackData;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    public abstract class NightShadeSwordAttackData : EnemyAttackData
    {
        [Header("공격 전진 보정")]
        [SerializeField, Min(0f)] private float moveDistance;
        [SerializeField]
        private AnimationCurve movementCurve = CreateDefaultMovementCurve();
        [SerializeField, Min(0f)] private float targetStopDistance = 1.3f;
        [SerializeField, Min(0f)]
        private float maximumAddedMoveDistance = 0.35f;
        [SerializeField, Range(0f, 180f)]
        private float maximumTurnAngle = 20f;

        internal abstract NightShadeSwordActionId ActionId { get; }
        internal virtual float ComboFirstExitNormalizedTime => 1f;
        internal virtual float ComboSecondDelay => 0f;
        internal float MoveDistance => moveDistance;
        internal AnimationCurve MovementCurve => movementCurve;
        internal float TargetStopDistance => targetStopDistance;
        internal float MaximumAddedMoveDistance =>
            maximumAddedMoveDistance;
        internal float MaximumTurnAngle => maximumTurnAngle;

        internal void Validate()
        {
            ValidateAttackData(
                ActionId == NightShadeSwordActionId.Combo ? 2 : 1);
            moveDistance = Mathf.Max(0f, moveDistance);
            targetStopDistance = Mathf.Max(0f, targetStopDistance);
            maximumAddedMoveDistance = Mathf.Max(
                0f,
                maximumAddedMoveDistance);
            maximumTurnAngle = Mathf.Clamp(maximumTurnAngle, 0f, 180f);
            if (movementCurve == null || movementCurve.length < 2)
            {
                movementCurve = CreateDefaultMovementCurve();
            }

            ValidateNightShadeAttack();
        }

        protected abstract void ValidateNightShadeAttack();

        private static AnimationCurve CreateDefaultMovementCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 5f),
                new Keyframe(0.18f, 1f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f));
        }
    }
}
