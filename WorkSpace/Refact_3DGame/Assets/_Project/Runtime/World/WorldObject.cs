using System;

namespace rudIsland.RPG3D.World
{
    // 월드 객체의 공통 호출 순서를 한곳에서 지킨다.
    public abstract class WorldObject : IWorldObject
    {
        private bool isDisposed;

        public bool IsCreated { get; private set; }
        public bool IsEnabled { get; private set; }

        // 최초 한 번만 필요한 준비 작업을 실행한다.
        public void Create()
        {
            if (IsCreated || isDisposed)
            {
                return;
            }

            IsCreated = true;
            OnCreate();
        }

        // Create가 끝난 객체만 사용할 수 있는 상태로 바꾼다.
        public void Enable()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!IsCreated)
            {
                throw new InvalidOperationException(
                    "WorldObject.Create()를 먼저 호출해야 합니다.");
            }

            if (IsEnabled)
            {
                return;
            }

            IsEnabled = true;
            OnEnable();
        }

        // 활성 상태일 때만 자식 객체의 갱신 로직을 실행한다.
        public void Tick(float deltaTime)
        {
            if (!IsEnabled || isDisposed)
            {
                return;
            }

            OnTick(deltaTime);
        }

        // 사용 중인 객체를 멈추며 중복 호출은 무시한다.
        public void Disable()
        {
            if (!IsEnabled)
            {
                return;
            }

            IsEnabled = false;
            OnDisable();
        }

        // 비활성화한 뒤 마지막 정리 작업을 한 번만 실행한다.
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            Disable();
            isDisposed = true;
            OnDispose();
        }

        // 자식 클래스는 공개 생명주기 대신 아래 메서드만 필요한 만큼 구현한다.
        protected virtual void OnCreate()
        {
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnTick(float deltaTime)
        {
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnDispose()
        {
        }
    }
}
