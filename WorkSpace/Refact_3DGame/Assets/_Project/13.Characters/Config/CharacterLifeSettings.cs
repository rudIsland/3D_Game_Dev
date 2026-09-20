namespace Characters
{
    // 공유 가능한 기준값만 보관한다. 현재 체력과 경직 누적량은 개체가 소유한다.
    internal sealed class CharacterLifeSettings
    {
        internal float MaxHealth { get; }
        internal float StaggerLimit { get; }
        internal float StaggerRecoverDelay { get; }
        internal float StaggerRecoverSpeed { get; }

        internal CharacterLifeSettings(float maxHealth, float staggerLimit,
            float staggerRecoverDelay, float staggerRecoverSpeed)
        {
            MaxHealth = maxHealth;
            StaggerLimit = staggerLimit;
            StaggerRecoverDelay = staggerRecoverDelay;
            StaggerRecoverSpeed = staggerRecoverSpeed;
        }
    }
}
