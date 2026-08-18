using NUnit.Framework;
using rudIsland.RPG3D.Player.Runtime;
using rudIsland.RPG3D.Player.States;
using rudIsland.RPG3D.Player.States.Actions;

namespace rudIsland.RPG3D.Tests
{
    public sealed class PlayerActionInputBufferTests
    {
        [Test]
        public void Update_같은프레임공격과구르기는구르기를예약한다()
        {
            var inputBuffer = new PlayerActionInputBuffer(0.2f);

            inputBuffer.Update(0f, true, true);

            Assert.That(
                inputBuffer.CurrentAction,
                Is.EqualTo(PlayerBufferedAction.Roll));
        }

        [Test]
        public void Update_새입력은기존예약을교체한다()
        {
            var inputBuffer = new PlayerActionInputBuffer(0.2f);
            inputBuffer.Update(0f, false, true);

            inputBuffer.Update(0.05f, true, false);

            Assert.That(
                inputBuffer.CurrentAction,
                Is.EqualTo(PlayerBufferedAction.Roll));
        }

        [Test]
        public void Update_예약시간이지나면입력을지운다()
        {
            var inputBuffer = new PlayerActionInputBuffer(0.2f);
            inputBuffer.Update(0f, false, true);

            inputBuffer.Update(0.2f, false, false);
            Assert.That(
                inputBuffer.CurrentAction,
                Is.EqualTo(PlayerBufferedAction.Attack));

            inputBuffer.Update(0.001f, false, false);
            Assert.That(
                inputBuffer.CurrentAction,
                Is.EqualTo(PlayerBufferedAction.None));
        }

        [Test]
        public void Clear_예약행동을모두지운다()
        {
            var inputBuffer = new PlayerActionInputBuffer(0.2f);
            inputBuffer.Update(0f, false, true);

            inputBuffer.Clear();

            Assert.That(
                inputBuffer.CurrentAction,
                Is.EqualTo(PlayerBufferedAction.None));
        }

        [Test]
        public void TryConsume_스태미나가부족하면값을소비하지않는다()
        {
            var stamina = new PlayerStamina(20f, 0.8f, 35f);

            Assert.That(stamina.TryConsume(25f), Is.False);
            Assert.That(stamina.CurrentStamina, Is.EqualTo(20f));
        }

        [Test]
        public void ShouldStartRunAttack_직전실제달리기기록만사용한다()
        {
            Assert.That(
                PlayerStateMachine.ShouldStartRunAttack(false),
                Is.False);
            Assert.That(
                PlayerStateMachine.ShouldStartRunAttack(true),
                Is.True);
        }
    }
}
