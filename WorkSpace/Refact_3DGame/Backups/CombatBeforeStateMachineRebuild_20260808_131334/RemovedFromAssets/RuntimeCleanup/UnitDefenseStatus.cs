using System;

namespace rudIsland.RPG3D.Characters
{
    // Unit의 무적, 가드, 패리와 슈퍼아머 창을 관리한다.
    public sealed class UnitDefenseStatus
    {
        public bool IsInvincible { get; private set; }
        public bool IsGuarding { get; private set; }
        public bool IsParryWindowOpen { get; private set; }
        public bool IsSuperArmorActive { get; private set; }
        public float GuardAngle { get; }

        public UnitDefenseStatus(float guardAngle)
        {
            if (float.IsNaN(guardAngle) ||
                float.IsInfinity(guardAngle))
            {
                throw new ArgumentOutOfRangeException(nameof(guardAngle));
            }

            GuardAngle = Math.Max(0f, Math.Min(180f, guardAngle));
        }

        public void StartInvincible() => IsInvincible = true;
        public void StopInvincible() => IsInvincible = false;
        public void StartGuard() => IsGuarding = true;
        public void StopGuard() => IsGuarding = false;
        public void StartParryWindow() => IsParryWindowOpen = true;
        public void StopParryWindow() => IsParryWindowOpen = false;
        public void StartSuperArmor() => IsSuperArmorActive = true;
        public void StopSuperArmor() => IsSuperArmorActive = false;

        public void Reset()
        {
            IsInvincible = false;
            IsGuarding = false;
            IsParryWindowOpen = false;
            IsSuperArmorActive = false;
        }
    }
}
