using UnityEngine;
using UnityEngine.Serialization;
using Characters.Combat.AttackData;

namespace Characters.Player.StateMachine.States.Attack
{
    // 공격 하나의 변경 가능한 설정을 보관할 ScriptableObject다.
    [CreateAssetMenu(fileName = "PlayerAttackData", menuName = "Characters/Player/Attack Data")]
    public sealed class PlayerAttackData : ScriptableObject
    {
        [Header("공격 식별")]
        [SerializeField, Range(1, 6)] private int attackNumber = 1;

        [Header("피해")]
        [SerializeField] private AttackDamage attackDamage = new();

        [Header("Stamina")]
        [SerializeField, Min(0f)] private float staminaCost = 20f;

        [SerializeField] private AudioClip swingSound;

        [Header("콤보 연결")]
        [FormerlySerializedAs("nextInputTime")]
        [SerializeField, Range(0f, 1f)] private float comboOpenNormalizedTime = 1f;

        [Header("구르기 취소")]
        [FormerlySerializedAs("rollCancelStartTime")]
        [SerializeField, Range(0f, 1f)]
        private float rollCancelOpenNormalizedTime = 0.6f;

        [Header("공격 회전")]
        [SerializeField, Range(0f, 1f)]
        private float turnEndNormalizedTime = 0.3f;

        [Header("동작 이동")]
        [FormerlySerializedAs("moveScale")]
        [SerializeField, Min(0f)] private float moveDistance = 0.5f;
        [SerializeField]
        private AnimationCurve movementCurve = CreateDefaultMovementCurve();

        public int AttackNumber => attackNumber;
        public AttackDamage Damage => attackDamage;
        public float StaminaCost => staminaCost;
        public AudioClip SwingSound => swingSound;
        public float ComboOpenNormalizedTime => comboOpenNormalizedTime;
        public float RollCancelOpenNormalizedTime =>
            rollCancelOpenNormalizedTime;
        public float TurnEndNormalizedTime => turnEndNormalizedTime;
        public float MoveDistance => moveDistance;
        public AnimationCurve MovementCurve => movementCurve;
        internal bool CanStartComboAt(
            float normalizedTime,
            bool hasAttackHitEnded,
            float comboCloseNormalizedTime)
        {
            return hasAttackHitEnded &&
                normalizedTime >= comboOpenNormalizedTime &&
                normalizedTime <= comboCloseNormalizedTime;
        }

        internal bool CanCancelToRollAt(float normalizedTime)
        {
            return normalizedTime >= rollCancelOpenNormalizedTime;
        }

        internal bool CanTurnAt(float normalizedTime)
        {
            return normalizedTime < turnEndNormalizedTime;
        }

        private void OnValidate()
        {
            attackNumber = Mathf.Clamp(attackNumber, 1, 6);
            attackDamage ??= new AttackDamage();
            staminaCost = Mathf.Max(0f, staminaCost);
            comboOpenNormalizedTime = Mathf.Clamp01(comboOpenNormalizedTime);
            rollCancelOpenNormalizedTime = Mathf.Clamp01(rollCancelOpenNormalizedTime);
            turnEndNormalizedTime = Mathf.Clamp01(turnEndNormalizedTime);
            moveDistance = Mathf.Max(0f, moveDistance);
            if (movementCurve == null || movementCurve.length < 2)
            {
                movementCurve = CreateDefaultMovementCurve();
            }
        }

        private static AnimationCurve CreateDefaultMovementCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 4f),
                new Keyframe(0.4f, 1f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f));
        }
    }
}
