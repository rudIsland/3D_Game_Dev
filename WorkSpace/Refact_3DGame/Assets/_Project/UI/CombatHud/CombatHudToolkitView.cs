using UnityEngine;
using UnityEngine.UIElements;
using Core;

namespace UI
{
    // 한 개의 UIDocument에서 전투 HUD 전체를 찾아 표시한다.
    public sealed class CombatHudToolkitView : MonoBehaviour
    {
        private const float PlayerHealthBaseWidthPercent = 100f;
        private const float PlayerStaminaBaseWidthPercent = 75f;

        [SerializeField] private UIDocument hudDocument;

        private VisualElement playerHealthRoot;
        private VisualElement playerHealthBar;
        private VisualElement playerHealthFill;
        private Label playerHealthText;
        private Label playerNameText;

        private VisualElement playerStaminaRoot;
        private VisualElement playerStaminaFill;

        private VisualElement playerInventoryRoot;
        private VisualElement inventorySlot1Icon;
        private VisualElement inventorySlot2Icon;

        private VisualElement enemyHealthRoot;
        private VisualElement enemyHealthFill;
        private Label enemyHealthText;
        private Label enemyNameText;

        private VisualElement enemyStaggerRoot;
        private VisualElement enemyStaggerFill;
        private Label enemyStaggerText;

        private VisualElement interactionRoot;
        private VisualElement interactionKey;
        private Label interactionText;
        private VisualElement cachedDocumentRoot;

        private int displayedCurrentStagger = int.MinValue;
        private int displayedMaxStagger = int.MinValue;
        private bool elementsCached;
        private bool missingElementLogged;

        /// <summary>UIDocument의 필수 표시 요소가 연결되었는지 확인한다. 활성화 후 호출한다.</summary>
        public bool IsReady => EnsureElements();

        public void HideAll()
        {
            if (!EnsureElements())
            {
                return;
            }

            SetVisible(playerHealthRoot, false);
            SetVisible(playerStaminaRoot, false);
            SetVisible(playerInventoryRoot, false);
            SetVisible(enemyHealthRoot, false);
            SetVisible(enemyStaggerRoot, false);
            SetVisible(interactionRoot, false);
        }

        public void ShowPlayerHealth(
            string targetName,
            float currentHealth,
            float maxHealth,
            float maximumScale)
        {
            if (!EnsureElements())
            {
                return;
            }

            playerNameText.text = targetName;
            SetBarWidth(
                playerHealthBar,
                PlayerHealthBaseWidthPercent,
                maximumScale);
            UpdateHealth(
                currentHealth,
                maxHealth,
                playerHealthFill,
                playerHealthText);
            SetVisible(playerHealthRoot, true);
        }

        public void UpdatePlayerHealth(
            float currentHealth,
            float maxHealth,
            float maximumScale)
        {
            if (!EnsureElements())
            {
                return;
            }

            SetBarWidth(
                playerHealthBar,
                PlayerHealthBaseWidthPercent,
                maximumScale);
            UpdateHealth(
                currentHealth,
                maxHealth,
                playerHealthFill,
                playerHealthText);
        }

        public void HidePlayerHealth()
        {
            if (EnsureElements())
            {
                SetVisible(playerHealthRoot, false);
            }
        }

        public void ShowPlayerStamina(
            float currentStamina,
            float maxStamina,
            float maximumScale)
        {
            if (!EnsureElements())
            {
                return;
            }

            UpdatePlayerStamina(
                currentStamina,
                maxStamina,
                maximumScale);
            SetVisible(playerStaminaRoot, true);
        }

        public void UpdatePlayerStamina(
            float currentStamina,
            float maxStamina,
            float maximumScale)
        {
            if (!EnsureElements())
            {
                return;
            }

            SetBarWidth(
                playerStaminaRoot,
                PlayerStaminaBaseWidthPercent,
                maximumScale);
            SetFillWidth(playerStaminaFill, currentStamina, maxStamina);
        }

        public void HidePlayerStamina()
        {
            if (EnsureElements())
            {
                SetVisible(playerStaminaRoot, false);
            }
        }

        public void ShowPlayerInventory(ItemDefinition firstItem, ItemDefinition secondItem)
        {
            if (!EnsureElements())
            {
                return;
            }

            UpdatePlayerInventory(firstItem, secondItem);
            SetVisible(playerInventoryRoot, true);
        }

        public void UpdatePlayerInventory(ItemDefinition firstItem, ItemDefinition secondItem)
        {
            if (!EnsureElements())
            {
                return;
            }

            SetInventorySlot(
                inventorySlot1Icon,
                firstItem);
            SetInventorySlot(
                inventorySlot2Icon,
                secondItem);
        }

        public void HidePlayerInventory()
        {
            if (EnsureElements())
            {
                SetVisible(playerInventoryRoot, false);
            }
        }

        public void ShowEnemyHealth(string targetName, UnitHealth health)
        {
            if (health == null || !EnsureElements())
            {
                return;
            }

            enemyNameText.text = targetName;
            UpdateHealth(
                health,
                enemyHealthFill,
                enemyHealthText);
            SetVisible(enemyHealthRoot, true);
        }

        public void UpdateEnemyHealth(UnitHealth health)
        {
            if (health == null || !EnsureElements())
            {
                return;
            }

            UpdateHealth(
                health,
                enemyHealthFill,
                enemyHealthText);
        }

        public void HideEnemyHealth()
        {
            if (EnsureElements())
            {
                SetVisible(enemyHealthRoot, false);
            }
        }

        public void ShowEnemyStagger(float currentStagger, float maxStagger)
        {
            if (!EnsureElements())
            {
                return;
            }

            UpdateEnemyStagger(currentStagger, maxStagger);
            SetVisible(enemyStaggerRoot, true);
        }

        public void UpdateEnemyStagger(float currentStagger, float maxStagger)
        {
            if (!EnsureElements())
            {
                return;
            }

            SetFillWidth(enemyStaggerFill, currentStagger, maxStagger);

            int currentValue = Mathf.CeilToInt(currentStagger);
            int maxValue = Mathf.CeilToInt(maxStagger);
            if (displayedCurrentStagger == currentValue &&
                displayedMaxStagger == maxValue)
            {
                return;
            }

            displayedCurrentStagger = currentValue;
            displayedMaxStagger = maxValue;
            enemyStaggerText.text = string.Format(
                "STAGGER {0} / {1}",
                currentValue,
                maxValue);
        }

        public void HideEnemyStagger()
        {
            if (EnsureElements())
            {
                SetVisible(enemyStaggerRoot, false);
            }
        }

        public void ShowInteractionGuide(PlayerInteractionGuide guide)
        {
            if (EnsureElements())
            {
                interactionText.text = guide.Message;
                SetVisible(interactionKey, guide.CanInteract);
                SetVisible(interactionRoot, true);
            }
        }

        public void HideInteractionGuide()
        {
            if (EnsureElements())
            {
                SetVisible(interactionRoot, false);
            }
        }

        private bool EnsureElements()
        {
            if (hudDocument == null)
            {
                hudDocument = GetComponent<UIDocument>();
            }

            VisualElement root = hudDocument != null
                ? hudDocument.rootVisualElement
                : null;
            if (root == null)
            {
                return false;
            }

            if (elementsCached && ReferenceEquals(cachedDocumentRoot, root))
            {
                return true;
            }

            cachedDocumentRoot = root;
            SetPickingIgnored(root);

            playerHealthRoot = root.Q<VisualElement>("player-health");
            playerHealthBar = root.Q<VisualElement>("player-health-bar");
            playerHealthFill = root.Q<VisualElement>("player-health-fill");
            playerHealthText = root.Q<Label>("player-health-text");
            playerNameText = root.Q<Label>("player-name");
            playerStaminaRoot = root.Q<VisualElement>("player-stamina");
            playerStaminaFill = root.Q<VisualElement>("player-stamina-fill");
            playerInventoryRoot = root.Q<VisualElement>("player-inventory");
            inventorySlot1Icon = root.Q<VisualElement>("inventory-slot-1-icon");
            inventorySlot2Icon = root.Q<VisualElement>("inventory-slot-2-icon");
            enemyHealthRoot = root.Q<VisualElement>("enemy-health");
            enemyHealthFill = root.Q<VisualElement>("enemy-health-fill");
            enemyHealthText = root.Q<Label>("enemy-health-text");
            enemyNameText = root.Q<Label>("enemy-name");
            enemyStaggerRoot = root.Q<VisualElement>("enemy-stagger");
            enemyStaggerFill = root.Q<VisualElement>("enemy-stagger-fill");
            enemyStaggerText = root.Q<Label>("enemy-stagger-text");
            interactionRoot = root.Q<VisualElement>("interaction-guide");
            interactionKey = root.Q<VisualElement>("interaction-key");
            interactionText = root.Q<Label>("interaction-text");

            elementsCached =
                playerHealthRoot != null &&
                playerHealthBar != null &&
                playerHealthFill != null &&
                playerHealthText != null &&
                playerNameText != null &&
                playerStaminaRoot != null &&
                playerStaminaFill != null &&
                playerInventoryRoot != null &&
                inventorySlot1Icon != null &&
                inventorySlot2Icon != null &&
                enemyHealthRoot != null &&
                enemyHealthFill != null &&
                enemyHealthText != null &&
                enemyNameText != null &&
                enemyStaggerRoot != null &&
                enemyStaggerFill != null &&
                enemyStaggerText != null &&
                interactionRoot != null &&
                interactionKey != null &&
                interactionText != null;

            if (!elementsCached && !missingElementLogged)
            {
                missingElementLogged = true;
                Debug.LogError(
                    "CombatHudToolkitView could not find every required UXML element.",
                    this);
            }

            return elementsCached;
        }

        private static void UpdateHealth(
            UnitHealth health,
            VisualElement fill,
            Label valueText)
        {
            UpdateHealth(health.CurrentHealth, health.MaxHealth, fill, valueText);
        }

        // 표시 수치만 받아 플레이어와 적 체력바에 같은 계산을 사용한다.
        private static void UpdateHealth(
            float currentHealth,
            float maxHealth,
            VisualElement fill,
            Label valueText)
        {
            SetFillWidth(fill, currentHealth, maxHealth);
            valueText.text = string.Format(
                "{0:0} / {1:0}",
                currentHealth,
                maxHealth);
        }

        private static void SetFillWidth(
            VisualElement fill,
            float currentValue,
            float maximumValue)
        {
            float ratio = maximumValue > 0f
                ? Mathf.Clamp01(currentValue / maximumValue)
                : 0f;
            fill.style.scale = new Scale(new Vector3(ratio, 1f, 1f));
        }

        private static void SetBarWidth(
            VisualElement bar,
            float baseWidthPercent,
            float maximumScale)
        {
            if (maximumScale <= 0f ||
                float.IsNaN(maximumScale) ||
                float.IsInfinity(maximumScale))
            {
                maximumScale = 1f;
            }

            bar.style.width = new Length(
                baseWidthPercent * maximumScale,
                LengthUnit.Percent);
        }

        private static void SetVisible(VisualElement element, bool isVisible)
        {
            element.style.display = isVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private static void SetInventorySlot(
            VisualElement iconElement,
            ItemDefinition item)
        {
            bool hasIcon = item != null && item.Icon != null;
            if (hasIcon)
            {
                iconElement.style.backgroundImage =
                    new StyleBackground(item.Icon);
                iconElement.tooltip = item.DisplayName;
            }
            else
            {
                iconElement.style.backgroundImage = StyleKeyword.None;
                iconElement.tooltip = string.Empty;
            }

            SetVisible(iconElement, hasIcon);
        }

        private static void SetPickingIgnored(VisualElement element)
        {
            element.pickingMode = PickingMode.Ignore;

            VisualElement.Hierarchy hierarchy = element.hierarchy;
            for (int index = 0; index < hierarchy.childCount; index++)
            {
                SetPickingIgnored(hierarchy[index]);
            }
        }

#if UNITY_EDITOR
        public void ConnectForEditor(UIDocument document)
        {
            hudDocument = document;
            elementsCached = false;
            cachedDocumentRoot = null;
        }
#endif
    }
}
