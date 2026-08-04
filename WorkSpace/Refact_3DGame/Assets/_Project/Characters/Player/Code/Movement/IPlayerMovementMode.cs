using UnityEngine;

namespace rudIsland.RPG3D.Player.Movement
{
    // 이동 모드마다 입력 방향 계산과 플레이어 회전 방식만 교체한다.
    internal interface IPlayerMovementMode
    {
        Vector3 GetMoveDirection(Vector2 moveInput);
        void UpdateFacing(Vector3 moveDirection, float deltaTime);
    }
}
