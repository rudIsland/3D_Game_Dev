namespace rudIsland.RPG3D.Player.States.Attack
{
    // 일반 콤보 1타의 공격 설정을 가진 상태다.
    internal sealed class PlayerComboAttack01State : IAttackState
    {
        private readonly PlayerAttackData attackData;

        public PlayerComboAttack01State(PlayerAttackData attackData)
        {
            this.attackData = attackData;
        }

        public int AttackNumber => attackData.AttackNumber;
        public float Damage => attackData.Damage;
        public float NextInputTime => attackData.NextInputTime;
        public float MoveScale => attackData.MoveScale;
    }
}
