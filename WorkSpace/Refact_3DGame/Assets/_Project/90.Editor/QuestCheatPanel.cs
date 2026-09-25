using System;
using Boot;
using Quest;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorTools
{
    // 퀘스트 기록 API를 치트 입력에 연결하고 현재 단계·실행 결과를 표시한다.
    public sealed class QuestCheatPanel
    {
        private Boots boot;
        private GroundQuestProgress current;
        private Label status;
        private Label result;
        private Button book;
        private Button exchange;
        private Button exit;
        private Button complete;
        private Button reset;

        /// <summary>지상 퀘스트 진행과 기존 기록 함수를 실행할 버튼을 만든다.</summary>
        public VisualElement CreateView()
        {
            var view = new VisualElement { name = "questCheatPanel" };
            view.AddToClassList("cheat-section");
            var title = new Label("지상 탐험 퀘스트");
            title.AddToClassList("section-title");
            view.Add(title);
            status = new Label { name = "questStatus" };
            status.AddToClassList("selection");
            view.Add(status);
            var help = new Label("진행 기록만 변경합니다. 아이템 획득·교환·소모와 맵 이동은 아이템·플레이어 탭에서 따로 실행하세요. 초기화는 가방과 맵 객체를 복원하지 않습니다.");
            help.AddToClassList("selection");
            view.Add(help);
            var steps = new VisualElement();
            steps.AddToClassList("actions");
            book = AddButton(steps, "책 발견 기록", "questBook", quest => quest.RecordBookFound());
            exchange = AddButton(steps, "교환 완료 기록", "questExchange", quest => quest.RecordBookExchanged());
            exit = AddButton(steps, "출구 도착 기록", "questExit", quest => quest.RecordExitReached());
            view.Add(steps);
            var actions = new VisualElement();
            actions.AddToClassList("actions");
            complete = AddButton(actions, "전체 완료", "questComplete", quest =>
            {
                quest.RecordBookFound();
                quest.RecordBookExchanged();
                quest.RecordExitReached();
            });
            reset = AddButton(actions, "진행 초기화", "questReset", quest => quest.CheatReset());
            reset.tooltip = "진행 기록만 책 찾기로 되돌립니다. 이후 실제 획득·교환·출구 판정이 발생하면 다시 진행됩니다.";
            view.Add(actions);
            result = new Label { name = "questCheatResult" };
            result.AddToClassList("selection");
            view.Add(result);
            Refresh();
            return view;
        }

        // 버튼별 실행을 하나의 진입점으로 모아 준비 상태와 이전 기록을 다시 확인한다.
        private Button AddButton(VisualElement parent, string title, string name, Action<GroundQuestProgress> action)
        {
            var button = new Button(() => Run(title, action)) { text = title, name = name };
            button.AddToClassList("action");
            parent.Add(button);
            return button;
        }

        // 반환·맵 전환 중인 기록에 접근하지 않고 게임 소유자에게 현재 대상을 요청한다.
        private bool TryGetQuest(out GroundQuestProgress quest)
        {
            quest = null;
            if (!EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return false;
            if (boot == null) boot = UnityEngine.Object.FindFirstObjectByType<Boots>();
            return boot != null && boot.TryGetCheatQuest(out quest);
        }

        // 기존 기록 함수를 실행해 단계 변경 알림이 HUD까지 전달되도록 한다.
        private void Run(string title, Action<GroundQuestProgress> action)
        {
            if (!TryGetQuest(out var quest) || !ReferenceEquals(quest, current)) { Refresh(); return; }
            GroundQuestStep before = quest.Step;
            action(quest);
            result.text = title + ": " + StepName(before) + " → " + StepName(quest.Step);
            Refresh();
        }

        /// <summary>현재 기록·단계를 읽고 준비·완료 상태에 맞춰 버튼을 갱신한다.</summary>
        public void Refresh()
        {
            if (status == null) return;
            bool ready = TryGetQuest(out var quest);
            if (!ReferenceEquals(current, quest)) result.text = string.Empty;
            current = quest;
            GroundQuestStep step = quest != null ? quest.Step : GroundQuestStep.FindBook;
            status.text = ready ? "현재 단계: " + StepName(step) : "Play에서 맵·플레이어·퀘스트 준비를 기다리는 중입니다.";
            book.SetEnabled(ready && step == GroundQuestStep.FindBook);
            exchange.SetEnabled(ready && step < GroundQuestStep.ReachExit);
            exit.SetEnabled(ready && step == GroundQuestStep.ReachExit);
            complete.SetEnabled(ready && step != GroundQuestStep.Complete);
            reset.SetEnabled(ready && step != GroundQuestStep.FindBook);
        }

        // enum 값은 실행 계약으로 유지하고 창에만 읽기 쉬운 단계 이름을 표시한다.
        private static string StepName(GroundQuestStep step)
        {
            switch (step)
            {
                case GroundQuestStep.FindBook: return "1. 책 찾기";
                case GroundQuestStep.ExchangeBook: return "2. 책을 스크롤로 교환";
                case GroundQuestStep.ReachExit: return "3. 출구로 이동";
                default: return "완료";
            }
        }
    }
}
