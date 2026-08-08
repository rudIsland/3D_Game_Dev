namespace rudIsland.RPG3D.Player.States.Attack
{
    // 달리기 공격의 공격 설정을 가진 상태다.
    internal sealed class PlayerRunAttackState : IAttackState
    {
        public PlayerRunAttackState(float moveScale)
        {
            MoveScale = moveScale;
        }

        public int AttackNumber => 6;
        public float NextInputTime => 1f;
        public float MoveScale { get; }
    }
}
