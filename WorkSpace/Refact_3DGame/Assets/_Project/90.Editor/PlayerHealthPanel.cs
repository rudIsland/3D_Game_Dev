using Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorTools
{
    // 치트 창의 체력 버튼을 플레이어의 Editor 전용 API에 연결한다.
    public sealed class PlayerHealthPanel
    {
        private VisualElement view;
        private FloatField damage;
        private Label health;
        private Label result;
        private Button decrease;
        private Button hit;
        private Button kill;

        /// <summary>플레이어 탭에 붙일 체력 조회·감소·피격·즉사 영역을 만든다.</summary>
        public VisualElement CreateView()
        {
            view = new VisualElement { name = "playerHealthPanel" };
            view.AddToClassList("cheat-section");
            var title = new Label("플레이어 체력");
            title.AddToClassList("section-title");
            view.Add(title);
            health = new Label { name = "cheatHealth" };
            view.Add(health);
            damage = new FloatField("감소량") { name = "cheatDamage", value = 10f };
            damage.RegisterValueChangedCallback(_ => Refresh());
            view.Add(damage);
            var actions = new VisualElement();
            actions.AddToClassList("actions");
            decrease = new Button(() => Run(false)) { text = "체력 감소", name = "decreaseHealth" };
            decrease.tooltip = "방어·구르기 판정 없이 체력을 감소시킵니다. 체력 변경·사망 이벤트는 실행됩니다.";
            decrease.AddToClassList("action");
            actions.Add(decrease);
            hit = new Button(() => Run(true)) { text = "피격 처리", name = "applyHit" };
            hit.tooltip = "기존 피격 API를 호출합니다. 구르기 무적 등 판정 결과를 확인할 수 있습니다.";
            hit.AddToClassList("action");
            actions.Add(hit);
            kill = new Button(() =>
            {
                var player = PlayerController.Instance;
                result.text = CanRun(player) && player.CheatKill() ? "즉사 처리 완료" : "실행 가능한 플레이어가 없습니다.";
                Refresh();
            }) { text = "즉사", name = "killPlayer" };
            kill.AddToClassList("action");
            kill.AddToClassList("danger");
            actions.Add(kill);
            view.Add(actions);
            result = new Label { name = "healthCheatResult" };
            result.AddToClassList("selection");
            view.Add(result);
            Refresh();
            return view;
        }

        // 실행 직전에 플레이어와 수치를 다시 확인해 반환·Play 종료 후 요청을 막는다.
        private void Run(bool useHit)
        {
            var player = PlayerController.Instance;
            if (!CanRun(player) || !ValidDamage()) { result.text = "살아 있는 플레이어와 양수의 감소량이 필요합니다."; return; }
            float before = player.RuntimeUnit.CurrentHealth;
            string outcome = useHit ? player.CheatTakeHit(damage.value).ToString() :
                player.CheatDecreaseHealth(damage.value) + " 감소";
            result.text = $"{outcome}: {before:0.##} → {player.RuntimeUnit.CurrentHealth:0.##}";
            Refresh();
        }

        private bool ValidDamage() => damage.value > 0f && !float.IsNaN(damage.value) && !float.IsInfinity(damage.value);
        private static bool CanRun(PlayerController player) => !EditorApplication.isCompiling &&
            EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode && player != null && player.CanRunCheat;

        /// <summary>현재 플레이어와 체력·사망 상태를 읽어 버튼을 갱신한다.</summary>
        public void Refresh()
        {
            if (view == null) return;
            var player = PlayerController.Instance;
            bool canRun = CanRun(player);
            health.text = player == null || !player.IsReady ? "플레이어 준비 전 / Play 중에 실행할 수 있습니다." :
                $"체력 {player.RuntimeUnit.CurrentHealth:0.##} / {player.RuntimeUnit.Health.MaxHealth:0.##}" + (player.IsDead ? " · 사망" : "");
            decrease.SetEnabled(canRun && ValidDamage());
            hit.SetEnabled(canRun && ValidDamage());
            kill.SetEnabled(canRun);
        }
    }
}
