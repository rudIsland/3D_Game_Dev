namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    internal interface IZombieState
    {
        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}
