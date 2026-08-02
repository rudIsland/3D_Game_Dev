using NUnit.Framework;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman.Tests
{
    public sealed class DemonSwordsmanAttackChooserTests
    {
        private DemonSwordsmanBossSettings settings; // 행동 설정 참조
        private DemonSwordsmanAttackChooser chooser; // 내부에서 사용하는 값

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<
                DemonSwordsmanBossSettings>();
            settings.SetRuntimeDefaults();
            chooser = new DemonSwordsmanAttackChooser(settings.Attacks);
            chooser.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void Choose_거리와자세에맞는공격만고른다()
        {
            DemonSwordsmanAttackPattern swordAttack = chooser.Choose(
                DemonSwordsmanPhase.PhaseOne,
                DemonSwordsmanStyle.Sword,
                1.5f,
                10f,
                0f,
                0f);
            DemonSwordsmanAttackPattern wrongStyleAttack = chooser.Choose(
                DemonSwordsmanPhase.PhaseOne,
                DemonSwordsmanStyle.Beast,
                1.5f,
                10f,
                0f,
                0f);
            DemonSwordsmanAttackPattern tooFarAttack = chooser.Choose(
                DemonSwordsmanPhase.PhaseOne,
                DemonSwordsmanStyle.Sword,
                20f,
                10f,
                0f,
                0f);

            Assert.That(swordAttack, Is.Not.Null);
            Assert.That(swordAttack.Style, Is.EqualTo(
                DemonSwordsmanStyle.Sword));
            Assert.That(wrongStyleAttack, Is.Null);
            Assert.That(tooFarAttack, Is.Null);
        }

        [Test]
        public void Choose_같은공격을연속으로고르지않는다()
        {
            DemonSwordsmanAttackPattern firstAttack = chooser.Choose(
                DemonSwordsmanPhase.PhaseOne,
                DemonSwordsmanStyle.Sword,
                1.5f,
                10f,
                0f,
                0f);
            chooser.MarkUsed(firstAttack, 0f);

            DemonSwordsmanAttackPattern nextAttack = chooser.Choose(
                DemonSwordsmanPhase.PhaseOne,
                DemonSwordsmanStyle.Sword,
                1.5f,
                10f,
                10f,
                0f);

            Assert.That(nextAttack, Is.Not.Null);
            Assert.That(nextAttack, Is.Not.SameAs(firstAttack));
        }

        [Test]
        public void Choose_재사용시간동안이전공격을제외한다()
        {
            DemonSwordsmanAttackPattern firstAttack = chooser.Choose(
                DemonSwordsmanPhase.PhaseOne,
                DemonSwordsmanStyle.Sword,
                1.5f,
                10f,
                0f,
                0f);
            chooser.MarkUsed(firstAttack, 0f);

            DemonSwordsmanAttackPattern secondAttack = chooser.Choose(
                DemonSwordsmanPhase.PhaseOne,
                DemonSwordsmanStyle.Sword,
                1.5f,
                10f,
                0.01f,
                0f);
            Assert.That(secondAttack, Is.Not.Null);
            chooser.MarkUsed(secondAttack, 0.01f);

            DemonSwordsmanAttackPattern cooldownAttack = chooser.Choose(
                DemonSwordsmanPhase.PhaseOne,
                DemonSwordsmanStyle.Sword,
                1.5f,
                10f,
                0.02f,
                0f);

            Assert.That(cooldownAttack, Is.Not.SameAs(firstAttack));
        }
    }
}
