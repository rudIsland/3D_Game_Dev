using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat.Attack;
using rudIsland.RPG3D.Combat.Detection;
using rudIsland.RPG3D.Combat.Resolution;
using rudIsland.RPG3D.Combat.Result;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class AttackHitResultCalculatorTests
    {
        private sealed class TestUnit : Unit
        {
            public TestUnit(
                UnitTeam team = UnitTeam.Enemy,
                float maxHealth = 100f,
                float staggerLimit = 20f,
                float maxStamina = 100f)
                : base(
                    team,
                    maxHealth,
                    staggerLimit,
                    1f,
                    10f,
                    maxStamina,
                    1f,
                    20f,
                    120f)
            {
                Create();
                Enable();
            }

            public void ApplyResult(in AttackHitResult result)
            {
                ApplyAttackHitResult(in result);
            }
        }

        private readonly AttackHitResultCalculator calculator =
            new AttackHitResultCalculator();

        [Test]
        public void CalculateResult_DoesNotChangeTargetValues()
        {
            var target = new TestUnit();
            var hit = CreateHit(30f, 5f);

            AttackHitResult result = calculator.CalculateResult(
                in hit,
                target,
                Vector3.forward);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Damaged));
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(100f));
            Assert.That(target.Stagger.CurrentStagger, Is.Zero);
            Assert.That(target.Stamina.CurrentStamina, Is.EqualTo(100f));
        }

        [Test]
        public void SameInput_ReturnsSameResult()
        {
            var firstTarget = new TestUnit();
            var secondTarget = new TestUnit();
            var hit = CreateHit(30f, 5f);

            AttackHitResult first = calculator.CalculateResult(
                in hit,
                firstTarget,
                Vector3.forward);
            AttackHitResult second = calculator.CalculateResult(
                in hit,
                secondTarget,
                Vector3.forward);

            Assert.That(first.Type, Is.EqualTo(second.Type));
            Assert.That(first.HealthDamage, Is.EqualTo(second.HealthDamage));
            Assert.That(first.StaggerDamage, Is.EqualTo(second.StaggerDamage));
        }

        [Test]
        public void InvalidAttack_ReturnsIgnored()
        {
            var target = new TestUnit();
            var hit = new AttackHitInput(default, UnitTeam.Player, 1);

            AttackHitResult result = calculator.CalculateResult(
                in hit,
                target,
                Vector3.forward);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Ignored));
        }

        [Test]
        public void FriendlyAttack_ReturnsIgnored()
        {
            var target = new TestUnit(UnitTeam.Player);
            var hit = CreateHit(10f, 1f, UnitTeam.Player);

            AttackHitResult result = calculator.CalculateResult(
                in hit,
                target,
                Vector3.forward);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Ignored));
        }

        [Test]
        public void DeadTarget_ReturnsIgnored()
        {
            var target = new TestUnit();
            target.Health.TakeDamage(100f);
            var hit = CreateHit(10f, 1f);

            AttackHitResult result = calculator.CalculateResult(
                in hit,
                target,
                Vector3.forward);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Ignored));
        }

        [Test]
        public void LethalDamage_ReturnsKilledBeforeStaggered()
        {
            var target = new TestUnit(maxHealth: 10f, staggerLimit: 1f);
            var hit = CreateHit(10f, 10f);

            AttackHitResult result = calculator.CalculateResult(
                in hit,
                target,
                Vector3.forward);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Killed));
            Assert.That(result.HealthDamage, Is.EqualTo(10f));
            Assert.That(result.StaggerDamage, Is.Zero);
        }

        [Test]
        public void HealthDamageAndStaggerDamage_AreIndependent()
        {
            var target = new TestUnit(staggerLimit: 20f);
            var hit = CreateHit(30f, 5f);

            AttackHitResult result = calculator.CalculateResult(
                in hit,
                target,
                Vector3.forward);
            target.ApplyResult(in result);

            Assert.That(target.Health.CurrentHealth, Is.EqualTo(70f));
            Assert.That(target.Stagger.CurrentStagger, Is.EqualTo(5f));
        }

        [Test]
        public void ApplyAttackHitResult_ChangesEachValueOnce()
        {
            var target = new TestUnit();
            var hit = CreateHit(30f, 5f);
            AttackHitResult result = calculator.CalculateResult(
                in hit,
                target,
                Vector3.forward);

            target.ApplyResult(in result);

            Assert.That(target.Health.CurrentHealth, Is.EqualTo(70f));
            Assert.That(target.Stagger.CurrentStagger, Is.EqualTo(5f));
        }

        [Test]
        public void FrontGuardWithBlockableAttack_ReturnsGuarded()
        {
            var target = new TestUnit();
            target.DefenseStatus.StartGuard();
            var hit = CreateHit(
                30f,
                5f,
                hitDirection: Vector3.back);

            AttackHitResult result = target.ReceiveAttackHit(
                in hit,
                Vector3.forward);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Guarded));
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(100f));
            Assert.That(target.Stagger.CurrentStagger, Is.Zero);
        }

        [Test]
        public void BackAttackWhileGuarding_ReturnsDamageResult()
        {
            var target = new TestUnit();
            target.DefenseStatus.StartGuard();
            var hit = CreateHit(
                30f,
                5f,
                hitDirection: Vector3.forward);

            AttackHitResult result = target.ReceiveAttackHit(
                in hit,
                Vector3.forward);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Damaged));
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(70f));
            Assert.That(target.Stagger.CurrentStagger, Is.EqualTo(5f));
        }

        [Test]
        public void UnblockableAttackWhileGuarding_ReturnsDamageResult()
        {
            var target = new TestUnit();
            target.DefenseStatus.StartGuard();
            var hit = CreateHit(
                30f,
                5f,
                canBeBlocked: false,
                hitDirection: Vector3.back);

            AttackHitResult result = target.ReceiveAttackHit(
                in hit,
                Vector3.forward);

            Assert.That(result.Type, Is.EqualTo(AttackHitResultType.Damaged));
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(70f));
        }

        private static AttackHitInput CreateHit(
            float healthDamage,
            float staggerDamage,
            UnitTeam attackerTeam = UnitTeam.Player,
            bool canBeBlocked = true,
            Vector3 hitDirection = default)
        {
            var contact = new HitContact(
                Vector3.zero,
                Vector3.zero,
                hitDirection,
                HitBodyPart.Body);
            return new AttackHitInput(
                new AttackDamage(healthDamage),
                attackerTeam,
                1,
                HitStrength.Light,
                staggerDamage,
                0f,
                canBeBlocked,
                true,
                0f,
                0f,
                contact);
        }
    }
}
