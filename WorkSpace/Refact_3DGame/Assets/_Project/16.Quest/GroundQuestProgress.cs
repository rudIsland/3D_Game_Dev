using System;

namespace Quest
{
    public enum GroundQuestStep
    {
        FindBook,
        ExchangeBook,
        ReachExit,
        Complete
    }

    // 씬과 UI를 모르고, 지상 퀘스트의 완료 순서만 관리한다.
    public sealed partial class GroundQuestProgress
    {
        public GroundQuestStep Step { get; private set; }
        public event Action Changed;

        public void RecordBookFound()
        {
            if (Step == GroundQuestStep.FindBook)
            {
                SetStep(GroundQuestStep.ExchangeBook);
            }
        }

        public void RecordBookExchanged()
        {
            if (Step < GroundQuestStep.ReachExit)
            {
                SetStep(GroundQuestStep.ReachExit);
            }
        }

        public void RecordExitReached()
        {
            if (Step == GroundQuestStep.ReachExit)
            {
                SetStep(GroundQuestStep.Complete);
            }
        }

        private void SetStep(GroundQuestStep step)
        {
            Step = step;
            Changed?.Invoke();
        }
    }
}
