using UnityEngine;

namespace Characters.Player.Movement
{
    // 자유시점과 타깃시점의 방향·회전 규칙을 정의한다.
    internal interface IPlayerMovementMode
    {
        Vector3 GetMoveDirection(Vector2 moveInput);
        Vector2 GetRollDirection(Vector2 moveInput);
        Vector3 GetAttackDirection();
        void UpdateFacing(Vector3 moveDirection, float deltaTime);
    }
}
