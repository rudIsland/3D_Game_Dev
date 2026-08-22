using NUnit.Framework;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Combat.AttackData;
using rudIsland.RPG3D.Characters.Enemies.AttackData;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEditor;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordConfigTests
    {
        private const string ConfigPath =
            "Assets/_Project/Characters/Enemies/NightShade/Configs/NightShadeSwordEliteConfig.asset";
        private const string PrefabPath =
            "Assets/_Project/Scenes/Dev/CharacterTest/Prefabs/NightShadeSwordElite.prefab";

        [Test]
        public void 공격Asset이없는Config_RuntimeSettings생성을거절한다()
        {
            NightShadeSwordConfig config =
                ScriptableObject.CreateInstance<NightShadeSwordConfig>();
            try
            {
                Assert.Throws<System.ArgumentException>(
                    () => config.CreateRuntimeSettings());
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void EliteConfig_EnemyAttackData배열과공통설정을보관한다()
        {
            NightShadeSwordConfig config =
                AssetDatabase.LoadAssetAtPath<NightShadeSwordConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, ConfigPath);

            var serializedConfig = new SerializedObject(config);
            SerializedProperty attacks =
                serializedConfig.FindProperty("attacks");
            Assert.That(attacks, Is.Not.Null);
            Assert.That(attacks.isArray, Is.True);
            Assert.That(attacks.arraySize, Is.EqualTo(4));
            for (int index = 0; index < attacks.arraySize; index++)
            {
                Object attack = attacks.GetArrayElementAtIndex(index)
                    .objectReferenceValue;
                Assert.That(attack, Is.InstanceOf<EnemyAttackData>());
            }
            Assert.That(
                serializedConfig.FindProperty("life.maxHealth"),
                Is.Not.Null);
            Assert.That(
                serializedConfig.FindProperty("combatRange.findRange"),
                Is.Not.Null);
            Assert.That(
                serializedConfig.FindProperty("engagement"),
                Is.Null);
            Assert.That(
                serializedConfig.FindProperty("attackSelection.distanceScoreWeight"),
                Is.Not.Null);
            Assert.That(
                serializedConfig.FindProperty("movement.walkSpeed"),
                Is.Not.Null);
            Assert.That(
                serializedConfig.FindProperty("recovery.moveDuration"),
                Is.Not.Null);
            Assert.That(
                serializedConfig.FindProperty("hitReaction.pushDuration"),
                Is.Not.Null);

            AssertExpectedSettings(config.CreateRuntimeSettings());
        }

        [Test]
        public void 공격Asset_설정값만보관하고실행상태는보관하지않는다()
        {
            NightShadeSwordConfig config =
                AssetDatabase.LoadAssetAtPath<NightShadeSwordConfig>(ConfigPath);
            var serializedConfig = new SerializedObject(config);
            SerializedProperty attacks =
                serializedConfig.FindProperty("attacks");

            for (int index = 0; index < attacks.arraySize; index++)
            {
                var attack = (EnemyAttackData)attacks
                    .GetArrayElementAtIndex(index)
                    .objectReferenceValue;
                var serializedAttack = new SerializedObject(attack);
                Assert.That(
                    serializedAttack.FindProperty("hitDamages"),
                    Is.Not.Null);
                Assert.That(
                    serializedAttack.FindProperty("postAttackDelay"),
                    Is.Not.Null);
                Assert.That(
                    serializedAttack.FindProperty("cooldownDuration"),
                    Is.Null);
                Assert.That(
                    serializedAttack.FindProperty("utility"),
                    Is.Not.Null);
                Assert.That(
                    serializedAttack.FindProperty("remainingAttackCooldown"),
                    Is.Null);
                Assert.That(
                    serializedAttack.FindProperty("isExecuting"),
                    Is.Null);
                Assert.That(
                    serializedAttack.FindProperty("comboStep"),
                    Is.Null);
            }
        }

        [Test]
        public void NightShadeSwordElitePrefab_Config하나만전투값으로참조한다()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            NightShadeSwordConfig expectedConfig =
                AssetDatabase.LoadAssetAtPath<NightShadeSwordConfig>(ConfigPath);
            Assert.That(prefab, Is.Not.Null, PrefabPath);
            Assert.That(expectedConfig, Is.Not.Null, ConfigPath);

            Component controller = FindNightShadeSwordController(prefab);
            Assert.That(controller, Is.Not.Null);
            var serializedController = new SerializedObject(controller);
            SerializedProperty configProperty =
                serializedController.FindProperty("config");
            Assert.That(configProperty, Is.Not.Null);
            Assert.That(
                configProperty.objectReferenceValue,
                Is.SameAs(expectedConfig));
            Assert.That(serializedController.FindProperty("maxHealth"), Is.Null);
            Assert.That(serializedController.FindProperty("targetLayers"), Is.Null);
            Assert.That(serializedController.FindProperty("lightAttackDamage"), Is.Null);
            Assert.That(serializedController.FindProperty("tuning"), Is.Null);
        }

        private static void AssertExpectedSettings(
            NightShadeSwordSettings settings)
        {
            Assert.That(settings.Life.MaxHealth, Is.EqualTo(250f));
            Assert.That(settings.Life.StaggerLimit, Is.EqualTo(100f));
            Assert.That(settings.Life.StaggerRecoverDelay, Is.EqualTo(2.5f));
            Assert.That(settings.Life.StaggerRecoverSpeed, Is.EqualTo(8f));
            Assert.That(settings.CombatRange.TargetLayers.value, Is.EqualTo(1 << 17));
            Assert.That(settings.Movement.Gravity, Is.EqualTo(-22f));
            Assert.That(settings.Movement.GroundPull, Is.EqualTo(-2f));

            Assert.That(settings.CombatRange.FindRangeSquared, Is.EqualTo(576f));
            Assert.That(settings.CombatRange.AttackRange, Is.EqualTo(2.4f));
            Assert.That(settings.CombatRange.AttackRangeSquared, Is.EqualTo(5.76f).Within(0.0001f));
            Assert.That(settings.CombatRange.WalkStartRangeSquared, Is.EqualTo(25f));
            Assert.That(settings.CombatRange.RunStartRangeSquared, Is.EqualTo(36f));
            Assert.That(settings.CombatRange.AttackFacingDot, Is.EqualTo(Mathf.Cos(14f * Mathf.Deg2Rad)).Within(0.0001f));
            Assert.That(settings.Movement.WalkSpeed, Is.EqualTo(1.8f));
            Assert.That(settings.Movement.ChaseSpeed, Is.EqualTo(3.8f));
            Assert.That(settings.Movement.TurnSpeed, Is.EqualTo(420f));
            Assert.That(settings.Movement.AttackTurnSpeed, Is.EqualTo(180f));

            Assert.That(settings.GetAttackData(NightShadeSwordActionId.Light).PostAttackDelay, Is.EqualTo(2f));
            Assert.That(settings.GetAttackData(NightShadeSwordActionId.Combo).PostAttackDelay, Is.EqualTo(2.5f));
            Assert.That(settings.GetAttackData(NightShadeSwordActionId.Heavy).PostAttackDelay, Is.EqualTo(3f));
            Assert.That(settings.GetAttackData(NightShadeSwordActionId.WideSwing).PostAttackDelay, Is.EqualTo(2.5f));
            Assert.That(settings.GetAttackData(NightShadeSwordActionId.Combo).ComboFirstExitNormalizedTime, Is.EqualTo(0.4f));
            Assert.That(settings.GetAttackData(NightShadeSwordActionId.Combo).ComboSecondDelay, Is.EqualTo(0.15f));
            AssertAttackCorrection(
                settings,
                NightShadeSwordActionId.Light,
                0.18f);
            AssertAttackCorrection(
                settings,
                NightShadeSwordActionId.Combo,
                0.17f);
            AssertAttackCorrection(
                settings,
                NightShadeSwordActionId.Heavy,
                0.26f);
            AssertAttackCorrection(
                settings,
                NightShadeSwordActionId.WideSwing,
                0.18f);
            Assert.That(settings.AttackSelection.DistanceScoreWeight, Is.EqualTo(0.55f));
            Assert.That(settings.AttackSelection.RepeatPenalty, Is.EqualTo(0.25f));
            Assert.That(settings.AttackSelection.RandomBonusMax, Is.EqualTo(0.05f));

            AssertAttackScore(
                settings.GetAttackData(NightShadeSwordActionId.Light).Score,
                0.35f,
                0.55f,
                0.55f);
            AssertAttackScore(
                settings.GetAttackData(NightShadeSwordActionId.Combo).Score,
                0.40f,
                0.25f,
                0.35f);
            AssertAttackScore(
                settings.GetAttackData(NightShadeSwordActionId.Heavy).Score,
                0.40f,
                0.90f,
                0.30f);
            AssertAttackScore(
                settings.GetAttackData(NightShadeSwordActionId.WideSwing).Score,
                0.38f,
                0.65f,
                0.45f);

            Assert.That(settings.Recovery.MoveSpeed, Is.EqualTo(2f));
            Assert.That(settings.Recovery.MoveDuration, Is.EqualTo(0.6f));
            Assert.That(settings.Recovery.IdleBaseScore, Is.EqualTo(0.35f));
            Assert.That(settings.Recovery.IdleDistanceWeight, Is.EqualTo(0.35f));
            Assert.That(settings.Recovery.BackBaseScore, Is.EqualTo(0.25f));
            Assert.That(settings.Recovery.BackCloseWeight, Is.EqualTo(0.65f));
            Assert.That(settings.Recovery.SideBaseScore, Is.EqualTo(0.35f));
            Assert.That(settings.Recovery.SideDistanceWeight, Is.EqualTo(0.35f));
            Assert.That(settings.Recovery.RepeatPenalty, Is.EqualTo(0.20f));
            Assert.That(settings.Recovery.RandomBonusMax, Is.EqualTo(0.05f));

            Assert.That(settings.HitReaction.PushDuration, Is.EqualTo(0.18f));
            Assert.That(settings.HitReaction.KnockbackPushDuration, Is.EqualTo(0.28f));
            Assert.That(settings.HitReaction.KnockdownPushDuration, Is.EqualTo(0.38f));
            Assert.That(settings.HitReaction.KnockdownStayDuration, Is.EqualTo(0.75f));
            Assert.That(settings.HitReaction.PushCurve, Is.Not.Null);
            Assert.That(settings.HitReaction.PushCurve.length, Is.EqualTo(2));
            Assert.That(settings.Life.DeadBodyKeepTime, Is.EqualTo(3f));

            AssertDamage(
                settings.GetAttackData(NightShadeSwordActionId.Light).GetHitDamage(0),
                18f,
                AttackStrength.Heavy,
                18f,
                0.4f,
                30f,
                0.06f);
            AssertDamage(
                settings.GetAttackData(NightShadeSwordActionId.Combo).GetHitDamage(0),
                12f,
                AttackStrength.Heavy,
                12f,
                0.25f,
                20f,
                0.045f);
            AssertDamage(
                settings.GetAttackData(NightShadeSwordActionId.Combo).GetHitDamage(1),
                16f,
                AttackStrength.Heavy,
                18f,
                0.4f,
                25f,
                0.06f);
            AssertDamage(
                settings.GetAttackData(NightShadeSwordActionId.Heavy).GetHitDamage(0),
                28f,
                AttackStrength.Knockdown,
                35f,
                0.75f,
                45f,
                0.08f);
            AssertDamage(
                settings.GetAttackData(NightShadeSwordActionId.WideSwing).GetHitDamage(0),
                22f,
                AttackStrength.Heavy,
                24f,
                0.55f,
                35f,
                0.07f);
        }

        private static Component FindNightShadeSwordController(
            GameObject prefab)
        {
            Component[] components = prefab.GetComponents<Component>();
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component != null &&
                    component.GetType().FullName ==
                        "rudIsland.RPG3D.Characters.Enemies.NightShade.NightShadeSwordController")
                {
                    return component;
                }
            }

            return null;
        }

        private static void AssertAttackScore(
            NightShadeSwordAttackScoreSettings score,
            float baseScore,
            float preferredDistance,
            float distanceTolerance)
        {
            Assert.That(score.BaseScore, Is.EqualTo(baseScore));
            Assert.That(score.PreferredDistance, Is.EqualTo(preferredDistance));
            Assert.That(score.DistanceTolerance, Is.EqualTo(distanceTolerance));
        }

        private static void AssertAttackCorrection(
            NightShadeSwordSettings settings,
            NightShadeSwordActionId actionId,
            float movementEndNormalizedTime)
        {
            NightShadeSwordRuntimeAttackData attack =
                settings.GetAttackData(actionId);
            Assert.That(attack.MoveDistance, Is.Zero);
            Assert.That(attack.TargetStopDistance, Is.EqualTo(1.3f));
            Assert.That(
                attack.MaximumAddedMoveDistance,
                Is.EqualTo(0.35f));
            Assert.That(attack.MaximumTurnAngle, Is.EqualTo(20f));
            Assert.That(attack.MovementCurve, Is.Not.Null);
            Assert.That(
                attack.MovementCurve.Evaluate(movementEndNormalizedTime),
                Is.EqualTo(1f).Within(0.0001f));
        }

        private static void AssertDamage(
            AttackDamage damage,
            float healthDamage,
            AttackStrength strength,
            float staggerDamage,
            float pushDistance,
            float guardStaminaDamage,
            float hitStopDuration)
        {
            Assert.That(damage, Is.Not.Null);
            Assert.That(damage.HealthDamage, Is.EqualTo(healthDamage));
            Assert.That(damage.Strength, Is.EqualTo(strength));
            Assert.That(damage.StaggerDamage, Is.EqualTo(staggerDamage));
            Assert.That(damage.PushDistance, Is.EqualTo(pushDistance));
            Assert.That(damage.GuardStaminaDamage, Is.EqualTo(guardStaminaDamage));
            Assert.That(damage.HitStopDuration, Is.EqualTo(hitStopDuration));
            Assert.That(damage.CanBlock, Is.True);
            Assert.That(damage.DamageSoundType, Is.EqualTo(DamageSoundType.SwordCut));
        }
    }
}
