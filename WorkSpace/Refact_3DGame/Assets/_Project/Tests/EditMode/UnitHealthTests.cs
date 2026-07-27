using NUnit.Framework;
using rudIsland.RPG3D.Characters;

namespace rudIsland.RPG3D.Tests
{
    public sealed class UnitHealthTests
    {
        [Test]
        public void TakeDamage_WhenHealthReachesZero_RaisesDeathOnce()
        {
            var health = new UnitHealth(10f);
            int diedCount = 0;
            health.Died += () => diedCount++;

            health.TakeDamage(4f);
            health.TakeDamage(6f);
            health.TakeDamage(1f);

            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(health.IsDead, Is.True);
            Assert.That(diedCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_AfterDeath_RestoresMaximumHealth()
        {
            var health = new UnitHealth(10f);
            health.TakeDamage(10f);

            health.Reset();

            Assert.That(health.CurrentHealth, Is.EqualTo(10f));
            Assert.That(health.IsDead, Is.False);
        }
    }
}
