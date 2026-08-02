using System;
using NUnit.Framework;
using rudIsland.RPG3D.Characters;

namespace rudIsland.RPG3D.Tests
{
    public sealed class UnitStaggerTests
    {
        [Test]
        public void AddStaggerDamage_BelowLimit_StoresCurrentStagger()
        {
            var stagger = new UnitStagger(20f, 1f, 10f);

            bool staggered = stagger.AddStaggerDamage(8f);

            Assert.That(staggered, Is.False);
            Assert.That(stagger.CurrentStagger, Is.EqualTo(8f));
        }

        [Test]
        public void AddStaggerDamage_ReachingLimit_ReturnsTrueAndResets()
        {
            var stagger = new UnitStagger(20f, 1f, 10f);
            stagger.AddStaggerDamage(8f);

            bool staggered = stagger.AddStaggerDamage(12f);

            Assert.That(staggered, Is.True);
            Assert.That(stagger.CurrentStagger, Is.Zero);
        }

        [Test]
        public void Update_WaitsThenRecoversWithRemainingTime()
        {
            var stagger = new UnitStagger(20f, 1f, 4f);
            stagger.AddStaggerDamage(10f);

            stagger.Update(0.5f);
            Assert.That(stagger.CurrentStagger, Is.EqualTo(10f));

            stagger.Update(0.75f);
            Assert.That(stagger.CurrentStagger, Is.EqualTo(9f));
        }

        [Test]
        public void AddStaggerDamage_WithInvalidValue_DoesNothing()
        {
            var stagger = new UnitStagger(20f, 1f, 10f);

            bool staggered = stagger.AddStaggerDamage(float.NaN);

            Assert.That(staggered, Is.False);
            Assert.That(stagger.CurrentStagger, Is.Zero);
        }

        [Test]
        public void Reset_ClearsCurrentStagger()
        {
            var stagger = new UnitStagger(20f, 1f, 10f);
            stagger.AddStaggerDamage(8f);

            stagger.Reset();

            Assert.That(stagger.CurrentStagger, Is.Zero);
        }

        [Test]
        public void Constructor_WithInvalidLimit_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new UnitStagger(0f, 1f, 10f));
        }
    }
}
