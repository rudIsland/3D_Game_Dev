namespace rudIsland.RPG3D.Player.States.Attack
{
    // 일반 콤보 5타의 공격 설정을 가진 상태다.
    internal sealed class PlayerComboAttack05State : IAttackState
    {
        public PlayerComboAttack05State(float moveScale)
        {
            MoveScale = moveScale;
        }

        public int AttackNumber => 5;
        public float NextInputTime => 1f;
        public float MoveScale { get; }
    }
}
