namespace rudIsland.RPG3D.Combat.Attack
{
    public enum AttackPhase
    {
        Ready = 0,
        Hit = 1,
        Recovery = 2
    }

    // 공격 시작부터 타격과 후딜까지 현재 구간을 한곳에서 관리한다.
    public sealed class AttackPhaseTracker
    {
        public bool IsAttackActive { get; private set; }
        public AttackPhase CurrentPhase { get; private set; }
        public bool CanTurn =>
            IsAttackActive &&
            CurrentPhase == AttackPhase.Ready;

        public void BeginAttack()
        {
            IsAttackActive = true;
            CurrentPhase = AttackPhase.Ready;
        }

        public bool BeginHit()
        {
            if (!IsAttackActive ||
                CurrentPhase != AttackPhase.Ready)
            {
                return false;
            }

            CurrentPhase = AttackPhase.Hit;
            return true;
        }

        public bool BeginRecovery()
        {
            if (!IsAttackActive ||
                CurrentPhase == AttackPhase.Recovery)
            {
                return false;
            }

            CurrentPhase = AttackPhase.Recovery;
            return true;
        }

        public void EndAttack()
        {
            IsAttackActive = false;
            CurrentPhase = AttackPhase.Ready;
        }
    }
}
