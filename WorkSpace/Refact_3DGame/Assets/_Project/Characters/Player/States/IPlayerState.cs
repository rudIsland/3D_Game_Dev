namespace rudIsland.RPG3D.Player.States
{
    internal interface IPlayerState
    {
        void Enter();
        void Update(
            float deltaTime,
            PlayerStateInput input);
        void Exit();
    }
}
