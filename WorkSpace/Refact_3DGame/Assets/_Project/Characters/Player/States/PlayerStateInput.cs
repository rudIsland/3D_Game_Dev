namespace rudIsland.RPG3D.Player.States
{
    // 이번 프레임에 상태가 판단할 입력만 전달한다.
    internal readonly struct PlayerStateInput
    {
        public bool RollPressed { get; }
        public bool AttackPressed { get; }
        public bool IsBlocking { get; }

        public PlayerStateInput(
            bool rollPressed,
            bool attackPressed,
            bool isBlocking)
        {
            RollPressed = rollPressed;
            AttackPressed = attackPressed;
            IsBlocking = isBlocking;
        }
    }
}