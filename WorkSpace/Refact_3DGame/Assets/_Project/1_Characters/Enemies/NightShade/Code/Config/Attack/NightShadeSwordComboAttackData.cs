using UnityEngine;

namespace Characters.Enemies.NightShade
{
    [CreateAssetMenu(
        fileName = "NightShadeSwordComboAttackData",
        menuName = "Characters/Enemies/NightShade/Combo Sword Attack Data")]
    public sealed class NightShadeSwordComboAttackData : NightShadeSwordAttackData
    {
        [Header("Combo 연결 설정")]
        [SerializeField, Range(0.35f, 1f)]
        private float firstExitNormalizedTime = 0.4f;
        [SerializeField, Min(0f)] private float secondDelay = 0.15f;

        internal override NightShadeSwordActionId ActionId =>
            NightShadeSwordActionId.Combo;
        internal override float ComboFirstExitNormalizedTime =>
            firstExitNormalizedTime;
        internal override float ComboSecondDelay => secondDelay;

        protected override void ValidateNightShadeAttack()
        {
            firstExitNormalizedTime = Mathf.Clamp(
                firstExitNormalizedTime,
                0.35f,
                1f);
            secondDelay = Mathf.Max(0f, secondDelay);
        }

        private void OnValidate()
        {
            Validate();
        }
    }
}
