using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    // Inspector와 WorldObject 생명주기를 일반 C# 보스 로직에 연결한다.
    public sealed class DemonSwordsmanController :
        WorldObjectView,
        IDemonSwordsmanTarget,
        IDemonSwordsmanCombatOutput
    {
        [Header("필수 연결")]
        [SerializeField] private Transform target;
        [SerializeField] private DemonSwordsmanBossSettings settings;
        [SerializeField] private DemonSwordsmanAnimationController bossAnimation;

#if UNITY_EDITOR
        [Header("Health Test")]
        [SerializeField, Min(0f)] private float testDamage = 10f;
#endif

        private CharacterController characterController;
        private DemonSwordsmanMovement movement;
        private DemonSwordsmanWorldUnit bossWorldUnit;

        public DemonSwordsmanPhase CurrentPhase =>
            bossWorldUnit?.CurrentPhase ??
            DemonSwordsmanPhase.PhaseOne;
        public string CurrentStateName =>
            bossWorldUnit?.CurrentStateName ?? "Not Ready";
        public string CurrentAttackName =>
            bossWorldUnit?.CurrentAttackName ?? "없음";
        public float HealthRatio =>
            bossWorldUnit?.HealthRatio ?? 0f;
        public bool IsPhaseChanging =>
            bossWorldUnit?.IsPhaseChanging ?? false;

        bool IDemonSwordsmanTarget.HasTarget => target != null;
        Vector3 IDemonSwordsmanTarget.Position =>
            target != null ? target.position : transform.position;

        protected override IWorldObject CreateRuntimeObject()
        {
            FindSceneReferences();

            if (settings == null ||
                bossAnimation == null)
            {
                throw new System.InvalidOperationException(
                    "DemonSwordsmanController에 Settings와 Animation 연결이 필요합니다.");
            }

            characterController = GetComponent<CharacterController>();
            movement = new DemonSwordsmanMovement(
                transform,
                characterController,
                settings.Gravity,
                settings.GroundPull);

            var stateMachine = new DemonSwordsmanStateMachine(
                settings,
                this,
                movement,
                bossAnimation,
                this);
            bossWorldUnit = new DemonSwordsmanWorldUnit(
                settings.MaxHealth,
                stateMachine);
            return bossWorldUnit;
        }

        public void SetTarget(Transform nextTarget)
        {
            target = nextTarget;
        }

        public void OpenBranchWindow()
        {
            bossWorldUnit?.OpenBranchWindow();
        }

        public void SwapWeapon()
        {
            bossWorldUnit?.SwapWeapon();
        }

        public void FinishAction()
        {
            bossWorldUnit?.FinishAction();
        }

        public void TakeDamage(float damage)
        {
            bossWorldUnit?.TakeDamage(damage);
        }

        void IDemonSwordsmanCombatOutput.SwapWeapon()
        {
            bossAnimation?.ShowStyle(
                bossWorldUnit?.CurrentStyle ??
                DemonSwordsmanStyle.Sword);
        }

        internal void ApplyAttackAnimationMove(
            Vector3 animationMove)
        {
            movement?.ApplyAttackAnimationMove(animationMove);
        }

        internal void Configure(
            WorldObjectManager manager,
            Transform targetTransform,
            DemonSwordsmanBossSettings bossSettings,
            DemonSwordsmanAnimationController animationController)
        {
            target = targetTransform;
            settings = bossSettings;
            bossAnimation = animationController;
        }

        private void FindSceneReferences()
        {
            if (bossAnimation == null)
            {
                bossAnimation =
                    GetComponentInChildren<DemonSwordsmanAnimationController>(
                        true);
            }

            if (target != null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Damage")]
        private void TestDamage()
        {
            if (!Application.isPlaying || bossWorldUnit == null)
            {
                Debug.LogWarning(
                    "Test Damage requires Play Mode and an initialized boss.",
                    this);
                return;
            }

            float healthBeforeDamage = bossWorldUnit.Health.CurrentHealth;
            bossWorldUnit.TakeDamage(testDamage);

            Debug.Log(
                $"Boss health: {healthBeforeDamage} -> {bossWorldUnit.Health.CurrentHealth}",
                this);
        }

        private void OnValidate()
        {
            if (bossAnimation == null)
            {
                bossAnimation =
                    GetComponentInChildren<DemonSwordsmanAnimationController>(
                        true);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (settings == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, settings.FindRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                transform.position,
                settings.TooCloseDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(
                transform.position,
                settings.PreferredDistance);
        }
#endif
    }
}
