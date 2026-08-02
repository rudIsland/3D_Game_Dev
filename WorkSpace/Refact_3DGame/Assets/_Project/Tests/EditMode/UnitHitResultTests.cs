using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Tests
{
    public sealed class UnitHitResultTests
    {
        private sealed class TestUnit : Unit
        {
            private readonly UnitStagger unitStagger;

            public TestUnit(UnitTeam team, float maxHealth)
                : base(team, maxHealth)
            {
            }

            public TestUnit(
                UnitTeam team,
                float maxHealth,
                float staggerLimit)
                : base(team, maxHealth)
            {
                unitStagger = new UnitStagger(
                    staggerLimit,
                    1f,
                    10f);
            }

            public float CurrentStagger =>
                unitStagger != null
                    ? unitStagger.CurrentStagger
                    : 0f;

            public AttackHitResult ApplyHit(in AttackHitData hit)
            {
                return ApplyHealthHit(in hit);
            }

            public AttackHitResult ApplyHitWithStagger(
                in AttackHitData hit)
            {
                return ApplyHealthAndStaggerHit(
                    in hit,
                    unitStagger);
            }
        }

        [Test]
        public void ApplyHit_FromSameTeam_ReturnsIgnored()
        {
            var unit = new TestUnit(UnitTeam.Player, 100f);
            var hit = new AttackHitData(
                new AttackDamage(10f), UnitTeam.Player, 1);

            AttackHitResult result = unit.ApplyHit(in hit);

            Assert.That(result, Is.EqualTo(AttackHitResult.Ignored));
            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void ApplyHit_WithInvalidDamage_ReturnsIgnored()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 100f);
            var hit = new AttackHitData(
                default, UnitTeam.Player, 1);

            AttackHitResult result = unit.ApplyHit(in hit);

            Assert.That(result, Is.EqualTo(AttackHitResult.Ignored));
            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void ApplyHit_WithRemainingHealth_ReturnsDamaged()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 100f);
            var hit = new AttackHitData(
                new AttackDamage(30f), UnitTeam.Player, 1);

            AttackHitResult result = unit.ApplyHit(in hit);

            Assert.That(result, Is.EqualTo(AttackHitResult.Damaged));
            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(70f));
        }

        [Test]
        public void ApplyHit_WithNoRemainingHealth_ReturnsKilled()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 20f);
            var hit = new AttackHitData(
                new AttackDamage(20f), UnitTeam.Player, 1);

            AttackHitResult killedResult = unit.ApplyHit(in hit);
            AttackHitResult deadUnitResult = unit.ApplyHit(in hit);

            Assert.That(
                killedResult,
                Is.EqualTo(AttackHitResult.Killed));
            Assert.That(
                deadUnitResult,
                Is.EqualTo(AttackHitResult.Ignored));
            Assert.That(unit.Health.CurrentHealth, Is.Zero);
        }

        [Test]
        public void ApplyHitWithStagger_BelowLimit_ReturnsDamaged()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 100f, 20f);
            var hit = new AttackHitData(
                new AttackDamage(10f),
                UnitTeam.Player,
                1,
                10f,
                0.4f);

            AttackHitResult result =
                unit.ApplyHitWithStagger(in hit);

            Assert.That(result, Is.EqualTo(AttackHitResult.Damaged));
            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(90f));
            Assert.That(unit.CurrentStagger, Is.EqualTo(10f));
        }

        [Test]
        public void ApplyHitWithStagger_ReachingLimit_ReturnsStaggered()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 100f, 20f);
            var hit = new AttackHitData(
                new AttackDamage(10f),
                UnitTeam.Player,
                1,
                10f,
                0.4f);

            AttackHitResult firstResult =
                unit.ApplyHitWithStagger(in hit);
            AttackHitResult secondResult =
                unit.ApplyHitWithStagger(in hit);

            Assert.That(
                firstResult,
                Is.EqualTo(AttackHitResult.Damaged));
            Assert.That(
                secondResult,
                Is.EqualTo(AttackHitResult.Staggered));
            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(80f));
            Assert.That(unit.CurrentStagger, Is.Zero);
        }

        [Test]
        public void ApplyHitWithStagger_WhenHealthEnds_ReturnsKilled()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 10f, 10f);
            var hit = new AttackHitData(
                new AttackDamage(10f),
                UnitTeam.Player,
                1,
                10f,
                0.4f);

            AttackHitResult result =
                unit.ApplyHitWithStagger(in hit);

            Assert.That(result, Is.EqualTo(AttackHitResult.Killed));
            Assert.That(unit.Health.CurrentHealth, Is.Zero);
            Assert.That(unit.CurrentStagger, Is.Zero);
        }
    }
}
