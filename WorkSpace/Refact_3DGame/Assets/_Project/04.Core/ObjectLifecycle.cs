using System;

namespace Core
{
    // 객체 하나의 생명주기 순서와 중복 호출을 제어한다. 구체적인 자원과 행동은 각 객체가 소유한다.
    public abstract class ObjectLifecycle
    {
        /// <summary>초기 참조와 상태 준비를 시작했는지 알려준다.</summary>
        public bool IsInitialized { get; private set; }
        /// <summary>내부 객체·자원 생성을 시작했는지 알려준다.</summary>
        public bool IsCreated { get; private set; }
        /// <summary>현재 갱신할 수 있는 활성 상태인지 알려준다.</summary>
        public bool IsEnabled { get; private set; }
        /// <summary>소유 자원 해제를 시작했는지 알려준다.</summary>
        public bool IsReleased { get; private set; }

        /// <summary>초기화한다. 실패 시 부분 초기화 자원 정리는 호출자가 담당한다.</summary>
        public void Init()
        {
            CheckUsable();
            if (IsInitialized) return;
            IsInitialized = true;
            OnInit();
        }

        /// <summary>초기화에서 준비한 참조·설정으로 내부 객체와 자원을 한 번 생성한다.</summary>
        public void Create()
        {
            CheckUsable();
            if (!IsInitialized) throw new InvalidOperationException("Init()을 먼저 호출하세요.");
            if (IsCreated) return;
            IsCreated = true;
            OnCreate();
        }

        /// <summary>생성한 객체를 활성화한다. 실패 시 비활성화는 호출자가 담당한다.</summary>
        public void Enable()
        {
            CheckUsable();
            if (!IsCreated) throw new InvalidOperationException("Create()를 먼저 호출하세요.");
            if (IsEnabled) return;
            IsEnabled = true;
            OnEnable();
        }

        /// <summary>소유자가 전달한 프레임 시간으로 활성 객체를 갱신한다. 필요 없는 객체는 호출하지 않는다.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsEnabled || IsReleased) return;
            OnTick(deltaTime);
        }

        /// <summary>동작을 멈추고 재활성화에 필요한 자원은 유지한다.</summary>
        public void Disable()
        {
            if (!IsEnabled) return;
            IsEnabled = false;
            OnDisable();
        }

        /// <summary>소유 자원과 구독을 한 번 정리한다. 먼저 Disable해야 하며 이후 재실행은 허용하지 않는다.</summary>
        public void Release()
        {
            if (IsReleased) return;
            if (IsEnabled) throw new InvalidOperationException("Disable()을 먼저 호출하세요.");
            IsReleased = true;
            OnRelease();
        }

        // 해제한 자원을 다시 사용하는 초기화·생성·활성화 요청을 거절한다.
        private void CheckUsable()
        {
            if (IsReleased) throw new ObjectDisposedException(GetType().Name);
        }

        // 초기화
        protected virtual void OnInit() { }
        // 생성
        protected virtual void OnCreate() { }
        // 활성화
        protected virtual void OnEnable() { }
        // 갱신
        protected virtual void OnTick(float deltaTime) { }
        // 비활성화
        protected virtual void OnDisable() { }
        // 자원 정리
        protected virtual void OnRelease() { }
    }
}
