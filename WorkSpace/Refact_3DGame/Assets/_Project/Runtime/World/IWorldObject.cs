using System;

namespace rudIsland.RPG3D.World
{
    // 씬에 존재하는 객체가 따라야 하는 공통 생명주기다.
    public interface IWorldObject : IDisposable
    {
        bool IsCreated { get; } //생성 여부
        bool IsEnabled { get; } //활성화 여부

        // 최초 준비 → 사용 시작 → 매 프레임 갱신 → 사용 중지 순서로 호출한다.
        void Create();  //생성
        void Enable();  //활성화
        void Tick(float deltaTime); //갱신
        void Disable(); //비활성화

        //void Dispose();
    }
}
