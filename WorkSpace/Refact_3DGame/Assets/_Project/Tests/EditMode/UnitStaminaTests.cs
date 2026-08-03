using System;
using NUnit.Framework;
using rudIsland.RPG3D.Characters;

namespace rudIsland.RPG3D.Tests
{
    public sealed class UnitStaminaTests
    {
        [Test]
        public void Constructor_StartsAtMaximum()
        {
            var stamina = new UnitStamina(50f, 1f, 10f);

            Assert.That(stamina.MaxStamina, Is.EqualTo(50f));
            Assert.That(stamina.CurrentStamina, Is.EqualTo(50f));
        }

        [Test]
        public void Spend_StartsRecoveryDelay()
        {
            var stamina = new UnitStamina(50f, 1f, 10f);

            stamina.Spend(20f);
            stamina.Update(0.5f, true);

            Assert.That(stamina.CurrentStamina, Is.EqualTo(30f));
        }

        [Test]
        public void Update_RecoversAfterDelay()
        {
            var stamina = new UnitStamina(50f, 1f, 10f);
            stamina.Spend(20f);

            stamina.Update(1.5f, true);

            Assert.That(stamina.CurrentStamina, Is.EqualTo(35f));
        }

        [Test]
        public void Update_WhenRecoveryDisabled_DoesNothing()
        {
            var stamina = new UnitStamina(50f, 0f, 10f);
            stamina.Spend(20f);

            stamina.Update(1f, false);

            Assert.That(stamina.CurrentStamina, Is.EqualTo(30f));
        }

        [Test]
        public void Reset_RestoresMaximum()
        {
            var stamina = new UnitStamina(50f, 1f, 10f);
            stamina.Spend(20f);

            stamina.Reset();

            Assert.That(stamina.CurrentStamina, Is.EqualTo(50f));
        }

        [Test]
        public void Constructor_WithInvalidValue_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new UnitStamina(-1f, 1f, 10f));
        }
    }
}
