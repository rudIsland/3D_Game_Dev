namespace rudIsland.RPG3D.Player.States.Actions
{
    internal enum PlayerBufferedAction
    {
        None = 0,
        Attack = 1,
        Roll = 2
    }

    // 공격과 구르기 중 마지막으로 누른 행동 하나를 짧게 보관한다.
    internal sealed class PlayerActionInputBuffer
    {
        private readonly float duration;
        private float age;

        internal PlayerBufferedAction CurrentAction { get; private set; }

        internal PlayerActionInputBuffer(float duration)
        {
            this.duration = duration > 0f ? duration : 0f;
        }

        internal void Update(
            float deltaTime,
            bool rollPressed,
            bool attackPressed)
        {
            if (CurrentAction != PlayerBufferedAction.None)
            {
                age += deltaTime > 0f ? deltaTime : 0f;
                if (age > duration)
                {
                    Clear();
                }
            }

            // 같은 프레임에 둘 다 눌리면 생존 행동인 구르기를 우선한다.
            if (rollPressed)
            {
                Reserve(PlayerBufferedAction.Roll);
            }
            else if (attackPressed)
            {
                Reserve(PlayerBufferedAction.Attack);
            }
        }

        internal bool TryTake(PlayerBufferedAction action)
        {
            if (CurrentAction != action)
            {
                return false;
            }

            Clear();
            return true;
        }

        internal void Clear()
        {
            CurrentAction = PlayerBufferedAction.None;
            age = 0f;
        }

        private void Reserve(PlayerBufferedAction action)
        {
            CurrentAction = action;
            age = 0f;
        }
    }
}
