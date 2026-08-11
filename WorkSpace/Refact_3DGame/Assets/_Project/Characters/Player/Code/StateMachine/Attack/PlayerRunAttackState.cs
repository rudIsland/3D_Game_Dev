namespace rudIsland.RPG3D.Player.States.Attack
{
    // 달리기 공격의 공격 설정을 가진 상태다.
    internal sealed class PlayerRunAttackState : IAttackState
    {
        private readonly PlayerAttackData attackData;

        public PlayerRunAttackState(PlayerAttackData attackData)
        {
            this.attackData = attackData;
        }

        public int AttackNumber => attackData.AttackNumber;
        public float Damage => attackData.Damage;
        public float NextInputTime => attackData.NextInputTime;
        public float MoveScale => attackData.MoveScale;
    }
}
