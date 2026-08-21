using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    [CreateAssetMenu(
        fileName = "NightShadeSwordSingleAttackData",
        menuName = "rudIsland/RPG3D/NightShade/Single Sword Attack Data")]
    public sealed class NightShadeSwordSingleAttackData : NightShadeSwordAttackData
    {
        [Header("NightShade Sword 공격 식별")]
        [SerializeField] private NightShadeSwordActionId actionId =
            NightShadeSwordActionId.Light;

        internal override NightShadeSwordActionId ActionId => actionId;

        protected override void ValidateNightShadeAttack()
        {
            if (actionId != NightShadeSwordActionId.Light &&
                actionId != NightShadeSwordActionId.Heavy &&
                actionId != NightShadeSwordActionId.WideSwing)
            {
                actionId = NightShadeSwordActionId.Light;
            }
        }

        private void OnValidate()
        {
            Validate();
        }
    }
}
