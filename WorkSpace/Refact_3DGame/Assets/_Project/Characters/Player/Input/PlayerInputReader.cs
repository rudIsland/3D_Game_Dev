using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace rudIsland.RPG3D.Player.Input
{
    // Input System 값을 게임에서 읽기 쉬운 상태로 저장한다.
    public sealed class PlayerInputReader : PlayerControls.IPlayerActions
    {
        private PlayerControls playerControls;

        // 누르고 있는 동안 계속 유지되는 입력 상태다.
        public Vector2 MoveValue { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsBlocking { get; private set; }

        // 버튼을 한 번 눌렀을 때 Update에서 한 번만 사용한다.
        private bool hasRollInput;
        private bool hasBlockImpactTestInput;

        // Input Actions를 생성하고 이 클래스의 콜백을 연결한다.
        public void Create()
        {
            if (playerControls != null)
            {
                return;
            }

            playerControls = new PlayerControls();
            playerControls.Player.SetCallbacks(this);
        }

        // 플레이어 입력을 받기 시작한다.
        public void Enable()
        {
            if (playerControls == null)
            {
                throw new InvalidOperationException(
                    "PlayerInputReader.Create()를 먼저 호출해야 합니다.");
            }

            playerControls.Player.Enable();
        }

        // 입력을 멈추고 남아 있는 입력값을 모두 초기화한다.
        public void Disable()
        {
            if (playerControls == null)
            {
                return;
            }

            playerControls.Player.Disable();
            MoveValue = Vector2.zero;
            IsSprinting = false;
            IsBlocking = false;
            hasRollInput = false;
            hasBlockImpactTestInput = false;
        }

        // 콜백과 Input Actions를 안전하게 해제한다.
        public void Destroy()
        {
            if (playerControls == null)
            {
                return;
            }

            Disable();
            playerControls.Player.SetCallbacks(null);
            playerControls.Dispose();
            playerControls = null;
        }

        // Input System이 호출한 값을 현재 입력 상태에 저장한다.
        public void OnLook(InputAction.CallbackContext context)
        {
            // 현재 플레이어 이동에서는 Look 값을 사용하지 않는다.
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            MoveValue = context.ReadValue<Vector2>();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            IsSprinting = context.ReadValueAsButton();
        }

        // 저장된 롤 입력을 한 번 반환한 뒤 지운다.
        public bool TakeRollInput()
        {
            if (!hasRollInput)
            {
                return false;
            }

            hasRollInput = false;
            return true;
        }

        public void OnRoll(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                hasRollInput = true;
            }
        }

        public void OnShield(InputAction.CallbackContext context)
        {
            IsBlocking = context.ReadValueAsButton();
        }

        // 테스트용 방어 피격 입력을 한 번 반환한 뒤 지운다.
        public bool TakeBlockImpactTestInput()
        {
            if (!hasBlockImpactTestInput)
            {
                return false;
            }

            hasBlockImpactTestInput = false;
            return true;
        }

        public void OnShieldImpactTest(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                hasBlockImpactTestInput = true;
            }
        }

    }
}
