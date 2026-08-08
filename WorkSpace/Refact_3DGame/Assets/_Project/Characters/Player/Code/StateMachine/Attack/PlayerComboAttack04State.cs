namespace rudIsland.RPG3D.Player.States.Attack
{
    // 일반 콤보 4타의 공격 설정을 가진 상태다.
    internal sealed class PlayerComboAttack04State : IAttackState
    {
        public PlayerComboAttack04State(
            float nextInputTime,
            float moveScale)
        {
            NextInputTime = nextInputTime;
            MoveScale = moveScale;
        }

        public int AttackNumber => 4;
        public float NextInputTime { get; }
        public float MoveScale { get; }
    }
}
