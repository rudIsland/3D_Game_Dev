using System;

namespace World
{
    // 월드 객체의 생성·활성화·갱신·비활성화·제거 순서를 관리한다.
    public abstract class WorldObject : IWorldObject
    {
        // 객체가 완전히 제거되었는지 기록한다.
        private bool isDisposed;

        // 최초 생성 작업이 끝났는지 알려준다.
        public bool IsCreated { get; private set; }
        // 현재 객체가 활성화되어 있는지 알려준다.
        public bool IsEnabled { get; private set; }

        // 객체를 최초 한 번만 생성하고 준비 작업을 실행한다.
        public void Create()
        {
            // 이미 생성되었거나 제거된 객체는 다시 만들지 않는다.
            if (IsCreated || isDisposed)
            {
                return;
            }

            IsCreated = true;
            OnCreate();
        }

        // 생성된 객체를 사용 중인 상태로 바꾸고 활성화 작업을 실행한다.
        public void Enable()
        {
            // 제거된 객체는 다시 사용할 수 없다.
            if (isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // 생성 전에 활성화하면 호출 순서를 잘못 사용한 것이다.
            if (!IsCreated)
            {
                throw new InvalidOperationException("WorldObject.Create()를 먼저 호출해야 합니다.");
            }

            // 이미 활성화된 객체는 중복 처리하지 않는다.
            if (IsEnabled)
            {
                return;
            }

            IsEnabled = true;
            OnEnable();
        }

        // 활성 상태인 객체의 갱신 작업을 실행한다.
        public void Tick(float deltaTime)
        {
            if (!IsEnabled || isDisposed)
            {
                return;
            }

            OnTick(deltaTime);
        }

        // 사용 중인 객체를 비활성화하고 중복 호출은 무시한다.
        public void Disable()
        {
            if (!IsEnabled)
            {
                return;
            }

            IsEnabled = false;
            OnDisable();
        }

        // 객체를 비활성화한 뒤 마지막 정리 작업을 한 번만 실행한다.
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

        // 자식 클래스가 각 생명주기 단계에서 필요한 작업을 작성하는 지점이다.
        // 자식 클래스가 최초 생성 시 필요한 작업을 작성하는 지점이다.
        protected virtual void OnCreate()
        {
        }

        // 자식 클래스가 활성화될 때 필요한 작업을 작성하는 지점이다.
        protected virtual void OnEnable()
        {
        }

        // 자식 클래스가 매 프레임 갱신할 작업을 작성하는 지점이다.
        protected virtual void OnTick(float deltaTime)
        {
        }

        // 자식 클래스가 비활성화될 때 필요한 작업을 작성하는 지점이다.
        protected virtual void OnDisable()
        {
        }

        // 자식 클래스가 완전히 제거될 때 필요한 작업을 작성하는 지점이다.
        protected virtual void OnDispose()
        {
        }
    }
}
