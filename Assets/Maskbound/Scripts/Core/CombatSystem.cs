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
        [SerializeField] private float attackCooldown = 0.1f;
        [SerializeField] private int maxComboCount = 4;

        [Header("Attack Detection")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private LayerMask enemyLayers;

        [Header("Damage Settings")]
        [SerializeField] private float[] comboDamage = { 10f, 15f, 20f, 30f };
        [SerializeField] private float heavyAttackMultiplier = 1.5f;

        [Header("Hit Effects")]
        [SerializeField] private float hitStopDuration = 0.06f;
        [SerializeField] private float cameraShakeMagnitude = 0.2f;

        // Components
        [SerializeField] private Animator animator;
        [SerializeField] private ThirdPersonController controller;

        // Combat state
        private int currentComboIndex = 0;
        private float lastAttackTime;
        private float comboResetTimer;
        private bool canAttack = true;
        private bool isAttacking = false;

        // Animation hashes
        private int animIDAttack;
        private int animIDComboIndex;
        private int animIDHeavyAttack;
        private int animIDInCombat;

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
        }

        private void AssignAnimationIDs()
        {
            animIDAttack = Animator.StringToHash("Attack");
            animIDComboIndex = Animator.StringToHash("ComboIndex");
            animIDHeavyAttack = Animator.StringToHash("HeavyAttack");
            animIDInCombat = Animator.StringToHash("InCombat");
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

        public void PerformAttack()
        {
            if (!canAttack || isAttacking) return;

            StartCoroutine(AttackRoutine(false));
        }

        public void PerformHeavyAttack()
        {
            if (!canAttack || isAttacking) return;

            StartCoroutine(AttackRoutine(true));
        }

        private IEnumerator AttackRoutine(bool isHeavy)
        {
            canAttack = false;
            isAttacking = true;
            lastAttackTime = Time.time;
            comboResetTimer = 0f;

            EnterCombat();

            // Trigger animation
            if (isHeavy)
            {
                animator.SetTrigger(animIDHeavyAttack);
            }
            else
            {
                animator.SetInteger(animIDComboIndex, currentComboIndex);
                animator.SetTrigger(animIDAttack);
            }

            // Increment combo
            currentComboIndex = Mathf.Min(currentComboIndex + 1, maxComboCount - 1);
            OnComboIncreased?.Invoke(currentComboIndex);

            // Wait for animation event to trigger hit detection
            yield return new WaitForSeconds(attackCooldown);

            canAttack = true;
            isAttacking = false;
        }

        // Called by animation event
        public void OnAttackHit()
        {
            DetectHits();
        }

        private void DetectHits()
        {
            if (attackPoint == null) return;

            Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);

            foreach (Collider enemy in hitEnemies)
            {
                float damage = comboDamage[Mathf.Min(currentComboIndex, comboDamage.Length - 1)];
                
                // Apply damage
                IDamageable damageable = enemy.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    OnHitEnemy?.Invoke(enemy.gameObject, damage);
                    
                    // Apply hit effects
                    ApplyHitEffects();
                }
            }
        }

        private void ApplyHitEffects()
        {
            StartCoroutine(HitStopRoutine());
            CameraShake();
        }

        private IEnumerator HitStopRoutine()
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = 1f;
        }

        private void CameraShake()
        {
            // Implement camera shake if you have a camera shake system
            // Example: CameraShakeManager.Instance?.ShakeCamera(cameraShakeMagnitude, 0.1f);
        }

        private void ResetCombo()
        {
            currentComboIndex = 0;
            comboResetTimer = 0f;
            animator.SetInteger(animIDComboIndex, 0);
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
                Gizmos.DrawWireSphere(attackPoint.position, attackRange);
            }
        }

        #region Public API
        public void SetCanAttack(bool value) => canAttack = value;
        public void ForceResetCombo() => ResetCombo();
        #endregion
    }

    public interface IDamageable
    {
        void TakeDamage(float damage);
        void Die();
    }
}
