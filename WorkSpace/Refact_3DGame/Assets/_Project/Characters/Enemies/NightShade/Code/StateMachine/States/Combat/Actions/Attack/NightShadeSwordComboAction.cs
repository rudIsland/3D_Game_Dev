using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordComboAction : NightShadeSwordAttackActionBase
    {
        private enum ComboStep
        {
            FirstAttack = 0,
            Connecting = 1,
            SecondAttack = 2
        }

        private float connectionElapsedTime;
        private ComboStep comboStep;

        public override NightShadeSwordActionId ActionId => NightShadeSwordActionId.Combo;
        protected override NightShadeSwordAttackType FirstAttackType => NightShadeSwordAttackType.ComboFirst;

        internal NightShadeSwordComboAction(
            NightShadeSwordBehaviorContext context,
            NightShadeSwordRuntimeAttackData attackData,
            NightShadeSwordAttackSelectionRuntimeConfig attackSelection,
            NightShadeSwordCombatOutput combatOutput)
            : base(
                context,
                attackData,
                attackSelection,
                combatOutput)
        {
        }

        public override void Enter()
        {
            base.Enter();
            connectionElapsedTime = 0f;
            comboStep = ComboStep.FirstAttack;
        }

        public override void Update(float deltaTime)
        {
            switch (comboStep)
            {
                case ComboStep.FirstAttack:
                    UpdateFirstAttack(deltaTime);
                    break;
                case ComboStep.Connecting:
                    UpdateConnection(deltaTime);
                    break;
                case ComboStep.SecondAttack:
                    base.Update(deltaTime);
                    break;
            }
        }

        private void UpdateFirstAttack(float deltaTime)
        {
            ProcessQueuedEvents();
            UpdateAttackMovement(deltaTime);
            if (!HasCurrentAnimationFinished(
                    AttackData.ComboFirstExitNormalizedTime))
            {
                return;
            }

            ClearQueuedEvents();
            CloseOpenHit();
            Animation.ResetAttackPlaybackSpeed();
            connectionElapsedTime = 0f;
            comboStep = ComboStep.Connecting;
        }

        private void UpdateConnection(float deltaTime)
        {
            Movement.StayOnGround(deltaTime);
            if (!TargetStatus.IsDetected ||
                !TargetStatus.IsInsideAttackRange)
            {
                IsFinished = true;
                return;
            }

            connectionElapsedTime += Mathf.Max(0f, deltaTime);
            if (connectionElapsedTime < AttackData.ComboSecondDelay)
            {
                return;
            }

            comboStep = ComboStep.SecondAttack;
            StartAttackClip(NightShadeSwordAttackType.ComboSecond);
        }
    }
}
