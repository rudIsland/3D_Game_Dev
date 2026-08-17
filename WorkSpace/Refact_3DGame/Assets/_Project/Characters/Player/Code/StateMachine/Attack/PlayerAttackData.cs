using UnityEngine;
using UnityEngine.Serialization;
using rudIsland.RPG3D.Characters.Combat;

namespace rudIsland.RPG3D.Player.States.Attack
{
    // 공격 하나의 변경 가능한 설정을 보관할 ScriptableObject다.
    [CreateAssetMenu(
        fileName = "PlayerAttackData",
        menuName = "rudIsland/RPG3D/Player/Attack Data")]
    public sealed class PlayerAttackData : ScriptableObject
    {
        [Header("공격 식별")]
        [SerializeField, Range(1, 6)] private int attackNumber = 1;

        [Header("피해")]
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float staggerDamage = 10f;
        [SerializeField, Min(0f)] private float pushDistance = 0.25f;
        [SerializeField, Min(0f)] private float hitStopDuration =
            CombatHitStop.DefaultDamageDuration;

        [Header("Stamina")]
        [SerializeField, Min(0f)] private float staminaCost = 20f;

        [SerializeField] private AudioClip swingSound;

        [Header("콤보 연결")]
        [SerializeField, Range(0f, 1f)] private float nextInputTime = 1f;
        [Header("구르기 취소")]
        [SerializeField, Range(0f, 1f)] private float rollCancelStartTime = 0.6f;

        [Header("동작 이동")]
        [FormerlySerializedAs("moveScale")]
        [SerializeField, Min(0f)] private float moveDistance = 0.5f;
        [SerializeField]
        private AnimationCurve movementCurve = CreateDefaultMovementCurve();

        public int AttackNumber => attackNumber;
        public float Damage => damage;
        public float StaggerDamage => staggerDamage;
        public float PushDistance => pushDistance;
        public float HitStopDuration => hitStopDuration;
        public float StaminaCost => staminaCost;
        public AudioClip SwingSound => swingSound;
        public float NextInputTime => nextInputTime;
        public float RollCancelStartTime => rollCancelStartTime;
        public float MoveDistance => moveDistance;
        public AnimationCurve MovementCurve => movementCurve;

        private void OnValidate()
        {
            attackNumber = Mathf.Clamp(attackNumber, 1, 6);
            damage = Mathf.Max(0f, damage);
            staggerDamage = Mathf.Max(0f, staggerDamage);
            pushDistance = Mathf.Max(0f, pushDistance);
            hitStopDuration = Mathf.Max(0f, hitStopDuration);
            staminaCost = Mathf.Max(0f, staminaCost);
            nextInputTime = Mathf.Clamp01(nextInputTime);
            rollCancelStartTime = Mathf.Clamp01(rollCancelStartTime);
            moveDistance = Mathf.Max(0f, moveDistance);
            if (movementCurve == null || movementCurve.length < 2)
            {
                movementCurve = CreateDefaultMovementCurve();
            }
        }

        private static AnimationCurve CreateDefaultMovementCurve()
        {
            return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }
}
