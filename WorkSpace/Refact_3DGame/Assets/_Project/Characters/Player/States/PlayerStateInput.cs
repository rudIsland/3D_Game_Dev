namespace rudIsland.RPG3D.Player.States
{
    // 이번 프레임에 상태가 판단할 입력만 전달한다.
    internal readonly struct PlayerStateInput
    {
        public bool RollPressed { get; } // 기능 사용 여부
        public bool AttackPressed { get; } // 기능 사용 여부
        public bool IsBlocking { get; } // 기능 사용 여부

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