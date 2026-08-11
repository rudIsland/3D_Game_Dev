namespace rudIsland.RPG3D.Player.States.Attack
{
    // 일반 콤보 3타의 공격 설정을 가진 상태다.
    internal sealed class PlayerComboAttack03State : IAttackState
    {
        private readonly PlayerAttackData attackData;

        public PlayerComboAttack03State(PlayerAttackData attackData)
        {
            this.attackData = attackData;
        }

        public int AttackNumber => attackData.AttackNumber;
        public float Damage => attackData.Damage;
        public float NextInputTime => attackData.NextInputTime;
        public float MoveScale => attackData.MoveScale;
    }
}
