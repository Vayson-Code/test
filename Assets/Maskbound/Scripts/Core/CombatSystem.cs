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

        [Header("Attack Detection (Box)")]
        [SerializeField] private Transform attackPointBlade; 
        [SerializeField] private Transform attackPointHandle;
        
        // Since your sword is along the Y axis:
        // X = Width, Y = Length (Height), Z = Thickness
        [SerializeField] private Vector3 lightAttackBox = new Vector3(0.5f, 1.5f, 0.5f);
        [SerializeField] private Vector3 heavyAttackBox = new Vector3(0.7f, 2.0f, 0.7f);
        [SerializeField] private Vector3 handleAttackBox = new Vector3(0.4f, 0.8f, 0.4f);
        [SerializeField] private LayerMask enemyLayers;
        
        private Transform currentAttackPoint;

        [Header("Damage Settings")]
        [SerializeField] private float[] lightComboDamage = { 10f, 12f, 15f, 20f };
        [SerializeField] private float heavyAttackDamage = 35f;

        [Header("Attack Properties")]
        [SerializeField] private float lightAttackDuration = 0.6f;
        [SerializeField] private float heavyAttackDuration = 1.2f;
        [SerializeField] private float comboWindowStart = 0.3f;
        [SerializeField] private float attackRecoveryTime = 0.2f;
        
        [Header("Hit Effects")]
        [SerializeField] private float hitStopDuration = 0.06f;
        [SerializeField] private float heavyHitStopDuration = 0.12f;

        [Header("Components")]
        [SerializeField] private Animator animator;

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

        // Events
        public event Action<int> OnComboIncreased;
        public event Action OnComboReset;
        public event Action<GameObject, float> OnHitEnemy;

        // Required by your ThirdPersonController
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
            animIDLightAttack1 = Animator.StringToHash("LightAttack1");
            animIDLightAttack2 = Animator.StringToHash("LightAttack2");
            animIDLightAttack3 = Animator.StringToHash("LightAttack3");
            animIDLightAttack4 = Animator.StringToHash("LightAttack4");
            animIDHeavyAttack = Animator.StringToHash("HeavyAttack");
            animIDInCombat = Animator.StringToHash("InCombat");
            
            if (animator != null && animator.layerCount > 1)
            {
                animator.SetLayerWeight(1, 1f);
            }
        }

        private void UpdateComboTimer()
        {
            if (currentComboIndex > 0 && !isAttacking)
            {
                comboResetTimer += Time.deltaTime;
                if (comboResetTimer >= comboResetTime) ResetCombo();
            }
        }

        private void UpdateCombatState()
        {
            if (InCombat && Time.time - lastAttackTime > 3f) ExitCombat();
        }

        private void ProcessQueuedAttacks()
        {
            if (hasQueuedAttack && Time.time >= currentAttackEndTime)
            {
                hasQueuedAttack = false;
                if (isHeavyQueued) ExecuteHeavyAttack();
                else ExecuteLightAttack();
            }
        }

        public void PerformAttack()
        {
            if (isAttacking)
            {
                if (Time.time >= lastAttackTime + comboWindowStart && !hasQueuedAttack)
                {
                    hasQueuedAttack = true;
                    isHeavyQueued = false;
                }
                return;
            }
            if (canAttack) ExecuteLightAttack();
        }

        public void PerformHeavyAttack()
        {
            if (isAttacking)
            {
                if (Time.time >= lastAttackTime + comboWindowStart && !hasQueuedAttack)
                {
                    hasQueuedAttack = true;
                    isHeavyQueued = true;
                }
                return;
            }
            if (canAttack) ExecuteHeavyAttack();
        }

        private void ExecuteLightAttack() => StartCoroutine(AttackRoutine(false));
        private void ExecuteHeavyAttack() => StartCoroutine(AttackRoutine(true));

        private IEnumerator AttackRoutine(bool isHeavy)
        {
            canAttack = false;
            isAttacking = true;
            lastAttackTime = Time.time;
            comboResetTimer = 0f;

            EnterCombat();

            float totalDuration = isHeavy ? heavyAttackDuration : lightAttackDuration;
            
            if (isHeavy)
            {
                animator.SetTrigger(animIDHeavyAttack);
                currentAttackPoint = attackPointBlade;
            }
            else
            {
                TriggerLightComboAnimation(currentComboIndex);
                currentComboIndex = (currentComboIndex + 1) % maxLightCombo;
                OnComboIncreased?.Invoke(currentComboIndex);
            }

            currentAttackEndTime = Time.time + totalDuration;
            yield return new WaitForSeconds(totalDuration);
            yield return new WaitForSeconds(attackRecoveryTime);

            if (isHeavy) ResetCombo();

            canAttack = true;
            isAttacking = false;
        }

        private void TriggerLightComboAnimation(int comboStep)
        {
            switch (comboStep)
            {
                case 0:
                    animator.SetTrigger(animIDLightAttack1);
                    currentAttackPoint = attackPointHandle;
                    break;
                case 1:
                    animator.SetTrigger(animIDLightAttack2);
                    currentAttackPoint = attackPointBlade;
                    break;
                case 2:
                    animator.SetTrigger(animIDLightAttack3);
                    currentAttackPoint = attackPointBlade;
                    break;
                case 3:
                    animator.SetTrigger(animIDLightAttack4);
                    currentAttackPoint = attackPointBlade;
                    break;
            }
        }

        public void OnAttackHit() => DetectHits(false);
        public void OnHeavyAttackHit() => DetectHits(true);

        private void DetectHits(bool isHeavy)
        {
            Transform activePoint = currentAttackPoint != null ? currentAttackPoint : attackPointBlade;
            if (activePoint == null) return;

            Vector3 boxSize;
            if (isHeavy) boxSize = heavyAttackBox;
            else boxSize = (currentComboIndex == 1 && currentAttackPoint == attackPointHandle) ? handleAttackBox : lightAttackBox;

            // Using Physics.OverlapBox with the rotation of the sword point
            Collider[] hitEnemies = Physics.OverlapBox(activePoint.position, boxSize / 2, activePoint.rotation, enemyLayers);

            foreach (Collider enemy in hitEnemies)
            {
                IDamageable damageable = enemy.GetComponent<IDamageable>() ?? enemy.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    float damage = isHeavy ? heavyAttackDamage : lightComboDamage[Mathf.Clamp(currentComboIndex - 1, 0, lightComboDamage.Length - 1)];
                    damageable.TakeDamage(damage);
                    OnHitEnemy?.Invoke(enemy.gameObject, damage);
                    ApplyHitEffects(isHeavy);
                }
            }
        }

        private void ApplyHitEffects(bool isHeavy) => StartCoroutine(HitStopRoutine(isHeavy ? heavyHitStopDuration : hitStopDuration));

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
            if (!InCombat) { InCombat = true; animator.SetBool(animIDInCombat, true); }
        }

        private void ExitCombat()
        {
            if (InCombat) { InCombat = false; animator.SetBool(animIDInCombat, false); ResetCombo(); }
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPointBlade != null)
            {
                Gizmos.matrix = Matrix4x4.TRS(attackPointBlade.position, attackPointBlade.rotation, Vector3.one);
                Gizmos.color = Color.red; Gizmos.DrawWireCube(Vector3.zero, lightAttackBox);
                Gizmos.color = Color.yellow; Gizmos.DrawWireCube(Vector3.zero, heavyAttackBox);
            }
            if (attackPointHandle != null)
            {
                Gizmos.matrix = Matrix4x4.TRS(attackPointHandle.position, attackPointHandle.rotation, Vector3.one);
                Gizmos.color = Color.cyan; Gizmos.DrawWireCube(Vector3.zero, handleAttackBox);
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
    }
}