namespace Characters
{
    // 플레이어 유닛의 공통 타입과 체력 생성을 제공한다.
    public abstract class PlayerUnit : Unit
    {
        // 플레이어의 최대 체력으로 기본 유닛을 만든다.
        protected PlayerUnit(float maxHealth)
            : base(maxHealth)
        {
        }
    }
}