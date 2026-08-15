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
        public float StaggerDamage => attackData.StaggerDamage;
        public float PushDistance => attackData.PushDistance;
        public float HitStopDuration => attackData.HitStopDuration;
        public float StaminaCost => attackData.StaminaCost;
        public float NextInputTime => attackData.NextInputTime;
        public float MoveDistance => attackData.MoveDistance;
        public UnityEngine.AnimationCurve MovementCurve =>
            attackData.MovementCurve;
    }
}
