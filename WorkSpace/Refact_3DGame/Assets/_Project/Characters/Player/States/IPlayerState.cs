namespace rudIsland.RPG3D.Player.States
{
    internal interface IPlayerState
    {
        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}
