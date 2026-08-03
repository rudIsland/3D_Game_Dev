using System;
using NUnit.Framework;
using rudIsland.RPG3D.Characters;

namespace rudIsland.RPG3D.Tests
{
    public sealed class UnitDefenseStatusTests
    {
        [Test]
        public void StartAndStopDefenseWindows_UpdatesState()
        {
            var defense = new UnitDefenseStatus(120f);

            defense.StartGuard();
            defense.StartInvincible();
            defense.StartParryWindow();
            defense.StartSuperArmor();

            Assert.That(defense.IsGuarding, Is.True);
            Assert.That(defense.IsInvincible, Is.True);
            Assert.That(defense.IsParryWindowOpen, Is.True);
            Assert.That(defense.IsSuperArmorActive, Is.True);

            defense.StopGuard();
            defense.StopInvincible();
            defense.StopParryWindow();
            defense.StopSuperArmor();

            Assert.That(defense.IsGuarding, Is.False);
            Assert.That(defense.IsInvincible, Is.False);
            Assert.That(defense.IsParryWindowOpen, Is.False);
            Assert.That(defense.IsSuperArmorActive, Is.False);
        }

        [Test]
        public void Reset_ClosesAllDefenseWindows()
        {
            var defense = new UnitDefenseStatus(90f);
            defense.StartGuard();
            defense.StartInvincible();
            defense.StartParryWindow();
            defense.StartSuperArmor();

            defense.Reset();

            Assert.That(defense.IsGuarding, Is.False);
            Assert.That(defense.IsInvincible, Is.False);
            Assert.That(defense.IsParryWindowOpen, Is.False);
            Assert.That(defense.IsSuperArmorActive, Is.False);
        }

        [Test]
        public void Constructor_ClampsGuardAngle()
        {
            var defense = new UnitDefenseStatus(240f);

            Assert.That(defense.GuardAngle, Is.EqualTo(180f));
        }

        [Test]
        public void Constructor_WithInvalidAngle_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new UnitDefenseStatus(float.NaN));
        }
    }
}
