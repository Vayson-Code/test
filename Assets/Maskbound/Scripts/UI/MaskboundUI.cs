using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Maskbound.UI
{
    public class MaskboundUI : MonoBehaviour
    {
        [Header("Mask Display")]
        [SerializeField] private Image currentMaskIcon;
        [SerializeField] private TextMeshProUGUI maskNameText;
        [SerializeField] private TextMeshProUGUI maskDescriptionText;

        [Header("Ability Cooldown")]
        [SerializeField] private Image abilityCooldownFill;
        [SerializeField] private Image abilityIcon;
        [SerializeField] private TextMeshProUGUI cooldownText;

        [Header("Combo Display")]
        [SerializeField] private TextMeshProUGUI comboCountText;
        [SerializeField] private Animator comboAnimator;

        [Header("Mask Switcher")]
        [SerializeField] private Transform maskSwitcherContainer;
        [SerializeField] private GameObject maskSlotPrefab;

        [Header("References")]
        [SerializeField] private Maskbound.Core.MaskManager maskManager;
        [SerializeField] private Maskbound.Core.CombatSystem combatSystem;

        private void Start()
        {
            if (maskManager != null)
            {
                maskManager.OnMaskChanged += UpdateMaskDisplay;
                maskManager.OnAbilityUsed += StartCooldownDisplay;
                
                UpdateMaskDisplay(maskManager.CurrentMask);
                PopulateMaskSwitcher();
            }

            if (combatSystem != null)
            {
                combatSystem.OnComboIncreased += UpdateComboDisplay;
                combatSystem.OnComboReset += ResetComboDisplay;
            }
        }

        private void Update()
        {
            UpdateCooldownDisplay();
        }

        private void UpdateMaskDisplay(Maskbound.Core.MaskData mask)
        {
            if (mask == null) return;

            if (currentMaskIcon != null)
            {
                currentMaskIcon.sprite = mask.maskIcon;
                currentMaskIcon.color = mask.maskColor;
            }

            if (maskNameText != null)
            {
                maskNameText.text = mask.maskName;
            }

            if (maskDescriptionText != null)
            {
                maskDescriptionText.text = mask.GetAbilityDescription();
            }

            if (abilityIcon != null)
            {
                abilityIcon.sprite = mask.maskIcon;
            }
        }

        private void UpdateCooldownDisplay()
        {
            if (maskManager == null || abilityCooldownFill == null) return;

            float remaining = maskManager.AbilityCooldownRemaining;
            bool isReady = maskManager.IsAbilityReady;

            if (isReady)
            {
                abilityCooldownFill.fillAmount = 1f;
                if (cooldownText != null)
                {
                    cooldownText.text = "READY";
                    cooldownText.color = Color.green;
                }
            }
            else
            {
                float cooldown = maskManager.CurrentMask != null ? maskManager.CurrentMask.abilityCooldown : 2.6f;
                abilityCooldownFill.fillAmount = 1f - (remaining / cooldown);
                
                if (cooldownText != null)
                {
                    cooldownText.text = remaining.ToString("F1") + "s";
                    cooldownText.color = Color.white;
                }
            }
        }

        private void StartCooldownDisplay(float cooldownDuration)
        {
            if (abilityCooldownFill != null)
            {
                abilityCooldownFill.fillAmount = 0f;
            }
        }

        private void UpdateComboDisplay(int comboCount)
        {
            if (comboCountText != null)
            {
                comboCountText.text = $"x{comboCount}";
                comboCountText.gameObject.SetActive(true);

                if (comboAnimator != null)
                {
                    comboAnimator.SetTrigger("ComboHit");
                }
            }
        }

        private void ResetComboDisplay()
        {
            if (comboCountText != null)
            {
                comboCountText.gameObject.SetActive(false);
            }
        }

        private void PopulateMaskSwitcher()
        {
            if (maskSwitcherContainer == null || maskSlotPrefab == null) return;

            // Clear existing slots
            foreach (Transform child in maskSwitcherContainer)
            {
                Destroy(child.gameObject);
            }

            // Create slots for each available mask
            var masks = maskManager.GetAvailableMasks();
            for (int i = 0; i < masks.Count; i++)
            {
                GameObject slot = Instantiate(maskSlotPrefab, maskSwitcherContainer);
                MaskSlotUI slotUI = slot.GetComponent<MaskSlotUI>();
                
                if (slotUI != null)
                {
                    slotUI.SetMask(masks[i], i == maskManager.GetCurrentMaskIndex());
                }
            }
        }

        private void OnDestroy()
        {
            if (maskManager != null)
            {
                maskManager.OnMaskChanged -= UpdateMaskDisplay;
                maskManager.OnAbilityUsed -= StartCooldownDisplay;
            }

            if (combatSystem != null)
            {
                combatSystem.OnComboIncreased -= UpdateComboDisplay;
                combatSystem.OnComboReset -= ResetComboDisplay;
            }
        }
    }

    public class MaskSlotUI : MonoBehaviour
    {
        [SerializeField] private Image maskIcon;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject selectedIndicator;

        public void SetMask(Maskbound.Core.MaskData mask, bool isSelected)
        {
            if (maskIcon != null)
            {
                maskIcon.sprite = mask.maskIcon;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = mask.maskColor * 0.5f;
            }

            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(isSelected);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(selected);
            }
        }
    }
}
