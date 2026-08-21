// 이전 선택과 공격 후딜처럼 Tick 사이에 유지할 전투 기록이다.
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal enum NightShadeSwordComboStep
    {
        None = 0,
        ComboFirst = 1,
        Connecting = 2,
        ComboSecond = 3
    }

    // 한 번의 활성화 동안 공격, Recovery와 콤보 진행 기록을 보관한다.
    internal sealed class NightShadeSwordFightMemory
    {
        internal bool HasPreviousAttack { get; private set; }
        internal NightShadeSwordActionId PreviousAttack { get; private set; }
        internal bool HasPreviousRecovery { get; private set; }
        internal NightShadeSwordActionId PreviousRecovery { get; private set; }
        internal float RemainingPostAttackDelay { get; private set; }
        internal NightShadeSwordComboStep ComboStep { get; private set; }
        internal NightShadeSwordActionId RecentSelection { get; private set; }

        internal void Reset()
        {
            HasPreviousAttack = false;
            PreviousAttack = NightShadeSwordActionId.None;
            HasPreviousRecovery = false;
            PreviousRecovery = NightShadeSwordActionId.None;
            RemainingPostAttackDelay = 0f;
            ComboStep = NightShadeSwordComboStep.None;
            RecentSelection = NightShadeSwordActionId.None;
        }

        internal void UpdatePostAttackDelay(float deltaTime)
        {
            if (RemainingPostAttackDelay > 0f)
            {
                RemainingPostAttackDelay = Mathf.Max(
                    0f,
                    RemainingPostAttackDelay - deltaTime);
            }
        }

        internal void RecordAttack(NightShadeSwordActionId actionId)
        {
            PreviousAttack = actionId;
            HasPreviousAttack = true;
            RecentSelection = actionId;
        }

        internal void RecordRecovery(NightShadeSwordActionId actionId)
        {
            PreviousRecovery = actionId;
            HasPreviousRecovery = true;
            RecentSelection = actionId;
        }

        internal void StartPostAttackDelay(float postAttackDelay)
        {
            RemainingPostAttackDelay = Mathf.Max(0f, postAttackDelay);
        }

        internal void SetComboStep(NightShadeSwordComboStep step)
        {
            ComboStep = step;
        }

        internal void ClearCombo()
        {
            ComboStep = NightShadeSwordComboStep.None;
        }
    }
}
