namespace rudIsland.RPG3D.Player.States.Target
{
    // 잠깐 가려진 락온 대상을 즉시 놓치지 않도록 시간을 잰다.
    internal sealed class PlayerTargetVisibilityGrace
    {
        private const float TimeComparisonTolerance = 0.000001f;
        private readonly float duration;
        private float hiddenDuration;

        internal PlayerTargetVisibilityGrace(float duration)
        {
            this.duration = duration > 0f ? duration : 0f;
        }

        internal bool CanKeepTarget(bool isVisible, float deltaTime)
        {
            if (isVisible)
            {
                Reset();
                return true;
            }

            hiddenDuration += deltaTime > 0f ? deltaTime : 0f;
            return hiddenDuration <=
                duration + TimeComparisonTolerance;
        }

        internal void Reset()
        {
            hiddenDuration = 0f;
        }
    }
}
