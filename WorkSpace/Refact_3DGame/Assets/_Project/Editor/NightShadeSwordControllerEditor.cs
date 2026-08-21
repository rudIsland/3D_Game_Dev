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
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Play Mode 전투 선택",
                EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("상위 상태", controller.DebugTopStateName);
                EditorGUILayout.TextField("Combat 단계", controller.DebugCombatPhaseName);
                EditorGUILayout.TextField("현재 Action", controller.DebugCurrentActionName);
                EditorGUILayout.TextField("현재 중단 상태", controller.DebugCurrentStopReasonName);
                EditorGUILayout.TextField("마지막 평가 단계", controller.DebugLastEvaluatedPhaseName);
                EditorGUILayout.TextField("선택된 Action", controller.DebugSelectedActionName);
                EditorGUILayout.TextField("이전 종료 사유", controller.DebugPreviousStopReasonName);
            }

            DrawCandidateHeader();
            for (int index = 0; index < controller.DebugCandidateCount; index++)
            {
                DrawCandidate(controller, index);
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
            NightShadeSwordController controller,
            int index)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string actionName = controller.GetDebugCandidateActionName(index);
                if (controller.GetDebugCandidateIsSelected(index))
                {
                    actionName = $"> {actionName}";
                }

                GUILayout.Label(actionName, GUILayout.Width(90f));
                GUILayout.Label(
                    controller.GetDebugCandidateCanStart(index)
                        ? "가능"
                        : controller.GetDebugCandidateRejectReasonName(index),
                    GUILayout.Width(110f));
                DrawScore(controller.GetDebugCandidateBaseScore(index));
                DrawScore(controller.GetDebugCandidateDistanceScore(index));
                DrawScore(controller.GetDebugCandidateRepeatPenalty(index));
                DrawScore(controller.GetDebugCandidateRandomBonus(index));
                DrawScore(controller.GetDebugCandidateFinalScore(index));
            }
        }

        private static void DrawScore(float score)
        {
            GUILayout.Label(score.ToString("0.00"), GUILayout.Width(42f));
        }
    }
}
