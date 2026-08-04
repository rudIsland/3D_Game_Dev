using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    [Serializable]
    public sealed class DemonSwordsmanAttackPattern
    {
        [SerializeField] private DemonSwordsmanAttackKind kind; // Inspector 설정 값
        [SerializeField] private string displayName = string.Empty; // 표시 이름
        [SerializeField] private string animatorStateName = string.Empty; // 애니메이터 상태 이름
        [SerializeField] private DemonSwordsmanPhaseMask phaseMask; // 현재 행동 상태
        [SerializeField] private DemonSwordsmanStyle style; // 현재 자세
        [SerializeField, Min(0f)] private float minimumDistance; // 거리 설정
        [SerializeField, Min(0.1f)] private float maximumDistance = 2f; // 거리 설정
        [SerializeField, Range(1f, 180f)] private float maximumAngle = 45f; // 각도 설정
        [SerializeField, Min(0.01f)] private float selectionWeight = 1f; // 내부에서 사용하는 값
        [SerializeField, Min(0f)] private float cooldown; // 시간 설정
        [SerializeField, Range(0.01f, 0.3f)] private float crossFadeTime = 0.1f; // 시간 설정
        [SerializeField, Min(0.01f)] private float warningTime = 0.32f; // 시간 설정
        [SerializeField, Min(0.01f)] private float activeTime = 0.35f; // 시간 설정
        [SerializeField, Min(0.01f)] private float recoveryTime = 0.5f; // 시간 설정
        [SerializeField, Range(0f, 1f)] private float stopTurningAtNormalizedTime = 0.45f; // 시간 설정
        [SerializeField, Range(0f, 2f)] private float rootMoveMultiplier = 1f; // 이동 정보
        [SerializeField, Range(0.5f, 1.12f)] private float animationSpeed = 1f; // 이동 속도
        [SerializeField] private bool jumpAttack; // 기능 사용 여부
        [SerializeField] private bool hasBranch; // 기능 사용 여부
        [SerializeField, Min(0f)] private float branchTime; // 시간 설정
        [SerializeField] private DemonSwordsmanAttackKind closeBranchKind; // Inspector 설정 값
        [SerializeField] private DemonSwordsmanAttackKind farBranchKind; // Inspector 설정 값

        [NonSerialized] private int animatorStateHash; // 애니메이터 참조

        public DemonSwordsmanAttackKind Kind => kind; // 외부에 제공하는 읽기 값
        public string DisplayName => displayName; // 표시 이름
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

        public DemonSwordsmanPhaseMask PhaseMask => phaseMask; // 현재 행동 상태
        public DemonSwordsmanStyle Style => style; // 현재 자세
        public float MinimumDistance => minimumDistance; // 거리 설정
        public float MaximumDistance => maximumDistance; // 거리 설정
        public float MaximumAngle => maximumAngle; // 각도 설정
        public float SelectionWeight => selectionWeight; // 외부에 제공하는 읽기 값
        public float Cooldown => cooldown; // 시간 설정
        public float CrossFadeTime => crossFadeTime; // 시간 설정
        public float WarningTime => warningTime; // 시간 설정
        public float ActiveTime => activeTime; // 시간 설정
        public float RecoveryTime => recoveryTime; // 시간 설정
        public float TotalTime => warningTime + activeTime + recoveryTime; // 시간 설정
        public float StopTurningAtNormalizedTime => stopTurningAtNormalizedTime; // 시간 설정
        public float RootMoveMultiplier => rootMoveMultiplier; // 이동 정보
        public float AnimationSpeed => animationSpeed; // 이동 속도
        public bool IsJumpAttack => jumpAttack; // 기능 사용 여부
        public bool HasBranch => // 기능 사용 여부
            hasBranch ||
            kind == DemonSwordsmanAttackKind.SwordCombo ||
            kind == DemonSwordsmanAttackKind.BeastCombo;
        public float BranchTime => branchTime > 0f // 시간 설정
            ? branchTime
            : warningTime + activeTime * 0.55f;
        public DemonSwordsmanAttackKind CloseBranchKind => // 외부에 제공하는 읽기 값
            kind == DemonSwordsmanAttackKind.SwordCombo
                ? DemonSwordsmanAttackKind.QuickSlash
                : kind == DemonSwordsmanAttackKind.BeastCombo
                    ? DemonSwordsmanAttackKind.BeastWideAttack
                    : closeBranchKind;
        public DemonSwordsmanAttackKind FarBranchKind => // 외부에 제공하는 읽기 값
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
        [SerializeField, Min(1f)] private float maxHealth = 1000f; // 최대 체력
        [SerializeField, Min(1f)] private float findRange = 25f; // 거리 설정

        [Header("이동")]
        [SerializeField, Min(0.1f)] private float phaseOneMoveSpeed = 3.4f; // 이동 속도
        [SerializeField, Min(1f)] private float phaseOneTurnSpeed = 360f; // 이동 속도
        [SerializeField, Min(0.1f)] private float preferredDistance = 2.5f; // 거리 설정
        [SerializeField, Min(0.1f)] private float tooCloseDistance = 1.25f; // 거리 설정
        [SerializeField, Min(0.1f)] private float circleSpeed = 2.1f; // 이동 속도
        [SerializeField, Min(0.1f)] private float backAwaySpeed = 2.5f; // 이동 속도
        [SerializeField] private float gravity = -22f; // Inspector 설정 값
        [SerializeField] private float groundPull = -2f; // Inspector 설정 값
        [SerializeField, Range(1f, 1.5f)] private float phaseTwoMoveMultiplier = 1.12f; // 이동 정보
        [SerializeField, Range(1f, 1.5f)] private float phaseTwoTurnMultiplier = 1.2f; // 현재 행동 상태

        [Header("상태 시간")]
        [SerializeField, Min(0.01f)] private float noticeTime = 0.7f; // 시간 설정
        [SerializeField, Min(0.01f)] private float circleTime = 0.65f; // 시간 설정
        [SerializeField, Min(0.01f)] private float backAwayTime = 0.45f; // 시간 설정
        [SerializeField, Min(0.01f)] private float phaseFearTime = 0.85f; // 시간 설정
        [SerializeField, Min(0.01f)] private float phaseRageTime = 1.8f; // 시간 설정
        [SerializeField, Min(0.6f)] private float phaseRepositionTime = 0.6f; // 시간 설정
        [SerializeField, Range(0f, 1f)] private float phaseSwordStoreNormalizedTime = 0.45f; // 시간 설정
        [SerializeField, Min(0.01f)] private float styleChangeTime = 0.9f; // 시간 설정

        [Header("공격")]
        [SerializeField] private DemonSwordsmanAttackPattern[] attacks = // 공격 관련 설정 또는 상태
            Array.Empty<DemonSwordsmanAttackPattern>();

        public float MaxHealth => maxHealth; // 최대 체력
        public float FindRange => findRange; // 거리 설정
        public float PhaseOneMoveSpeed => phaseOneMoveSpeed; // 이동 속도
        public float PhaseOneTurnSpeed => phaseOneTurnSpeed; // 이동 속도
        public float PreferredDistance => preferredDistance; // 거리 설정
        public float TooCloseDistance => tooCloseDistance; // 거리 설정
        public float CircleSpeed => circleSpeed; // 이동 속도
        public float BackAwaySpeed => backAwaySpeed; // 이동 속도
        public float Gravity => gravity; // 외부에 제공하는 읽기 값
        public float GroundPull => groundPull; // 외부에 제공하는 읽기 값
        public float PhaseTwoMoveMultiplier => phaseTwoMoveMultiplier; // 이동 정보
        public float PhaseTwoTurnMultiplier => phaseTwoTurnMultiplier; // 현재 행동 상태
        public float NoticeTime => noticeTime; // 시간 설정
        public float CircleTime => circleTime; // 시간 설정
        public float BackAwayTime => backAwayTime; // 시간 설정
        public float PhaseFearTime => phaseFearTime; // 시간 설정
        public float PhaseRageTime => phaseRageTime; // 시간 설정
        public float PhaseRepositionTime => phaseRepositionTime; // 시간 설정
        public float PhaseSwordStoreNormalizedTime => phaseSwordStoreNormalizedTime; // 시간 설정
        public float StyleChangeTime => styleChangeTime; // 시간 설정
        public DemonSwordsmanAttackPattern[] Attacks => attacks; // 공격 관련 설정 또는 상태

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
