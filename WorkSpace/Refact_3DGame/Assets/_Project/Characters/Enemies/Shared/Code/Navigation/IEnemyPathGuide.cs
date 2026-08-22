using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Navigation
{
    // 적의 실제 이동 방식과 경로 계산 방식을 분리한다.
    internal interface IEnemyPathGuide
    {
        bool TryGetMoveDirection(
            Vector3 targetPosition,
            float deltaTime,
            out Vector3 moveDirection);

        void Stop();
        void Reset();
    }
}
