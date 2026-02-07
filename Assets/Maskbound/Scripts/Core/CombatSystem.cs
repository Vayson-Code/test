using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Maskbound.Core
{
    public class CombatSystem : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private float comboResetTime = 1.5f;
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private int maxLightCombo = 4;

        [Header("Attack Detection")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float lightAttackRange = 1.5f;
        [SerializeField] private float heavyAttackRange = 2.0f;
        [SerializeField] private LayerMask enemyLayers;

        [Header("Damage Settings")]
        [SerializeField] private float[] lightComboDamage = { 10f, 12f, 15f, 20f };
        [SerializeField] private float heavyAttackDamage = 35f;

        [Header("Attack Properties")]
        [SerializeField] private float lightAttackDuration = 0.6f;
        [SerializeField] private float heavyAttackDuration = 1.2f;
        [SerializeField] private float comboWindowStart = 0.3f; // When next input can be buffered
        
        [Header("Hit Effects")]
        [SerializeField] private float hitStopDuration = 0.06f;
        [SerializeField] private float heavyHitStopDuration = 0.12f;

        // Components
        [SerializeField] private Animator animator;
        [SerializeField] private ThirdPersonController controller;

        // Combat state
        private int currentComboIndex = 0;
        private float lastAttackTime;
        private float comboResetTimer;
        private bool canAttack = true;
        private bool isAttacking = false;
        private bool hasQueuedAttack = false;
        private bool isHeavyQueued = false;
        private float currentAttackEndTime;

        // Animation hashes
        private int animIDLightAttack1;
        private int animIDLightAttack2;
        private int animIDLightAttack3;
        private int animIDLightAttack4;
        private int animIDHeavyAttack;
        private int animIDInCombat;
        private int animIDAttackSpeed;

        // Events
        public event Action<int> OnComboIncreased;
        public event Action OnComboReset;
        public event Action<GameObject, float> OnHitEnemy;

        public bool InCombat { get; private set; }
        public bool IsInAction => isAttacking;
        public int CurrentCombo => currentComboIndex;

        private void Awake()
        {
            AssignAnimationIDs();
        }

        private void Update()
        {
            UpdateComboTimer();
            UpdateCombatState();
            ProcessQueuedAttacks();
        }

        private void AssignAnimationIDs()
        {
            // Light combo attacks
            animIDLightAttack1 = Animator.StringToHash("LightAttack1");
            animIDLightAttack2 = Animator.StringToHash("LightAttack2");
            animIDLightAttack3 = Animator.StringToHash("LightAttack3");
            animIDLightAttack4 = Animator.StringToHash("LightAttack4");
            
            animIDHeavyAttack = Animator.StringToHash("HeavyAttack");
            animIDInCombat = Animator.StringToHash("InCombat");
            animIDAttackSpeed = Animator.StringToHash("AttackSpeed");
            
            // Set combat layer weight (layer 1) to blend with movement
            if (animator != null && animator.layerCount > 1)
            {
                animator.SetLayerWeight(1, 1f); // Combat layer at full weight
            }
        }

        private void UpdateComboTimer()
        {
            if (currentComboIndex > 0)
            {
                comboResetTimer += Time.deltaTime;
                
                if (comboResetTimer >= comboResetTime)
                {
                    ResetCombo();
                }
            }
        }

        private void UpdateCombatState()
        {
            // Exit combat after period of inactivity
            if (InCombat && Time.time - lastAttackTime > 3f)
            {
                ExitCombat();
            }
        }

        private void ProcessQueuedAttacks()
        {
            if (hasQueuedAttack && Time.time >= currentAttackEndTime)
            {
                hasQueuedAttack = false;
                
                if (isHeavyQueued)
                {
                    ExecuteHeavyAttack();
                }
                else
                {
                    ExecuteLightAttack();
                }
            }
        }

        public void PerformAttack()
        {
            if (isAttacking)
            {
                // Buffer the attack if we're in combo window
                if (Time.time >= lastAttackTime + comboWindowStart && !hasQueuedAttack)
                {
                    hasQueuedAttack = true;
                    isHeavyQueued = false;
                }
                return;
            }

            if (!canAttack) return;

            ExecuteLightAttack();
        }

        public void PerformHeavyAttack()
        {
            if (isAttacking)
            {
                // Buffer heavy attack
                if (Time.time >= lastAttackTime + comboWindowStart && !hasQueuedAttack)
                {
                    hasQueuedAttack = true;
                    isHeavyQueued = true;
                }
                return;
            }

            if (!canAttack) return;

            ExecuteHeavyAttack();
        }

        private void ExecuteLightAttack()
        {
            StartCoroutine(AttackRoutine(false));
        }

        private void ExecuteHeavyAttack()
        {
            StartCoroutine(AttackRoutine(true));
        }

        private IEnumerator AttackRoutine(bool isHeavy)
        {
            canAttack = false;
            isAttacking = true;
            lastAttackTime = Time.time;
            comboResetTimer = 0f;

            EnterCombat();

            // Trigger appropriate animation
            if (isHeavy)
            {
                animator.SetTrigger(animIDHeavyAttack);
                currentAttackEndTime = Time.time + heavyAttackDuration;
                
                // Reset combo after heavy attack
                yield return new WaitForSeconds(heavyAttackDuration);
                ResetCombo();
            }
            else
            {
                // Trigger specific light attack based on combo index
                TriggerLightComboAnimation(currentComboIndex);
                currentAttackEndTime = Time.time + lightAttackDuration;
                
                // Increment combo
                currentComboIndex++;
                if (currentComboIndex >= maxLightCombo)
                {
                    currentComboIndex = 0;
                }
                OnComboIncreased?.Invoke(currentComboIndex);
                
                yield return new WaitForSeconds(lightAttackDuration);
            }

            canAttack = true;
            isAttacking = false;
        }

        private void TriggerLightComboAnimation(int comboStep)
        {
            switch (comboStep)
            {
                case 0:
                    animator.SetTrigger(animIDLightAttack1);
                    break;
                case 1:
                    animator.SetTrigger(animIDLightAttack2);
                    break;
                case 2:
                    animator.SetTrigger(animIDLightAttack3);
                    break;
                case 3:
                    animator.SetTrigger(animIDLightAttack4);
                    break;
            }
        }

        // Called by animation event
        public void OnAttackHit()
        {
            DetectHits(false);
        }

        // Called by heavy attack animation event
        public void OnHeavyAttackHit()
        {
            DetectHits(true);
        }

        private void DetectHits(bool isHeavy)
        {
            if (attackPoint == null) return;

            float range = isHeavy ? heavyAttackRange : lightAttackRange;
            Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, range, enemyLayers);

            foreach (Collider enemy in hitEnemies)
            {
                float damage = isHeavy ? heavyAttackDamage : 
                               lightComboDamage[Mathf.Min(currentComboIndex - 1, lightComboDamage.Length - 1)];
                
                // Apply damage
                IDamageable damageable = enemy.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    OnHitEnemy?.Invoke(enemy.gameObject, damage);
                    
                    // Apply hit effects
                    ApplyHitEffects(isHeavy);
                }
            }
        }

        private void ApplyHitEffects(bool isHeavy)
        {
            float stopDuration = isHeavy ? heavyHitStopDuration : hitStopDuration;
            StartCoroutine(HitStopRoutine(stopDuration));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }

        private void ResetCombo()
        {
            currentComboIndex = 0;
            comboResetTimer = 0f;
            hasQueuedAttack = false;
            OnComboReset?.Invoke();
        }

        private void EnterCombat()
        {
            if (!InCombat)
            {
                InCombat = true;
                animator.SetBool(animIDInCombat, true);
            }
        }

        private void ExitCombat()
        {
            if (InCombat)
            {
                InCombat = false;
                animator.SetBool(animIDInCombat, false);
                ResetCombo();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, lightAttackRange);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(attackPoint.position, heavyAttackRange);
            }
        }

        #region Public API
        public void SetCanAttack(bool value) => canAttack = value;
        public void ForceResetCombo() => ResetCombo();
        public void CancelAttack()
        {
            StopAllCoroutines();
            isAttacking = false;
            canAttack = true;
            hasQueuedAttack = false;
        }
        #endregion
    }

    public interface IDamageable
    {
        void TakeDamage(float damage);
        void Die();
    }
    
}