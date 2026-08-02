namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    // 검과 맨손 전투 스타일을 바꾸는 행동을 담당한다.
    internal sealed class DemonSwordsmanStyleChangeState :
        IDemonSwordsmanState
    {
        private const float LocomotionFadeTime = 0.15f;
        private readonly DemonSwordsmanStateMachine stateMachine;
        private DemonSwordsmanStyle nextStyle;
        private bool weaponChanged;

        public string Name =>
            nameof(DemonSwordsmanActionState.StyleChange);

        internal DemonSwordsmanStyleChangeState(
            DemonSwordsmanStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            stateMachine.Movement.SetAttackRootMove(false, 1f);
            stateMachine.Animation.SetAnimationSpeed(1f);
            nextStyle =
                stateMachine.Style == DemonSwordsmanStyle.Sword
                    ? DemonSwordsmanStyle.Beast
                    : DemonSwordsmanStyle.Sword;
            weaponChanged = false;
            stateMachine.Animation.PlayStyleChange(nextStyle);
        }

        public void Update(float deltaTime)
        {
            stateMachine.Movement.Stop(deltaTime);

            if (stateMachine.Target.HasTarget)
            {
                stateMachine.Movement.TurnTo(
                    stateMachine.Target.Position,
                    stateMachine.GetCurrentTurnSpeed(),
                    deltaTime);
            }

            stateMachine.UpdateMoveAnimation(deltaTime);

            if (!weaponChanged &&
                stateMachine.StateTime >=
                    stateMachine.Settings.StyleChangeTime * 0.5f)
            {
                weaponChanged = true;
                stateMachine.ChangeStyle(nextStyle);
                stateMachine.Animation.ShowStyle(stateMachine.Style);
            }

            if (stateMachine.StateTime <
                stateMachine.Settings.StyleChangeTime)
            {
                return;
            }

            stateMachine.ChangeStyle(nextStyle);
            stateMachine.ResetStyleActionCount();
            stateMachine.Animation.ShowStyle(stateMachine.Style);
            stateMachine.Animation.PlayLocomotion(
                stateMachine.Style,
                LocomotionFadeTime);
            stateMachine.ChangeToMove(
                DemonSwordsmanMoveMode.Approach);
        }

        public void Exit()
        {
        }
    }
}
