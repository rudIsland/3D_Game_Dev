namespace rudIsland.RPG3D.Player.States.Attack
{
    // 일반 콤보 2타의 공격 설정을 가진 상태다.
    internal sealed class PlayerComboAttack02State : IAttackState
    {
        private readonly PlayerAttackData attackData;

        public PlayerComboAttack02State(PlayerAttackData attackData)
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
