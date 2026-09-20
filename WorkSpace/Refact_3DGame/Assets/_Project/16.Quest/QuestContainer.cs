using System;
using System.Collections.Generic;
using Characters.Player.Lifecycle;

namespace World.Quests
{
    // 진행 기록은 게임 동안 유지하고 씬의 판정 컴포넌트는 연결·해제한다.
    public sealed class QuestContainer : IDisposable
    {
        public GroundQuestProgress Ground { get; } = new GroundQuestProgress();
        private readonly List<GroundQuestController> controllers = new List<GroundQuestController>();
        private readonly PlayerController player;
        private bool disposed;
        public QuestContainer(PlayerController player) { this.player = player; }

        public void Connect(GroundQuestController controller)
        {
            if (disposed) throw new ObjectDisposedException(nameof(QuestContainer));
            if (controller == null || controllers.Contains(controller)) return;
            controller.Connect(player, Ground);
            controllers.Add(controller);
        }
        public void Tick(float deltaTime)
        {
            if (disposed || player == null || player.IsPaused || deltaTime <= 0f) return;
            for (int i = controllers.Count - 1; i >= 0; i--)
            {
                if (controllers[i] == null) controllers.RemoveAt(i);
                else if (controllers[i].isActiveAndEnabled) controllers[i].Tick();
            }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            for (int i = 0; i < controllers.Count; i++)
                if (controllers[i] != null) controllers[i].Disconnect();
            controllers.Clear();
        }
    }
}
