using System;
using UnityEngine;

namespace rudIsland.RPG3D.World
{
    // Unity GameObject와 일반 C# 월드 객체를 연결한다.
    public abstract class WorldObjectView : MonoBehaviour
    {
        // 뷰의 생성과 회수를 맡은 관리자다.
        private WorldObjectManager manager;

        // 실제 게임 규칙은 MonoBehaviour가 아닌 일반 C# 객체가 담당한다.
        // 실제 게임 규칙을 실행하는 일반 C# 객체다.
        public IWorldObject RuntimeObject { get; private set; }

        // 이 뷰가 속한 객체 풀이다.
        internal WorldObjectPool OwnerPool { get; private set; }
        // 현재 풀이 빌려서 사용 중인지 알려준다.
        internal bool IsTakenFromPool { get; set; }
        // 회수 요청을 이미 등록했는지 알려준다.
        internal bool IsWaitingForDespawn { get; set; }

        // 사용이 끝난 뷰를 풀로 돌려보내도록 요청한다.
        public void RequestDespawn()
        {
            manager?.Despawn(this);
        }

        // 처음 생성된 뷰에 관리자, 풀, RuntimeObject를 연결한다.
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

        // 풀에 반환하기 전에 자식 뷰의 Unity 상태를 초기화한다.
        internal void ResetForPool()
        {
            OnResetForPool();
        }

        // 뷰에 연결할 실제 게임 규칙 객체를 만든다.
        protected abstract IWorldObject CreateRuntimeObject();

        // 자식 뷰가 풀 반환 전 초기화 작업을 작성하는 지점이다.
        protected virtual void OnResetForPool()
        {
        }
    }
}
