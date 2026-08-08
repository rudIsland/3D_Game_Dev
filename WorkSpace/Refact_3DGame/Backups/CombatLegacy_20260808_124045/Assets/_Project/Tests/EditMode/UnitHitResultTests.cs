using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat.Attack;
using rudIsland.RPG3D.Combat.Result;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class UnitHitResultTests
    {
        private sealed class TestUnit : Unit
        {
            public TestUnit(UnitTeam team, float maxHealth)
                : base(team, maxHealth)
            {
                Create();
                Enable();
            }

            public TestUnit(
                UnitTeam team,
                float maxHealth,
                float staggerLimit)
                : base(
                    team,
                    maxHealth,
                    staggerLimit,
                    1f,
                    10f,
                    0f,
                    0f,
                    0f,
                    0f)
            {
                Create();
                Enable();
            }

            public float CurrentStagger => Stagger.CurrentStagger;

            public AttackHitResult ApplyHit(in AttackHitInput hit)
            {
                return ReceiveAttackHit(in hit, Vector3.forward);
            }
        }

        [Test]
        public void ApplyHit_FromSameTeam_ReturnsIgnored()
        {
            var unit = new TestUnit(UnitTeam.Player, 100f);
            var hit = new AttackHitInput(
                new AttackDamage(10f), UnitTeam.Player, 1);

            AttackHitResult result = unit.ApplyHit(in hit);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Ignored));
            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void ApplyHit_WithInvalidDamage_ReturnsIgnored()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 100f);
            var hit = new AttackHitInput(
                default, UnitTeam.Player, 1);

            AttackHitResult result = unit.ApplyHit(in hit);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Ignored));
            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void ApplyHit_WithRemainingHealth_ReturnsDamaged()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 100f);
            var hit = new AttackHitInput(
                new AttackDamage(30f),
                UnitTeam.Player,
                1,
                0f,
                0f);

            AttackHitResult result = unit.ApplyHit(in hit);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Damaged));
            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(70f));
        }

        [Test]
        public void ApplyHit_WithNoRemainingHealth_ReturnsKilled()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 20f);
            var hit = new AttackHitInput(
                new AttackDamage(20f), UnitTeam.Player, 1);

            AttackHitResult killedResult = unit.ApplyHit(in hit);
            AttackHitResult deadUnitResult = unit.ApplyHit(in hit);

            Assert.That(
                killedResult.Type,
                Is.EqualTo(AttackHitResultType.Killed));
            Assert.That(
                deadUnitResult.Type,
                Is.EqualTo(AttackHitResultType.Ignored));
            Assert.That(unit.Health.CurrentHealth, Is.Zero);
        }

        [Test]
        public void ApplyHitWithStagger_BelowLimit_ReturnsDamaged()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 100f, 20f);
            var hit = new AttackHitInput(
                new AttackDamage(10f),
                UnitTeam.Player,
                1,
                10f,
                0.4f);

            AttackHitResult result = unit.ApplyHit(in hit);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Damaged));
            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(90f));
            Assert.That(unit.CurrentStagger, Is.EqualTo(10f));
        }

        [Test]
        public void ApplyHitWithStagger_ReachingLimit_ReturnsStaggered()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 100f, 20f);
            var hit = new AttackHitInput(
                new AttackDamage(10f),
                UnitTeam.Player,
                1,
                10f,
                0.4f);

            AttackHitResult firstResult = unit.ApplyHit(in hit);
            AttackHitResult secondResult = unit.ApplyHit(in hit);

            Assert.That(
                firstResult.Type,
                Is.EqualTo(AttackHitResultType.Damaged));
            Assert.That(
                secondResult.Type,
                Is.EqualTo(AttackHitResultType.Staggered));
            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(80f));
            Assert.That(unit.CurrentStagger, Is.Zero);
        }

        [Test]
        public void ApplyHitWithStagger_WhenHealthEnds_ReturnsKilled()
        {
            var unit = new TestUnit(UnitTeam.Enemy, 10f, 10f);
            var hit = new AttackHitInput(
                new AttackDamage(10f),
                UnitTeam.Player,
                1,
                10f,
                0.4f);

            AttackHitResult result = unit.ApplyHit(in hit);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Killed));
            Assert.That(unit.Health.CurrentHealth, Is.Zero);
            Assert.That(unit.CurrentStagger, Is.Zero);
        }
    }
}
