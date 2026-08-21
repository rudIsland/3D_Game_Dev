using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal sealed class NightShadeSwordComboAction : NightShadeSwordAttackActionBase
    {
        private float connectionElapsedTime;

        public override NightShadeSwordActionId ActionId => NightShadeSwordActionId.Combo;
        protected override NightShadeSwordAttackType FirstAttackType => NightShadeSwordAttackType.ComboFirst;

        internal NightShadeSwordComboAction(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordActions actions)
            : base(
                situation,
                fightMemory,
                movement,
                animation,
                settings,
                actions)
        {
        }

        public override void Enter()
        {
            base.Enter();
            connectionElapsedTime = 0f;
            FightMemory.SetComboStep(NightShadeSwordComboStep.ComboFirst);
        }

        public override void Update(float deltaTime)
        {
            switch (FightMemory.ComboStep)
            {
                case NightShadeSwordComboStep.ComboFirst:
                    UpdateFirstAttack(deltaTime);
                    break;
                case NightShadeSwordComboStep.Connecting:
                    UpdateConnection(deltaTime);
                    break;
                case NightShadeSwordComboStep.ComboSecond:
                    base.Update(deltaTime);
                    break;
            }
        }

        private void UpdateFirstAttack(float deltaTime)
        {
            ProcessQueuedEvents();
            UpdateAttackMovement(deltaTime);
            if (!HasCurrentAnimationFinished(
                    Settings.ComboFirstExitNormalizedTime))
            {
                return;
            }

            ClearQueuedEvents();
            CloseOpenHit();
            Animation.ResetAttackPlaybackSpeed();
            connectionElapsedTime = 0f;
            FightMemory.SetComboStep(NightShadeSwordComboStep.Connecting);
        }

        private void UpdateConnection(float deltaTime)
        {
            Movement.StayOnGround(deltaTime);
            if (!Situation.IsTargetDetected ||
                !Situation.IsInsideAttackRange)
            {
                IsFinished = true;
                return;
            }

            connectionElapsedTime += Mathf.Max(0f, deltaTime);
            if (connectionElapsedTime < Settings.ComboSecondDelay)
            {
                return;
            }

            FightMemory.SetComboStep(NightShadeSwordComboStep.ComboSecond);
            StartAttackClip(NightShadeSwordAttackType.ComboSecond);
        }
    }
}
