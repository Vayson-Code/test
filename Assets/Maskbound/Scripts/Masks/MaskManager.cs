using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Maskbound.Core
{
    public class MaskManager : MonoBehaviour
    {
        [Header("Mask Settings")]
        [SerializeField] private List<MaskData> availableMasks = new List<MaskData>();
        [SerializeField] private int startingMaskIndex = 0;
        [SerializeField] private float abilityCooldown = 2.6f;
        [SerializeField] private bool showMaskSwitchUI = true;

        [Header("Effects")]
        [SerializeField] private ParticleSystem switchEffectPrefab;
        [SerializeField] private AudioClip switchSound;

        // Components
        private ThirdPersonController controller;
        private Animator animator;
        private AudioSource audioSource;

        // State
        private MaskData currentMask;
        private int currentMaskIndex;
        private float lastAbilityUseTime;
        private bool isAbilityOnCooldown;

        // Animation hashes
        private int animIDAbility;
        private int animIDMaskType;

        // Events
        public event Action<MaskData> OnMaskChanged;
        public event Action<float> OnAbilityUsed;

        public MaskData CurrentMask => currentMask;
        public float AbilityCooldownRemaining => Mathf.Max(0f, abilityCooldown - (Time.time - lastAbilityUseTime));
        public bool IsAbilityReady => !isAbilityOnCooldown;

        private void Awake()
        {
            controller = GetComponent<ThirdPersonController>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            AssignAnimationIDs();
        }

        private void Start()
        {
            if (availableMasks.Count > 0)
            {
                currentMaskIndex = Mathf.Clamp(startingMaskIndex, 0, availableMasks.Count - 1);
                EquipMask(availableMasks[currentMaskIndex], false);
            }
        }

        private void Update()
        {
            UpdateAbilityCooldown();
        }

        private void AssignAnimationIDs()
        {
            animIDAbility = Animator.StringToHash("UseAbility");
            animIDMaskType = Animator.StringToHash("MaskType");
        }

        private void UpdateAbilityCooldown()
        {
            if (isAbilityOnCooldown)
            {
                if (Time.time - lastAbilityUseTime >= abilityCooldown)
                {
                    isAbilityOnCooldown = false;
                }
            }
        }

        public void CycleToNextMask()
        {
            if (availableMasks.Count <= 1) return;

            currentMaskIndex = (currentMaskIndex + 1) % availableMasks.Count;
            EquipMask(availableMasks[currentMaskIndex], true);
        }

        public void CycleToPreviousMask()
        {
            if (availableMasks.Count <= 1) return;

            currentMaskIndex--;
            if (currentMaskIndex < 0) currentMaskIndex = availableMasks.Count - 1;
            
            EquipMask(availableMasks[currentMaskIndex], true);
        }

        public void EquipMaskByIndex(int index)
        {
            if (index < 0 || index >= availableMasks.Count) return;

            currentMaskIndex = index;
            EquipMask(availableMasks[index], true);
        }

        private void EquipMask(MaskData mask, bool playEffects)
        {
            if (mask == null) return;

            currentMask = mask;

            // Update animator
            if (animator != null)
            {
                animator.SetInteger(animIDMaskType, (int)mask.maskType);
            }

            // Play effects
            if (playEffects)
            {
                PlaySwitchEffects();
            }

            // Notify listeners
            OnMaskChanged?.Invoke(currentMask);

            Debug.Log($"Equipped mask: {mask.maskName}");
        }

        public void UseCurrentMaskAbility()
        {
            if (currentMask == null || isAbilityOnCooldown) return;

            StartCoroutine(UseAbilityRoutine());
        }

        private IEnumerator UseAbilityRoutine()
        {
            isAbilityOnCooldown = true;
            lastAbilityUseTime = Time.time;

            // Trigger animation
            animator?.SetTrigger(animIDAbility);

            // Execute mask-specific ability
            currentMask.ExecuteAbility(gameObject, controller);

            // Notify cooldown started
            OnAbilityUsed?.Invoke(abilityCooldown);

            yield return null;
        }

        private void PlaySwitchEffects()
        {
            // Spawn particle effect
            if (switchEffectPrefab != null)
            {
                ParticleSystem effect = Instantiate(switchEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect.gameObject, 2f);
            }

            // Play sound
            if (switchSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(switchSound);
            }
        }

        public void AddMask(MaskData mask)
        {
            if (!availableMasks.Contains(mask))
            {
                availableMasks.Add(mask);
            }
        }

        public void RemoveMask(MaskData mask)
        {
            if (availableMasks.Contains(mask))
            {
                availableMasks.Remove(mask);
                
                // If we removed the current mask, switch to another
                if (currentMask == mask && availableMasks.Count > 0)
                {
                    currentMaskIndex = 0;
                    EquipMask(availableMasks[0], true);
                }
            }
        }

        #region Public API
        public List<MaskData> GetAvailableMasks() => new List<MaskData>(availableMasks);
        public int GetCurrentMaskIndex() => currentMaskIndex;
        #endregion
    }
}
