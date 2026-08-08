using System;

namespace rudIsland.RPG3D.World
{
    // 월드 객체가 따라야 하는 공통 생명주기 규칙이다.
    public interface IWorldObject : IDisposable
    {
        // 최초 생성 작업이 끝났는지 알려준다.
        bool IsCreated { get; }

        // 현재 사용 중인 상태인지 알려준다.
        bool IsEnabled { get; }

        // 최초 준비 작업을 한 번 실행한다.
        void Create();

        // 객체를 사용 가능한 상태로 만든다.
        void Enable();

        // 활성 객체의 게임 로직을 한 번 갱신한다.
        void Tick(float deltaTime);

        // 객체 사용을 멈춘다.
        void Disable();
    }
}