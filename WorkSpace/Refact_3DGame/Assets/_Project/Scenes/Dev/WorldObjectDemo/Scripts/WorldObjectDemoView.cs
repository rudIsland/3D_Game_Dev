using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Dev.WorldDemo
{
    // 중앙 Tick과 풀 재사용을 눈으로 확인하기 위한 테스트용 뷰다.
    public sealed class WorldObjectDemoView : WorldObjectView
    {
        [SerializeField] private float turnSpeed = 45f; // 이동 속도

        // 이 뷰가 사용할 일반 C# 월드 객체를 최초 한 번 만든다.
        protected override IWorldObject CreateRuntimeObject()
        {
            return new RotatingWorldObject(transform, turnSpeed);
        }

        // 풀에 돌아갈 때 다음 사용을 위해 회전값을 초기화한다.
        protected override void OnResetForPool()
        {
            transform.localRotation = Quaternion.identity;
        }

        // Manager가 Tick할 때 연결된 Transform을 천천히 회전시킨다.
        private sealed class RotatingWorldObject : WorldObject
        {
            private readonly Transform target; // 대상 참조
            private readonly float turnSpeed; // 이동 속도

            public RotatingWorldObject(Transform target, float turnSpeed)
            {
                this.target = target;
                this.turnSpeed = turnSpeed;
            }

            protected override void OnTick(float deltaTime)
            {
                target.Rotate(
                    0f,
                    turnSpeed * deltaTime,
                    0f,
                    Space.World);
            }
        }
    }
}
