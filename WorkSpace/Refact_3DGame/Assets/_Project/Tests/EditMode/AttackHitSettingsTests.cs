using NUnit.Framework;
using rudIsland.RPG3D.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class AttackHitSettingsTests
    {
        private GameObject detectorObject; // 씬 또는 시스템 참조

        [TearDown]
        public void TearDown()
        {
            if (detectorObject != null)
            {
                Object.DestroyImmediate(detectorObject);
            }
        }

        [Test]
        public void TryFind_FindsStrengthDamageStaggerAndPushDistance()
        {
            AttackHitSettings[] settings = CreateSettings();

            bool foundFirst = AttackHitSettings.TryFind(
                settings,
                1,
                out AttackHitSettings firstAttack);
            bool foundRun = AttackHitSettings.TryFind(
                settings,
                6,
                out AttackHitSettings runAttack);

            Assert.That(foundFirst, Is.True);
            Assert.That(foundRun, Is.True);
            Assert.That(
                firstAttack.Damage.HealthDamage,
                Is.EqualTo(10f));
            Assert.That(firstAttack.Strength, Is.EqualTo(HitStrength.Light));
            Assert.That(firstAttack.StaggerDamage, Is.EqualTo(4f));
            Assert.That(firstAttack.PushDistance, Is.EqualTo(0.4f));
            Assert.That(
                runAttack.Damage.HealthDamage,
                Is.EqualTo(60f));
            Assert.That(runAttack.Strength, Is.EqualTo(HitStrength.Heavy));
            Assert.That(runAttack.StaggerDamage, Is.EqualTo(8f));
            Assert.That(runAttack.PushDistance, Is.EqualTo(0.5f));
        }

        [Test]
        public void TryFind_WithMissingNumber_ReturnsFalse()
        {
            bool found = AttackHitSettings.TryFind(
                CreateSettings(),
                7,
                out AttackHitSettings settings);

            Assert.That(found, Is.False);
            Assert.That(settings.Damage.IsValid, Is.False);
            Assert.That(settings.StaggerDamage, Is.Zero);
            Assert.That(settings.PushDistance, Is.Zero);
        }

        [Test]
        public void TryFind_WithInvalidDamage_ReturnsFalse()
        {
            AttackHitSettings[] settings =
            {
                new AttackHitSettings(
                    1, null, new AttackDamage(float.NaN), 4f, 0.4f)
            };

            bool found = AttackHitSettings.TryFind(
                settings,
                1,
                out AttackHitSettings foundSettings);

            Assert.That(found, Is.False);
            Assert.That(foundSettings.Damage.IsValid, Is.False);
        }

        [Test]
        public void HasDuplicateAttackNumber_FindsDuplicate()
        {
            AttackHitSettings[] settings =
            {
                new AttackHitSettings(
                    1, null, new AttackDamage(10f), 4f, 0.4f),
                new AttackHitSettings(
                    1, null, new AttackDamage(20f), 8f, 0.5f)
            };

            Assert.That(
                AttackHitSettings.HasDuplicateAttackNumber(
                    settings),
                Is.True);
        }

        [Test]
        public void PushDistance_WithInvalidValue_ReturnsZero()
        {
            var settings = new AttackHitSettings(
                1,
                null,
                new AttackDamage(10f),
                4f,
                float.NaN);

            Assert.That(settings.PushDistance, Is.Zero);
        }

        [Test]
        public void StaggerDamage_WithInvalidValue_ReturnsZero()
        {
            var settings = new AttackHitSettings(
                1,
                null,
                new AttackDamage(10f),
                float.NaN,
                0.4f);

            Assert.That(settings.StaggerDamage, Is.Zero);
        }

        [Test]
        public void Constructor_StoresHitDetector()
        {
            detectorObject = new GameObject("HitDetectorTest");
            detectorObject.SetActive(false);
            MeleeHitDetector detector =
                detectorObject.AddComponent<MeleeHitDetector>();
            var settings = new AttackHitSettings(
                1,
                detector,
                new AttackDamage(10f),
                4f,
                0.4f);

            Assert.That(settings.HitDetector, Is.SameAs(detector));
        }

        private static AttackHitSettings[] CreateSettings()
        {
            return new[]
            {
                new AttackHitSettings(
                    1, null, new AttackDamage(10f), 4f, 0.4f),
                new AttackHitSettings(
                    6,
                    null,
                    new AttackDamage(60f),
                    HitStrength.Heavy,
                    8f,
                    0.5f)
            };
        }
    }
}
