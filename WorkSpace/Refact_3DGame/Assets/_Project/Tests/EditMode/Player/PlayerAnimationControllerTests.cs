using System.Reflection;
using NUnit.Framework;
using rudIsland.RPG3D.Player.Animations;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class PlayerAnimationControllerTests
    {
        [Test]
        public void PlayerHitFullPathId_BaseLayer전체경로를사용한다()
        {
            FieldInfo fullPathIdField =
                typeof(PlayerAnimationController).GetField(
                    "PlayerHitFullPathId",
                    BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(fullPathIdField, Is.Not.Null);

            int fullPathId = (int)fullPathIdField.GetValue(null);
            int expectedFullPathId =
                Animator.StringToHash("Base Layer.PlayerHit");
            int shortNameId = Animator.StringToHash("PlayerHit");

            Assert.That(fullPathId, Is.EqualTo(expectedFullPathId));
            Assert.That(fullPathId, Is.Not.EqualTo(shortNameId));
        }
    }
}
