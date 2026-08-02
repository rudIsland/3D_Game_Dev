using NUnit.Framework;
using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Tests
{
    public sealed class AttackDamageTests
    {
        [Test]
        public void Constructor_WithPositiveDamage_StoresHealthDamage()
        {
            var damage = new AttackDamage(10f);

            Assert.That(damage.IsValid, Is.True);
            Assert.That(damage.HealthDamage, Is.EqualTo(10f));
        }

        [Test]
        public void Constructor_WithInvalidDamage_ReturnsZeroHealthDamage()
        {
            var negative = new AttackDamage(-1f);
            var nan = new AttackDamage(float.NaN);
            var positiveInfinity =
                new AttackDamage(float.PositiveInfinity);
            var negativeInfinity =
                new AttackDamage(float.NegativeInfinity);

            Assert.That(negative.IsValid, Is.False);
            Assert.That(nan.IsValid, Is.False);
            Assert.That(positiveInfinity.IsValid, Is.False);
            Assert.That(negativeInfinity.IsValid, Is.False);
            Assert.That(negative.HealthDamage, Is.Zero);
            Assert.That(nan.HealthDamage, Is.Zero);
            Assert.That(positiveInfinity.HealthDamage, Is.Zero);
            Assert.That(negativeInfinity.HealthDamage, Is.Zero);
        }
    }
}
