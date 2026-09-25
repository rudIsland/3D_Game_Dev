using System;
using System.Collections.Generic;
using Core;
using Quest;

namespace UI
{
    // 플레이어와 적의 표시 대상을 전달한다. 게임 객체를 생성하거나 갱신하지 않는다.
    public sealed class HudContainer : Singleton<HudContainer>, IDisposable
    {
        /// <summary>표시할 플레이어로 단일 컨테이너를 준비한다. 같은 플레이어는 재사용하며, 교체는 Dispose 후 수행한다.</summary>
        public static HudContainer Create(IPlayerHudSource player)
        {
            if (player == null || player.FollowTarget == null) throw new ArgumentNullException(nameof(player));
            if (CurrentInstance != null && CurrentInstance.player != player)
                throw new InvalidOperationException("기존 HudContainer를 Dispose한 뒤 플레이어를 바꾸세요.");
            return CurrentInstance ?? StoreInstance(new HudContainer(player));
        }

        // Domain Reload를 꺼도 이전 Play의 표시 대상에 접근하지 않는다.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay() { ResetInstance(); }

        private readonly IPlayerHudSource player;
        private readonly List<CombatHudController> views = new List<CombatHudController>();
        private readonly List<MinimapInfoController> minimaps = new List<MinimapInfoController>();
        private readonly List<IEnemyHudSource> enemies = new List<IEnemyHudSource>();
        // Create에서 전달한 플레이어를 HUD의 표시 대상으로 보관한다.
        private HudContainer(IPlayerHudSource player) { this.player = player; }
        public void Add(CombatHudController view)
        {
            if (view == null || views.Contains(view)) return;
            views.Add(view);
            view.Connect(player);
            foreach (IEnemyHudSource container in enemies) view.WatchEnemies(container);
        }
        /// <summary>미니맵에 같은 플레이어를 전달하고 반환 시 함께 해제할 대상으로 기록한다.</summary>
        public void Add(MinimapInfoController view, GroundQuestProgress quest = null)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (minimaps.Contains(view)) return;
            minimaps.Add(view);
            view.Connect(player.FollowTarget, quest);
        }

        public void AddEnemies(IEnemyHudSource container)
        {
            if (enemies.Contains(container)) return;
            enemies.Add(container);
            for (int i = views.Count - 1; i >= 0; i--)
            {
                if (views[i] == null) views.RemoveAt(i);
                else views[i].WatchEnemies(container);
            }
        }
        public void RemoveEnemies(IEnemyHudSource container)
        {
            enemies.Remove(container);
            foreach (CombatHudController view in views)
                if (view != null) view.StopWatchingEnemies(container);
        }
        /// <summary>HUD 연결을 정리하고 단일 인스턴스를 해제한다. 다음 사용 전에 Create를 호출한다.</summary>
        public void Dispose()
        {
            foreach (CombatHudController view in views)
                if (view != null) view.Disconnect();
            foreach (MinimapInfoController minimap in minimaps)
                if (minimap != null) minimap.Disconnect();
            minimaps.Clear();
            views.Clear();
            enemies.Clear();
            ClearInstance();
        }
    }
}
