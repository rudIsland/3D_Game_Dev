using Characters;
using Characters.Enemies;
using UnityEngine;
using World;

namespace GameUI.CombatHud
{
    // 적의 머리 위치를 따라가며 플레이어 카메라를 향하는 체력바다.
    [DisallowMultipleComponent]
    // Cinemachine 등 카메라의 LateUpdate가 끝난 뒤 방향을 맞춘다.
    [DefaultExecutionOrder(10000)]
    [RequireComponent(typeof(Canvas))]
    public sealed class EnemyHeadHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyView enemy;
        [SerializeField] private Transform head;
        [SerializeField] private RectTransform healthFill;
        [SerializeField, Min(0f)] private float headGap = 0.3f;

        private Canvas healthCanvas;
        private Transform barTransform;
        private Camera playerCamera;
        private Transform cameraTransform;
        private UnitHealth health;
        private IEnemyCombatStatus combatStatus;
        private float nextCameraSearchTime;

        private void Awake()
        {
            healthCanvas = GetComponent<Canvas>();
            barTransform = transform;
            healthCanvas.enabled = false;
            if (enemy == null || head == null || healthFill == null)
            {
                Debug.LogError("EnemyHeadHealthBar에 적, 머리 위치, 체력 채움 영역을 연결하세요.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            nextCameraSearchTime = 0f;
            ConnectHealth();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HealthChanged -= UpdateHealth;
                health = null;
            }
            combatStatus = null;
            if (healthCanvas != null)
            {
                healthCanvas.enabled = false;
            }
        }

        private void LateUpdate()
        {
            // 풀의 최초 Instantiate 때는 RuntimeObject가 아직 준비되지 않았을 수 있다.
            if (health == null)
            {
                ConnectHealth();
            }

            if (playerCamera == null || !playerCamera.isActiveAndEnabled)
            {
                healthCanvas.enabled = false;
                if (Time.unscaledTime < nextCameraSearchTime)
                {
                    return;
                }
                nextCameraSearchTime = Time.unscaledTime + 1f;
                playerCamera = Camera.main;
                cameraTransform = playerCamera != null ? playerCamera.transform : null;
            }

            if (health == null || health.IsDead || combatStatus == null ||
                !combatStatus.IsInCombat || head == null ||
                playerCamera == null || !playerCamera.isActiveAndEnabled)
            {
                healthCanvas.enabled = false;
                return;
            }

            // 화면과 평행한 방향을 유지하여 어느 각도에서도 체력바를 읽을 수 있게 한다.
            barTransform.SetPositionAndRotation(
                head.position + Vector3.up * headGap,
                cameraTransform.rotation);
            healthCanvas.enabled = true;
        }

        private void ConnectHealth()
        {
            if (health != null || enemy == null || healthFill == null ||
                !(enemy.RuntimeObject is Unit unit) ||
                !(enemy.RuntimeObject is IEnemyCombatStatus status))
            {
                return;
            }
            health = unit.Health;
            combatStatus = status;
            health.HealthChanged += UpdateHealth;
            UpdateHealth(health);
        }

        private void UpdateHealth(UnitHealth changedHealth)
        {
            float ratio = changedHealth.MaxHealth > 0f
                ? Mathf.Clamp01(changedHealth.CurrentHealth / changedHealth.MaxHealth) : 0f;
            Vector2 maximum = healthFill.anchorMax;
            maximum.x = ratio;
            healthFill.anchorMax = maximum;
            if (changedHealth.IsDead)
            {
                healthCanvas.enabled = false;
            }
        }
    }
}
