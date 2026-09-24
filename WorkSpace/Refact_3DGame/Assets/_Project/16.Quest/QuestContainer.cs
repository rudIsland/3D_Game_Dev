using Core;
using System;
using System.Collections.Generic;
using Characters.Player.Lifecycle;

namespace World.Quests
{
    // 진행 기록은 게임 동안 유지하고 씬의 판정 컴포넌트는 연결·해제한다.
    public sealed class QuestContainer : Singleton<QuestContainer>, IDisposable
    {
        /// <summary>플레이어를 연결해 단일 컨테이너를 준비한다. 같은 플레이어는 재사용하며, 교체는 Dispose 후 수행한다.</summary>
        public static QuestContainer Create(PlayerController player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (CurrentInstance != null && CurrentInstance.player != player)
                throw new InvalidOperationException("기존 QuestContainer를 Dispose한 뒤 플레이어를 바꾸세요.");
            return CurrentInstance ?? StoreInstance(new QuestContainer(player));
        }

        // Domain Reload를 꺼도 이전 Play의 진행 기록을 다시 사용하지 않는다.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay() { ResetInstance(); }

        public GroundQuestProgress Ground { get; } = new GroundQuestProgress();
        private readonly List<GroundQuestController> controllers = new List<GroundQuestController>();
        private readonly PlayerController player;
        private bool disposed;
        // Create에서 전달한 플레이어와 새 퀘스트 진행 기록을 연결한다.
        private QuestContainer(PlayerController player) { this.player = player; }

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
        /// <summary>판정 컴포넌트를 연결 해제하고 단일 인스턴스를 해제한다. 다시 Create하면 새 진행 기록으로 시작한다.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            for (int i = 0; i < controllers.Count; i++)
                if (controllers[i] != null) controllers[i].Disconnect();
            controllers.Clear();
            ClearInstance();
        }
    }
}
