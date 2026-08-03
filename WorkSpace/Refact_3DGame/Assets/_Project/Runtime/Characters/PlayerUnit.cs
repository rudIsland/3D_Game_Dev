namespace rudIsland.RPG3D.Characters
{
    // 플레이어는 재활성화만으로 체력을 되돌리지 않는다.
    public abstract class PlayerUnit : Unit
    {
        protected PlayerUnit(float maxHealth)
            : base(UnitTeam.Player, maxHealth)
        {
        }

        protected PlayerUnit(
            float maxHealth,
            float staggerLimit,
            float staggerRecoverDelay,
            float staggerRecoverSpeed,
            float maxStamina,
            float staminaRecoverDelay,
            float staminaRecoverSpeed,
            float guardAngle)
            : base(
                UnitTeam.Player,
                maxHealth,
                staggerLimit,
                staggerRecoverDelay,
                staggerRecoverSpeed,
                maxStamina,
                staminaRecoverDelay,
                staminaRecoverSpeed,
                guardAngle)
        {
        }

        protected override void OnUnitResourceEnable()
        {
            Stamina.Reset();
        }
    }
}
