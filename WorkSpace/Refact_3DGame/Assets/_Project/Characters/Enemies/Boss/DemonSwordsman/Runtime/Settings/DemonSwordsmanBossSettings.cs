using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    [Serializable]
    public sealed class DemonSwordsmanAttackPattern
    {
        [SerializeField] private DemonSwordsmanAttackKind kind;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string animatorStateName = string.Empty;
        [SerializeField] private DemonSwordsmanPhaseMask phaseMask;
        [SerializeField] private DemonSwordsmanStyle style;
        [SerializeField, Min(0f)] private float minimumDistance;
        [SerializeField, Min(0.1f)] private float maximumDistance = 2f;
        [SerializeField, Range(1f, 180f)] private float maximumAngle = 45f;
        [SerializeField, Min(0.01f)] private float selectionWeight = 1f;
        [SerializeField, Min(0f)] private float cooldown;
        [SerializeField, Range(0.01f, 0.3f)] private float crossFadeTime = 0.1f;
        [SerializeField, Min(0.01f)] private float warningTime = 0.32f;
        [SerializeField, Min(0.01f)] private float activeTime = 0.35f;
        [SerializeField, Min(0.01f)] private float recoveryTime = 0.5f;
        [SerializeField, Range(0f, 1f)] private float stopTurningAtNormalizedTime = 0.45f;
        [SerializeField, Range(0f, 2f)] private float rootMoveMultiplier = 1f;
        [SerializeField, Range(0.5f, 1.12f)] private float animationSpeed = 1f;
        [SerializeField] private bool jumpAttack;
        [SerializeField] private bool hasBranch;
        [SerializeField, Min(0f)] private float branchTime;
        [SerializeField] private DemonSwordsmanAttackKind closeBranchKind;
        [SerializeField] private DemonSwordsmanAttackKind farBranchKind;

        [NonSerialized] private int animatorStateHash;

        public DemonSwordsmanAttackKind Kind => kind;
        public string DisplayName => displayName;
        public int AnimatorStateHash
        {
            get
            {
                if (animatorStateHash == 0 && !string.IsNullOrEmpty(animatorStateName))
                {
                    animatorStateHash = Animator.StringToHash(animatorStateName);
                }

                return animatorStateHash;
            }
        }

        public DemonSwordsmanPhaseMask PhaseMask => phaseMask;
        public DemonSwordsmanStyle Style => style;
        public float MinimumDistance => minimumDistance;
        public float MaximumDistance => maximumDistance;
        public float MaximumAngle => maximumAngle;
        public float SelectionWeight => selectionWeight;
        public float Cooldown => cooldown;
        public float CrossFadeTime => crossFadeTime;
        public float WarningTime => warningTime;
        public float ActiveTime => activeTime;
        public float RecoveryTime => recoveryTime;
        public float TotalTime => warningTime + activeTime + recoveryTime;
        public float StopTurningAtNormalizedTime => stopTurningAtNormalizedTime;
        public float RootMoveMultiplier => rootMoveMultiplier;
        public float AnimationSpeed => animationSpeed;
        public bool IsJumpAttack => jumpAttack;
        public bool HasBranch =>
            hasBranch ||
            kind == DemonSwordsmanAttackKind.SwordCombo ||
            kind == DemonSwordsmanAttackKind.BeastCombo;
        public float BranchTime => branchTime > 0f
            ? branchTime
            : warningTime + activeTime * 0.55f;
        public DemonSwordsmanAttackKind CloseBranchKind =>
            kind == DemonSwordsmanAttackKind.SwordCombo
                ? DemonSwordsmanAttackKind.QuickSlash
                : kind == DemonSwordsmanAttackKind.BeastCombo
                    ? DemonSwordsmanAttackKind.BeastWideAttack
                    : closeBranchKind;
        public DemonSwordsmanAttackKind FarBranchKind =>
            kind == DemonSwordsmanAttackKind.SwordCombo
                ? DemonSwordsmanAttackKind.ChaseSlash
                : kind == DemonSwordsmanAttackKind.BeastCombo
                    ? DemonSwordsmanAttackKind.BeastRush
                    : farBranchKind;

        internal void Configure(
            DemonSwordsmanAttackKind attackKind,
            string attackDisplayName,
            string stateName,
            DemonSwordsmanPhaseMask availablePhase,
            DemonSwordsmanStyle attackStyle,
            float minDistance,
            float maxDistance,
            float maxAngle,
            float weight,
            float reuseTime,
            float fadeTime,
            float warning,
            float active,
            float recovery,
            float stopTurningNormalized,
            float rootMoveScale,
            float speed,
            bool isJump)
        {
            kind = attackKind;
            displayName = attackDisplayName;
            animatorStateName = stateName;
            phaseMask = availablePhase;
            style = attackStyle;
            minimumDistance = Mathf.Max(0f, minDistance);
            maximumDistance = Mathf.Max(minimumDistance + 0.1f, maxDistance);
            maximumAngle = Mathf.Clamp(maxAngle, 1f, 180f);
            selectionWeight = Mathf.Max(0.01f, weight);
            cooldown = Mathf.Max(0f, reuseTime);
            crossFadeTime = Mathf.Clamp(fadeTime, 0.01f, 0.3f);
            warningTime = Mathf.Max(0.01f, warning);
            activeTime = Mathf.Max(0.01f, active);
            recoveryTime = Mathf.Max(0.01f, recovery);
            stopTurningAtNormalizedTime = Mathf.Clamp01(stopTurningNormalized);
            rootMoveMultiplier = Mathf.Clamp(rootMoveScale, 0f, 2f);
            animationSpeed = Mathf.Clamp(speed, 0.5f, 1.12f);
            jumpAttack = isJump;
            SetBranchDefaults();
            animatorStateHash = Animator.StringToHash(animatorStateName);
        }

        private void SetBranchDefaults()
        {
            hasBranch = kind == DemonSwordsmanAttackKind.SwordCombo ||
                kind == DemonSwordsmanAttackKind.BeastCombo;
            branchTime = warningTime + activeTime * 0.55f;

            switch (kind)
            {
                case DemonSwordsmanAttackKind.SwordCombo:
                    closeBranchKind = DemonSwordsmanAttackKind.QuickSlash;
                    farBranchKind = DemonSwordsmanAttackKind.ChaseSlash;
                    branchTime = 1.801f;
                    break;
                case DemonSwordsmanAttackKind.BeastCombo:
                    closeBranchKind =
                        DemonSwordsmanAttackKind.BeastWideAttack;
                    farBranchKind = DemonSwordsmanAttackKind.BeastRush;
                    branchTime = 2.021f;
                    break;
            }
        }
    }

    [CreateAssetMenu(
        fileName = "DemonSwordsmanBossSettings",
        menuName = "RPG3D/Boss/Demon Swordsman Settings")]
    public sealed class DemonSwordsmanBossSettings : ScriptableObject
    {
        [Header("생명과 탐지")]
        [SerializeField, Min(1f)] private float maxHealth = 1000f;
        [SerializeField, Min(1f)] private float findRange = 25f;

        [Header("이동")]
        [SerializeField, Min(0.1f)] private float phaseOneMoveSpeed = 3.4f;
        [SerializeField, Min(1f)] private float phaseOneTurnSpeed = 360f;
        [SerializeField, Min(0.1f)] private float preferredDistance = 2.5f;
        [SerializeField, Min(0.1f)] private float tooCloseDistance = 1.25f;
        [SerializeField, Min(0.1f)] private float circleSpeed = 2.1f;
        [SerializeField, Min(0.1f)] private float backAwaySpeed = 2.5f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundPull = -2f;
        [SerializeField, Range(1f, 1.5f)] private float phaseTwoMoveMultiplier = 1.12f;
        [SerializeField, Range(1f, 1.5f)] private float phaseTwoTurnMultiplier = 1.2f;

        [Header("상태 시간")]
        [SerializeField, Min(0.01f)] private float noticeTime = 0.7f;
        [SerializeField, Min(0.01f)] private float circleTime = 0.65f;
        [SerializeField, Min(0.01f)] private float backAwayTime = 0.45f;
        [SerializeField, Min(0.01f)] private float phaseFearTime = 0.85f;
        [SerializeField, Min(0.01f)] private float phaseRageTime = 1.8f;
        [SerializeField, Min(0.6f)] private float phaseRepositionTime = 0.6f;
        [SerializeField, Range(0f, 1f)] private float phaseSwordStoreNormalizedTime = 0.45f;
        [SerializeField, Min(0.01f)] private float styleChangeTime = 0.9f;

        [Header("공격")]
        [SerializeField] private DemonSwordsmanAttackPattern[] attacks =
            Array.Empty<DemonSwordsmanAttackPattern>();

        public float MaxHealth => maxHealth;
        public float FindRange => findRange;
        public float PhaseOneMoveSpeed => phaseOneMoveSpeed;
        public float PhaseOneTurnSpeed => phaseOneTurnSpeed;
        public float PreferredDistance => preferredDistance;
        public float TooCloseDistance => tooCloseDistance;
        public float CircleSpeed => circleSpeed;
        public float BackAwaySpeed => backAwaySpeed;
        public float Gravity => gravity;
        public float GroundPull => groundPull;
        public float PhaseTwoMoveMultiplier => phaseTwoMoveMultiplier;
        public float PhaseTwoTurnMultiplier => phaseTwoTurnMultiplier;
        public float NoticeTime => noticeTime;
        public float CircleTime => circleTime;
        public float BackAwayTime => backAwayTime;
        public float PhaseFearTime => phaseFearTime;
        public float PhaseRageTime => phaseRageTime;
        public float PhaseRepositionTime => phaseRepositionTime;
        public float PhaseSwordStoreNormalizedTime => phaseSwordStoreNormalizedTime;
        public float StyleChangeTime => styleChangeTime;
        public DemonSwordsmanAttackPattern[] Attacks => attacks;

        internal void SetRuntimeDefaults()
        {
            attacks = new[]
            {
                CreateAttack(
                    DemonSwordsmanAttackKind.QuickSlash,
                    "빠른 검 베기",
                    "Base Layer.Sword.SwordQuickSlash",
                    DemonSwordsmanPhaseMask.Both,
                    DemonSwordsmanStyle.Sword,
                    0f, 2.4f, 55f, 2.2f, 0.8f,
                    0.09f, 0.875f, 0.765f, 0.957f,
                    0.55f, 0.85f, 1f, false),
                CreateAttack(
                    DemonSwordsmanAttackKind.SwordCombo,
                    "검 2연속 공격",
                    "Base Layer.Sword.SwordComboStart",
                    DemonSwordsmanPhaseMask.Both,
                    DemonSwordsmanStyle.Sword,
                    0f, 2.7f, 50f, 1.8f, 2.2f,
                    0.08f, 0.931f, 1.056f, 0.962f,
                    0.42f, 0.9f, 1.02f, false),
                CreateAttack(
                    DemonSwordsmanAttackKind.HeavySlash,
                    "지연 강공격",
                    "Base Layer.Sword.SwordHeavySlash",
                    DemonSwordsmanPhaseMask.Both,
                    DemonSwordsmanStyle.Sword,
                    1.2f, 3.7f, 42f, 1.1f, 5.5f,
                    0.11f, 1.273f, 0.49f, 0.563f,
                    0.32f, 1.05f, 0.98f, false),
                CreateAttack(
                    DemonSwordsmanAttackKind.ChaseSlash,
                    "추격 베기",
                    "Base Layer.Sword.SwordChaseSlash",
                    DemonSwordsmanPhaseMask.Both,
                    DemonSwordsmanStyle.Sword,
                    1.8f, 5.1f, 36f, 1.4f, 3.4f,
                    0.1f, 1.242f, 0.807f, 0.9f,
                    0.4f, 1.25f, 1.02f, false),
                CreateAttack(
                    DemonSwordsmanAttackKind.JumpSlash,
                    "점프 공격",
                    "Base Layer.Sword.SwordJumpAttack",
                    DemonSwordsmanPhaseMask.Both,
                    DemonSwordsmanStyle.Sword,
                    3f, 7.5f, 30f, 0.9f, 6f,
                    0.12f, 1.102f, 0.38f, 0.323f,
                    0.3f, 1.15f, 1f, true),
                CreateAttack(
                    DemonSwordsmanAttackKind.BeastCombo,
                    "맨손 빠른 3연타",
                    "Base Layer.Beast.BeastComboStart",
                    DemonSwordsmanPhaseMask.PhaseTwo,
                    DemonSwordsmanStyle.Beast,
                    0f, 2.35f, 60f, 2f, 1.8f,
                    0.07f, 0.836f, 1.185f, 1.29f,
                    0.45f, 0.85f, 1.1f, false),
                CreateAttack(
                    DemonSwordsmanAttackKind.BeastSlam,
                    "맨손 내려찍기",
                    "Base Layer.Beast.BeastSlam",
                    DemonSwordsmanPhaseMask.PhaseTwo,
                    DemonSwordsmanStyle.Beast,
                    0.8f, 3.2f, 50f, 1.4f, 3.8f,
                    0.09f, 1.282f, 0.564f, 0.59f,
                    0.36f, 1f, 1.04f, false),
                CreateAttack(
                    DemonSwordsmanAttackKind.BeastRush,
                    "맨손 돌진",
                    "Base Layer.Beast.BeastRush",
                    DemonSwordsmanPhaseMask.PhaseTwo,
                    DemonSwordsmanStyle.Beast,
                    1.8f, 5.6f, 38f, 1.25f, 4.2f,
                    0.08f, 0.871f, 0.871f, 0.692f,
                    0.4f, 1.35f, 1.08f, false),
                CreateAttack(
                    DemonSwordsmanAttackKind.BeastWideAttack,
                    "맨손 넓은 공격",
                    "Base Layer.Beast.BeastWideAttack",
                    DemonSwordsmanPhaseMask.PhaseTwo,
                    DemonSwordsmanStyle.Beast,
                    0.5f, 3.8f, 75f, 1.1f, 4.8f,
                    0.1f, 1.763f, 1.122f, 0.921f,
                    0.32f, 0.95f, 1.04f, false)
            };
        }

        private static DemonSwordsmanAttackPattern CreateAttack(
            DemonSwordsmanAttackKind kind,
            string displayName,
            string animatorStateName,
            DemonSwordsmanPhaseMask phaseMask,
            DemonSwordsmanStyle style,
            float minDistance,
            float maxDistance,
            float maxAngle,
            float weight,
            float cooldown,
            float fadeTime,
            float warning,
            float active,
            float recovery,
            float stopTurningNormalized,
            float rootMoveMultiplier,
            float animationSpeed,
            bool isJump)
        {
            var attack = new DemonSwordsmanAttackPattern();
            attack.Configure(
                kind,
                displayName,
                animatorStateName,
                phaseMask,
                style,
                minDistance,
                maxDistance,
                maxAngle,
                weight,
                cooldown,
                fadeTime,
                warning,
                active,
                recovery,
                stopTurningNormalized,
                rootMoveMultiplier,
                animationSpeed,
                isJump);
            return attack;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            findRange = Mathf.Max(1f, findRange);
            preferredDistance = Mathf.Max(0.1f, preferredDistance);
            tooCloseDistance = Mathf.Clamp(
                tooCloseDistance,
                0.1f,
                preferredDistance);
            phaseTwoMoveMultiplier = Mathf.Max(1f, phaseTwoMoveMultiplier);
            phaseTwoTurnMultiplier = Mathf.Max(1f, phaseTwoTurnMultiplier);
        }
#endif
    }
}
