using System;
using System.Collections.Generic;
using Characters.Enemies;
using Characters.Player.Lifecycle;
using GameUI.CombatHud;

namespace GameUI
{
    // 플레이어와 적의 표시 대상을 전달한다. 게임 객체를 생성하거나 갱신하지 않는다.
    public sealed class HudContainer : IDisposable
    {
        private readonly PlayerController player;
        private readonly List<CombatHudController> views = new List<CombatHudController>();
        private readonly List<EnemyContainer> enemies = new List<EnemyContainer>();
        public HudContainer(PlayerController player) { this.player = player; }
        public void Add(CombatHudController view)
        {
            if (view == null || views.Contains(view)) return;
            views.Add(view);
            view.Connect(player);
            foreach (EnemyContainer container in enemies) view.WatchEnemies(container);
        }
        public void AddEnemies(EnemyContainer container)
        {
            if (enemies.Contains(container)) return;
            enemies.Add(container);
            for (int i = views.Count - 1; i >= 0; i--)
            {
                if (views[i] == null) views.RemoveAt(i);
                else views[i].WatchEnemies(container);
            }
        }
        public void RemoveEnemies(EnemyContainer container)
        {
            enemies.Remove(container);
            foreach (CombatHudController view in views)
                if (view != null) view.StopWatchingEnemies(container);
        }
        public void Dispose()
        {
            foreach (CombatHudController view in views)
                if (view != null) view.Disconnect();
            views.Clear();
            enemies.Clear();
        }
    }
}
