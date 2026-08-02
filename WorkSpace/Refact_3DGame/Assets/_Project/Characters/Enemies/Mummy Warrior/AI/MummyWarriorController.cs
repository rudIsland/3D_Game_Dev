using System;
using rudIsland.RPG3D.Combat;
using rudIsland.RPG3D.Player;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.MummyWarrior
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(CharacterController),
        typeof(MummyWarriorAnimationController))]
    // Unity 입력과 일반 C# Mummy Warrior 전투 로직을 연결한다.
    public sealed class MummyWarriorController : WorldObjectView, IAttackHitReceiver
    {
        [Header("필수 연결")]
        [SerializeField] private Transform target;
        [SerializeField] private Animator mummyAnimator;
        [SerializeField] private MeleeHitDetector lanceHitDetector;

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 120f;
        [SerializeField, Min(0f)] private float deadBodyKeepTime = 2f;

        [Header("탐지와 이동")]
        [SerializeField, Min(0.1f)] private float findRange = 25f;
        [SerializeField, Min(0.1f)] private float runStartRange = 6f;
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.6f;
        [SerializeField, Min(0.1f)] private float runSpeed = 3.8f;
        [SerializeField, Min(1f)] private float turnSpeed = 300f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundPull = -2f;

        [Header("공격 목록")]
        [SerializeField] private MummyWarriorAttackPattern[] attackPatterns =
            { new MummyWarriorAttackPattern() };

#if UNITY_EDITOR
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f;
#endif

        private CharacterController characterController;
        private MummyWarriorAnimationController animationController;
        private MummyWarriorWorldUnit mummyWorldUnit;
        private MummyWarriorWorldUnit standaloneWorldUnit;

        private void Awake()
        {
            FindSceneReferences();
            FindUnityComponents();
        }

        // 풀을 거치지 않고 CharacterTestScene에 직접 놓인 객체도 같은 로직으로 실행한다.
        private void Start()
        {
            if (RuntimeObject != null) return;

            standaloneWorldUnit = (MummyWarriorWorldUnit)CreateRuntimeObject();
            standaloneWorldUnit.Create();
            standaloneWorldUnit.Enable();
        }

        private void Update()
        {
            standaloneWorldUnit?.Tick(Time.deltaTime);
        }

        private void OnEnable()
        {
            if (standaloneWorldUnit != null && !standaloneWorldUnit.IsEnabled)
            {
                standaloneWorldUnit.Enable();
            }
        }

        private void OnDisable()
        {
            standaloneWorldUnit?.Disable();
            EndAttackHit();
        }

        private void OnDestroy()
        {
            standaloneWorldUnit?.Dispose();
        }

        protected override IWorldObject CreateRuntimeObject()
        {
            FindSceneReferences();
            FindUnityComponents();

            if (target == null || mummyAnimator == null ||
                characterController == null || lanceHitDetector == null)
            {
                throw new InvalidOperationException(
                    "MummyWarriorController에 Target, Animator, CharacterController, Lance Hit Detector가 필요합니다.");
            }

            animationController.ConnectAnimator(mummyAnimator);
            var movement = new MummyWarriorMovement(
                transform,
                characterController,
                gravity,
                groundPull);
            var stateMachine = new MummyWarriorStateMachine(
                target,
                movement,
                animationController,
                attackPatterns,
                findRange,
                runStartRange,
                walkSpeed,
                runSpeed,
                turnSpeed,
                deadBodyKeepTime,
                StartAttackHit,
                EndAttackHit,
                RequestDeadMummyRelease);

            mummyWorldUnit = new MummyWarriorWorldUnit(maxHealth, stateMachine);
            return mummyWorldUnit;
        }

        public void ReceiveHit(in AttackHitData hit)
        {
            mummyWorldUnit?.ApplyHit(in hit);
        }

        public void PlayBlock() => animationController?.PlayBlock();
        public void PlayTurn() => animationController?.PlayTurn();
        public void PlayStepBack() => animationController?.PlayStepBack();
        public void PlayExit() => animationController?.PlayExit();

        private void StartAttackHit(
            MummyWarriorAttackPattern pattern,
            int attackNumber)
        {
            if (pattern == null || !pattern.Damage.IsValid) return;

            var hit = new AttackHitData(
                pattern.Damage,
                UnitTeam.Enemy,
                attackNumber);
            lanceHitDetector.StartHit(in hit);
        }

        private void EndAttackHit()
        {
            lanceHitDetector?.EndHit();
        }

        private void RequestDeadMummyRelease()
        {
            if (standaloneWorldUnit != null)
            {
                gameObject.SetActive(false);
                return;
            }

            RequestDespawn();
        }

        private void FindSceneReferences()
        {
            if (target != null) return;
            PlayerController player = FindFirstObjectByType<PlayerController>();
            target = player != null ? player.transform : null;
        }

        private void FindUnityComponents()
        {
            characterController = GetComponent<CharacterController>();
            animationController = GetComponent<MummyWarriorAnimationController>();
            if (mummyAnimator == null)
            {
                mummyAnimator = GetComponentInChildren<Animator>(true);
            }

            if (lanceHitDetector == null)
            {
                lanceHitDetector = GetComponentInChildren<MeleeHitDetector>(true);
            }
        }

        protected override void OnResetForPool()
        {
            EndAttackHit();
            animationController?.ResetAnimation();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Damage")]
        private void TestDamage()
        {
            if (!Application.isPlaying || mummyWorldUnit == null)
            {
                Debug.LogWarning("Test Damage는 Play 중이고 Mummy 준비가 끝난 뒤 사용할 수 있습니다.", this);
                return;
            }

            mummyWorldUnit.TakeDamage(testDamage);
        }

        private void OnValidate()
        {
            FindUnityComponents();
            findRange = Mathf.Max(0.1f, findRange);
            runStartRange = Mathf.Clamp(runStartRange, 0.1f, findRange);
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            runSpeed = Mathf.Max(walkSpeed, runSpeed);
            deadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);

            if (attackPatterns == null) return;
            for (int index = 0; index < attackPatterns.Length; index++)
            {
                attackPatterns[index]?.ClampValues();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, findRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, runStartRange);
        }
#endif
    }
}
