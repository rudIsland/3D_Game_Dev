using System;
using UnityEngine;

namespace Characters
{
    // 구체적인 유닛 종류를 몰라도 사망 여부를 확인하게 한다.
    public interface IUnitDeathState
    {
        // 유닛이 죽었는지 알려준다.
        bool IsDead { get; }
    }

    // 모든 유닛의 체력과 공통 생명주기를 관리한다.
    public abstract class Unit : IDisposable, IUnitDeathState
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
                throw new InvalidOperationException("Unit.Create()를 먼저 호출해야 합니다.");
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


        // 유닛의 현재 체력과 최대 체력을 관리한다.
        public UnitHealth Health { get; }

        // 풀에서 활성화된 횟수를 구분하기 위한 번호다.
        public int ActivationSequence { get; private set; }

        // 유닛의 체력이 0인지 알려준다.
        public bool IsDead => Health.IsDead;

        // 유닛의 체력 객체를 만든다.
        protected Unit(float maxHealth)
        {
            Health = new UnitHealth(maxHealth);
        }

        // 최초 생성 시 유닛 전용 생성 작업을 호출한다.
        private void OnCreate()
        {
            OnUnitCreate();
        }

        // 활성화 횟수를 올리고 유닛 전용 활성화 작업을 호출한다.
        private void OnEnable()
        {
            IncreaseActivationSequence();
            OnUnitResourceEnable();
            OnUnitEnable();
        }

        // 매 프레임 유닛 전용 갱신 작업을 호출한다.
        private void OnTick(float deltaTime)
        {
            OnUnitTick(deltaTime);
        }

        // 비활성화 시 유닛 전용 정리 작업을 호출한다.
        private void OnDisable()
        {
            OnUnitDisable();
        }

        // 제거 시 유닛 전용 정리와 체력 이벤트 해제를 처리한다.
        private void OnDispose()
        {
            OnUnitDispose();
            Health.ClearListeners();
        }

        // 자식 유닛이 최초 생성 시 필요한 작업을 작성하는 지점이다.
        protected virtual void OnUnitCreate()
        {
        }

        // 자식 유닛이 활성화될 때 자원을 준비하는 지점이다.
        protected virtual void OnUnitResourceEnable()
        {
        }

        // 자식 유닛이 활성화될 때 시작 작업을 작성하는 지점이다.
        protected virtual void OnUnitEnable()
        {
        }

        // 자식 유닛의 매 프레임 갱신 작업을 작성하는 지점이다.
        protected virtual void OnUnitTick(float deltaTime)
        {
        }

        // 자식 유닛이 비활성화될 때 작업을 작성하는 지점이다.
        protected virtual void OnUnitDisable()
        {
        }

        // 자식 유닛이 완전히 제거될 때 작업을 작성하는 지점이다.
        protected virtual void OnUnitDispose()
        {
        }

        // 활성화될 때마다 풀 재사용 번호를 하나 올린다.
        private void IncreaseActivationSequence()
        {
            ActivationSequence = ActivationSequence == int.MaxValue
                ? 1
                : ActivationSequence + 1;
        }
    }
}