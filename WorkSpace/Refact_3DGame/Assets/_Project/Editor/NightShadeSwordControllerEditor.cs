using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEditor;
using UnityEngine;

namespace rudIsland.RPG3D.Editor
{
    [CustomEditor(typeof(NightShadeSwordController))]
    public sealed class NightShadeSwordControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying)
            {
                return;
            }

            var controller = (NightShadeSwordController)target;
            NightShadeSwordCombatDebug combatDebug = controller.CombatDebug;
            if (combatDebug == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Play Mode 전투 선택",
                EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("상위 상태", combatDebug.TopState.ToString());
                EditorGUILayout.TextField("Combat 단계", combatDebug.CombatPhase.ToString());
                EditorGUILayout.TextField("현재 Action", combatDebug.CurrentAction.ToString());
                EditorGUILayout.TextField("현재 중단 상태", combatDebug.CurrentActionStopReason.ToString());
                EditorGUILayout.TextField("마지막 평가 단계", combatDebug.LastEvaluatedPhase.ToString());
                EditorGUILayout.TextField("선택된 Action", combatDebug.SelectedAction.ToString());
                EditorGUILayout.TextField("이전 종료 사유", combatDebug.PreviousActionStopReason.ToString());
            }

            DrawCandidateHeader();
            for (int index = 0; index < combatDebug.CandidateCount; index++)
            {
                DrawCandidate(combatDebug, index);
            }

            if (Event.current.type == EventType.Layout)
            {
                Repaint();
            }
        }

        private static void DrawCandidateHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Action", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
                GUILayout.Label("가능/거절", EditorStyles.miniBoldLabel, GUILayout.Width(110f));
                GUILayout.Label("기본", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
                GUILayout.Label("거리", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
                GUILayout.Label("반복", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
                GUILayout.Label("난수", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
                GUILayout.Label("최종", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
            }
        }

        private static void DrawCandidate(
            NightShadeSwordCombatDebug combatDebug,
            int index)
        {
            NightShadeSwordActionDebugEntry candidate =
                combatDebug.Candidates[index];
            using (new EditorGUILayout.HorizontalScope())
            {
                string actionName = candidate.ActionId.ToString();
                if (candidate.IsSelected)
                {
                    actionName = $"> {actionName}";
                }

                GUILayout.Label(actionName, GUILayout.Width(90f));
                GUILayout.Label(
                    candidate.CanStart
                        ? "가능"
                        : candidate.RejectReason.ToString(),
                    GUILayout.Width(110f));
                DrawScore(candidate.Score.BaseScore);
                DrawScore(candidate.Score.DistanceScore);
                DrawScore(candidate.Score.RepeatPenalty);
                DrawScore(candidate.Score.RandomBonus);
                DrawScore(candidate.Score.FinalScore);
            }
        }

        private static void DrawScore(float score)
        {
            GUILayout.Label(score.ToString("0.00"), GUILayout.Width(42f));
        }
    }
}
