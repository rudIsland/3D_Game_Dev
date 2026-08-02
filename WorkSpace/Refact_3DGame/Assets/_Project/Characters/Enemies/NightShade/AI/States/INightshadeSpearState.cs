namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 모든 Nightshade 상태가 따르는 최소 생명주기다.
    internal interface INightshadeSpearState
    {
        string Name { get; }
        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}
