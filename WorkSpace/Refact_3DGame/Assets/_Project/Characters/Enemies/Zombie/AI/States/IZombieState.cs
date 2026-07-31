namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    internal interface IZombieState
    {
        string Name { get; }
        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}
