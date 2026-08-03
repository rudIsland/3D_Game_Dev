using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace rudIsland.RPG3D.Player.Input
{
    // Input System 값을 게임에서 읽기 쉬운 상태로 저장한다.
    public sealed class PlayerInputReader : PlayerControls.IPlayerActions
    {
        private PlayerControls playerControls; // 내부에서 사용하는 값
        private bool hasRollInput; // 기능 사용 여부
        private bool hasAttackInput; // 기능 사용 여부
        private bool hasTargetToggleInput; // 기능 사용 여부

        // 누르고 있는 동안 계속 유지되는 입력 상태다.
        public Vector2 MoveValue { get; private set; } // 이동 정보
        public bool IsSprinting { get; private set; } // 기능 사용 여부
        public bool IsBlocking { get; private set; } // 기능 사용 여부

        public void Create()
        {
            if (playerControls != null)
            {
                return;
            }

            playerControls = new PlayerControls();
            playerControls.Player.SetCallbacks(this);
        }

        public void Enable()
        {
            if (playerControls == null)
            {
                throw new InvalidOperationException(
                    "PlayerInputReader.Create()를 먼저 호출해야 합니다.");
            }

            playerControls.Player.Enable();
        }

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
            hasAttackInput = false;
            hasTargetToggleInput = false;
        }

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

        public void OnLook(InputAction.CallbackContext context)
        {
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            MoveValue = context.ReadValue<Vector2>();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            IsSprinting = context.ReadValueAsButton();
        }

        public bool TakeRollInput()
        {
            if (!hasRollInput)
            {
                return false;
            }

            hasRollInput = false;
            hasAttackInput = false;
            return true;
        }

        public void OnRoll(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                hasRollInput = true;
            }
        }

        public bool TakeAttackInput()
        {
            if (!hasAttackInput)
            {
                return false;
            }

            hasAttackInput = false;
            return true;
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                hasAttackInput = true;
            }
        }

        public bool TakeTargetToggleInput()
        {
            if (!hasTargetToggleInput)
            {
                return false;
            }

            hasTargetToggleInput = false;
            return true;
        }

        public void OnTargetToggle(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                hasTargetToggleInput = true;
            }
        }

        public void OnShield(InputAction.CallbackContext context)
        {
            IsBlocking = context.ReadValueAsButton();
        }

        public void OnShieldImpactTest(InputAction.CallbackContext context)
        {
        }
    }
}
