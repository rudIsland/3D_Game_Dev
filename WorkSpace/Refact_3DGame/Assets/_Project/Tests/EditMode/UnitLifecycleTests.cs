using NUnit.Framework;
using rudIsland.RPG3D.Characters;

namespace rudIsland.RPG3D.Tests
{
    public sealed class UnitLifecycleTests
    {
        [Test]
        public void Enable_IncreasesActivationSequence()
        {
            var unit = new FakeUnit();
            unit.Create();

            unit.Enable();
            unit.Disable();
            unit.Enable();

            Assert.That(unit.ActivationSequence, Is.EqualTo(2));
        }

        [Test]
        public void Disable_ClosesDefenseWindowsAndResetsStagger()
        {
            var unit = new FakeUnit();
            unit.Create();
            unit.Enable();
            unit.DefenseStatus.StartGuard();
            unit.DefenseStatus.StartInvincible();
            unit.DefenseStatus.StartParryWindow();
            unit.DefenseStatus.StartSuperArmor();
            unit.Stagger.AddStaggerDamage(10f);

            unit.Disable();

            Assert.That(unit.DefenseStatus.IsGuarding, Is.False);
            Assert.That(unit.DefenseStatus.IsInvincible, Is.False);
            Assert.That(unit.DefenseStatus.IsParryWindowOpen, Is.False);
            Assert.That(unit.DefenseStatus.IsSuperArmorActive, Is.False);
            Assert.That(unit.Stagger.CurrentStagger, Is.Zero);
        }

        [Test]
        public void Tick_UpdatesResourcesBeforeDerivedTick()
        {
            var unit = new FakeUnit();
            unit.Create();
            unit.Enable();
            unit.Stagger.AddStaggerDamage(10f);
            unit.Stamina.Spend(70f);

            unit.Tick(2f);

            Assert.That(unit.StaggerSeenDuringTick, Is.EqualTo(0f));
            Assert.That(unit.StaminaSeenDuringTick, Is.EqualTo(50f));
        }

        [Test]
        public void PlayerEnable_DoesNotResetHealth()
        {
            var unit = new FakePlayerUnit();
            unit.Create();
            unit.Enable();
            unit.Health.TakeDamage(30f);
            unit.Disable();
            unit.Enable();

            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(70f));
        }

        [Test]
        public void EnemyEnable_ResetsHealthAndStamina()
        {
            var unit = new FakeEnemyUnit();
            unit.Create();
            unit.Enable();
            unit.Health.TakeDamage(30f);
            unit.Stamina.Spend(20f);
            unit.Disable();
            unit.Enable();

            Assert.That(unit.Health.CurrentHealth, Is.EqualTo(100f));
            Assert.That(unit.Stamina.CurrentStamina, Is.EqualTo(100f));
        }

        private sealed class FakeUnit : Unit
        {
            public float StaggerSeenDuringTick { get; private set; }
            public float StaminaSeenDuringTick { get; private set; }

            public FakeUnit()
                : base(UnitTeam.Enemy, 100f, 20f, 1f, 10f, 100f, 1f, 20f, 90f)
            {
            }

            protected override void OnUnitTick(float deltaTime)
            {
                StaggerSeenDuringTick = Stagger.CurrentStagger;
                StaminaSeenDuringTick = Stamina.CurrentStamina;
            }
        }

        private sealed class FakePlayerUnit : PlayerUnit
        {
            public FakePlayerUnit()
                : base(100f,
                    20f,
                    1f,
                    10f,
                    100f,
                    1f,
                    20f,
                    90f)
            {
            }
        }

        private sealed class FakeEnemyUnit : EnemyUnit
        {
            public FakeEnemyUnit()
                : base(100f, 20f, 1f, 10f, 100f, 1f, 20f, 90f)
            {
            }
        }
    }
}
