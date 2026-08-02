namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    // 상태 머신이 모든 보스 행동을 같은 순서로 실행하기 위한 약속이다.
    internal interface IDemonSwordsmanState
    {
        string Name { get; }
        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}
