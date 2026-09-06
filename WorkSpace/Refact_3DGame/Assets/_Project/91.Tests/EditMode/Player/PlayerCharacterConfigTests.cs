using NUnit.Framework;
using Characters.Player.Lifecycle;
using Characters.Player.Config;
using Characters.Player.Combat.Attack;
using Characters.Player.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests.Player
{
    public sealed class PlayerCharacterConfigTests
    {
        private const string ConfigPath =
            "Assets/_Project/Characters/Player/Configs/PlayerCharacterConfig.asset";
        private const string ScenePath =
            "Assets/_Project/Scenes/Dev/CharacterTestScene.unity";

        [Test]
        public void RuntimeConfig_이전한Player값과계산값을보관한다()
        {
            PlayerCharacterConfig config =
                AssetDatabase.LoadAssetAtPath<PlayerCharacterConfig>(ConfigPath);

            Assert.That(config, Is.Not.Null);
            PlayerCharacterRuntimeConfig runtime =
                config.CreateRuntimeConfig();

            Assert.That(runtime.Movement.WalkSpeed, Is.EqualTo(2.8f));
            Assert.That(runtime.Movement.SprintSpeed, Is.EqualTo(5.5f));
            Assert.That(runtime.Movement.Gravity, Is.EqualTo(-22f));
            Assert.That(runtime.Combat.MaxHealth, Is.EqualTo(100f));
            Assert.That(runtime.Combat.MaxStamina, Is.EqualTo(100f));
            Assert.That(
                runtime.Combat.MinimumGuardDot,
                Is.EqualTo(Mathf.Cos(60f * Mathf.Deg2Rad)).Within(0.000001f));
            Assert.That(runtime.Target.FindRange, Is.EqualTo(12f));
            Assert.That(runtime.Target.BreakDistanceSquared, Is.EqualTo(225f));
            Assert.That(runtime.Attacks.Attacks.Length, Is.EqualTo(6));
            Assert.That(runtime.Attacks.ComboCloseNormalizedTime, Is.EqualTo(0.9f));
        }

        [Test]
        public void CharacterTestScene_Config와역할컴포넌트가연결되어있다()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive);
            }
            try
            {
                PlayerController controller = FindInScene<PlayerController>(scene);
                Assert.That(controller, Is.Not.Null);

                var serializedController = new SerializedObject(controller);
                Assert.That(
                    serializedController.FindProperty("config")
                        .objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serializedController.FindProperty("weaponHitShape")
                        .objectReferenceValue,
                    Is.TypeOf<PlayerWeaponHitShape>());
                Assert.That(
                    serializedController.FindProperty("maxHealth"),
                    Is.Null);
                Assert.That(
                    controller.GetComponentInChildren<PlayerFootstepAudio>(true),
                    Is.Not.Null);
            }
            finally
            {
                if (openedForTest)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
                if (previousScene.IsValid())
                {
                    SceneManager.SetActiveScene(previousScene);
                }
            }
        }

        private static T FindInScene<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                T found = roots[index].GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
