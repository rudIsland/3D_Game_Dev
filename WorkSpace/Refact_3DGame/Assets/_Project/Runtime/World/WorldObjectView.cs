using System;
using UnityEngine;

namespace rudIsland.RPG3D.World
{
    // Unity 컴포넌트와 일반 C# 월드 객체를 연결하는 경계다.
    public abstract class WorldObjectView : MonoBehaviour
    {
        private WorldObjectManager manager; // 씬 또는 시스템 참조

        // 실제 게임 규칙은 MonoBehaviour가 아닌 일반 C# 객체가 담당한다.
        public IWorldObject RuntimeObject { get; private set; } // 시간 설정

        internal WorldObjectPool OwnerPool { get; private set; } // 씬 또는 시스템 참조
        internal bool IsTakenFromPool { get; set; } // 기능 사용 여부
        internal bool IsWaitingForDespawn { get; set; } // 기능 사용 여부

        // 아이템 획득이나 적 사망처럼 뷰에서 회수가 필요할 때 호출한다.
        public void RequestDespawn()
        {
            manager?.Despawn(this);
        }

        // 풀에서 처음 만든 뷰에 Manager, Pool, RuntimeObject를 한 번 연결한다.
        internal void Prepare(
            WorldObjectManager objectManager,
            WorldObjectPool objectPool)
        {
            manager = objectManager;
            OwnerPool = objectPool;

            if (RuntimeObject != null)
            {
                return;
            }

            RuntimeObject = CreateRuntimeObject();

            if (RuntimeObject == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}.CreateRuntimeObject()가 null을 반환했습니다.");
            }
        }

        // 풀 반환 전에 Animator, Collider 등을 자식 뷰가 초기화한다.
        internal void ResetForPool()
        {
            OnResetForPool();
        }

        protected abstract IWorldObject CreateRuntimeObject();

        protected virtual void OnResetForPool()
        {
        }
    }
}
