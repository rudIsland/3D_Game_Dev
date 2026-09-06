using System;
using Characters;
using Characters.Combat;
using Characters.Enemies.Zombie;
using Characters.Player.Config;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests
{
    public sealed class CharacterSettingsTests
    {
        private const string ZombieConfigPath =
            "Assets/_Project/02.Enemy/Zombie/Configs/ZombieConfig.asset";
        private const string ZombiePrefabPath =
            "Assets/_Project/02.Enemy/Zombie/Prefabs/Zombie.prefab";

        [Test]
        public void SharedLife_DamageUpgradeAndRecoveryRemainPerCharacter()
        {
            var settings = new CharacterLifeSettings(100f, 50f, 3f, 5f);
            var firstHealth = new UnitHealth(settings.MaxHealth);
            var secondHealth = new UnitHealth(settings.MaxHealth);
            var firstStagger = new StopPoint(settings);
            var secondStagger = new StopPoint(settings);

            firstHealth.TakeDamage(20f);
            firstHealth.MultiplyMaximum(2f);
            firstStagger.TryAccumulate(25f);
            firstStagger.UpdateRecovery(2f);
            Assert.That(firstStagger.CurrentPoint, Is.EqualTo(25f));
            firstStagger.UpdateRecovery(1f);
            Assert.That(firstStagger.CurrentPoint, Is.EqualTo(20f));
            Assert.That(firstHealth.CurrentHealth, Is.EqualTo(180f));
            Assert.That(secondHealth.CurrentHealth, Is.EqualTo(100f));
            Assert.That(secondStagger.CurrentPoint, Is.Zero);
            Assert.That(settings.MaxHealth, Is.EqualTo(100f));
        }

        [Test]
        public void ZombiePrefab_PreservesSettingsAndSceneReferences()
        {
            var config = AssetDatabase.LoadAssetAtPath<ZombieConfig>(ZombieConfigPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
            Assert.That(config, Is.Not.Null);
            Assert.That(prefab, Is.Not.Null);
            var controller = new SerializedObject(prefab.GetComponent<ZombieController>());
            Assert.That(controller.FindProperty("config").objectReferenceValue, Is.SameAs(config));
            Assert.That(controller.FindProperty("zombieAnimator").objectReferenceValue, Is.Not.Null);
            foreach (string shape in new[] { "swingHitShape", "kickHitShape", "upDownHitShape" })
            {
                Assert.That(controller.FindProperty(shape + ".startPoint").objectReferenceValue, Is.Not.Null);
                Assert.That(controller.FindProperty(shape + ".endPoint").objectReferenceValue, Is.Not.Null);
            }

            ZombieSettings settings = config.GetRuntimeSettings();
            Assert.That(settings.Life.MaxHealth, Is.EqualTo(100f));
            Assert.That(settings.Life.StaggerLimit, Is.EqualTo(50f));
            Assert.That(settings.FindRangeSquared, Is.EqualTo(225f));
            Assert.That(settings.AttackRangeSquared, Is.EqualTo(1f));
            Assert.That(settings.HomeRecoveryDelay, Is.EqualTo(3f));
            Assert.That(settings.HealthRecoverySpeed, Is.EqualTo(10f));
            Assert.That(settings.SwingAttackDamage.HealthDamage, Is.EqualTo(10f));
            Assert.That(settings.KickAttackDamage.Strength, Is.EqualTo(AttackStrength.Heavy));
            Assert.That(settings.EvaluateHitPushProgress(0.5f), Is.EqualTo(0.75f).Within(0.00001f));
        }

        [Test]
        public void ZombieSnapshot_DoesNotFollowLaterAuthoringChanges()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfig>();
            try
            {
                ZombieSettings first = config.GetRuntimeSettings();
                float originalProgress = first.EvaluateHitPushProgress(0.5f);
                var serialized = new SerializedObject(config);
                serialized.FindProperty("maxHealth").floatValue = 270f;
                serialized.FindProperty("hitPushCurve").animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ZombieSettings second = config.GetRuntimeSettings();

                Assert.That(first.Life.MaxHealth, Is.EqualTo(100f));
                Assert.That(second.Life.MaxHealth, Is.EqualTo(270f));
                Assert.That(first.EvaluateHitPushProgress(0.5f), Is.EqualTo(originalProgress));
                Assert.That(second.EvaluateHitPushProgress(0.5f), Is.EqualTo(0.5f).Within(0.00001f));
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void PlayerLife_KeepsAuthoringValuesAndSeparateStamina()
        {
            var source = AssetDatabase.LoadAssetAtPath<PlayerCharacterConfig>(
                "Assets/_Project/01.Player/Configs/PlayerCharacterConfig.asset");
            Assert.That(source, Is.Not.Null);
            var config = Object.Instantiate(source);
            try
            {
                var serialized = new SerializedObject(config);
                serialized.FindProperty("combat.maxHealth").floatValue = 230f;
                serialized.FindProperty("combat.maxStamina").floatValue = 170f;
                serialized.FindProperty("combat.stopPointLimit").floatValue = 65f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PlayerCombatRuntimeConfig combat = config.CreateRuntimeConfig().Combat;
                Assert.That(combat.Life.MaxHealth, Is.EqualTo(230f));
                Assert.That(combat.Life.StaggerLimit, Is.EqualTo(65f));
                Assert.That(combat.MaxStamina, Is.EqualTo(170f));
            }
            finally { Object.DestroyImmediate(config); }
        }

        // 에디터 자동 점검에서 Play 중 실제 캐시 경로의 할당과 세션 초기화를 확인한다.
        public static string VerifyPlayModeSharing()
        {
            Assert.That(Application.isPlaying, Is.True);
            var config = ScriptableObject.CreateInstance<ZombieConfig>();
            try
            {
                ZombieSettings first = config.GetRuntimeSettings();
                long before = GC.GetAllocatedBytesForCurrentThread();
                bool shared = true;
                for (int index = 0; index < 1000; index++)
                    shared &= ReferenceEquals(first, config.GetRuntimeSettings());
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.That(shared, Is.True);
                Assert.That(allocated, Is.Zero);

                // Domain Reload를 끈 다음 Play 진입과 같은 초기화 훅을 호출한다.
                typeof(ZombieConfig).GetMethod("BeginPlaySession",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null, null);
                ZombieSettings nextSession = config.GetRuntimeSettings();
                Assert.That(nextSession, Is.Not.SameAs(first));
                Assert.That(nextSession.Life.MaxHealth, Is.EqualTo(first.Life.MaxHealth));
                return "Zombie config: 1000 cached reads, " + allocated +
                    " managed bytes; same settings shared; session reset verified.";
            }
            finally { Object.DestroyImmediate(config); }
        }
    }
}
