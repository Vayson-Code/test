using Maskbound.Scripts.Enemis;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Maskbound.Core
{
    /// <summary>
    /// Example enemy implementation with health, damage, and AI
    /// Implements IDamageable for combat system compatibility
    /// </summary>
    public class Enemy : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool showHealthBar = true;

        [Header("Damage")]
        [SerializeField] private float attackDamage = 20f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 2f;

        [Header("Movement")]
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float detectionRange = 10f;

        [Header("Drops")]
        [SerializeField] private GameObject[] dropItems;
        [SerializeField] private int dropCount = 1;

        // Components
        private NavMeshAgent agent;
        private Animator animator;
        private Transform player;

        // State
        private float currentHealth;
        private float lastAttackTime;
        private bool isDead;
        private bool isStunned;
        private float stunEndTime;
        private float currentSpeed; // Dynamic speed for smooth acceleration
        private const float SpeedSmoothTime = 0.25f; // Smoothing factor for acceleration

        // Animation hashes
        private int animIDSpeed;
        private int animIDAttack;
        private int animIDHit;
        private int animIDDeath;
        private int animIDIsDead;

        public UnityEvent<float> OnHealthChanged = new UnityEvent<float>();

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            currentHealth = maxHealth;
            AssignAnimationIDs();
            currentSpeed = 0f;
        }

        private void Start()
        {
            // Find player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            EnemyManager.Instance.RegisterEnemy(gameObject);
        }

        private void Update()
        {
            if (isDead) return;

            UpdateStunState();
            UpdateAI();
            UpdateAnimations();
        }

        private void AssignAnimationIDs()
        {
            animIDSpeed = Animator.StringToHash("Speed");
            animIDAttack = Animator.StringToHash("Attack");
            animIDHit = Animator.StringToHash("Hit");
            animIDDeath = Animator.StringToHash("Death");
            animIDIsDead = Animator.StringToHash("IsDead"); // New bool parameter
        }

        private void UpdateStunState()
        {
            if (isStunned && Time.time >= stunEndTime)
            {
                isStunned = false;
                if (agent != null)
                {
                    agent.isStopped = false;
                }
            }
        }

        private void UpdateAI()
        {
            if (player == null || agent == null || isStunned) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                if (distanceToPlayer <= attackRange)
                {
                    agent.isStopped = true;
                    agent.speed = 0f;
                    // Smoothly lerp animation speed to 0 for blend tree
                    currentSpeed = Mathf.Lerp(currentSpeed, 0f, 1 - Mathf.Exp(-Time.deltaTime / SpeedSmoothTime));

                    // Face player only when stopped (attack)
                    Vector3 direction = (player.position - transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation, 
                            Quaternion.LookRotation(direction), 
                            Time.deltaTime * 15f
                        );
                    }

                    if (Time.time - lastAttackTime >= attackCooldown)
                    {
                        PerformAttack();
                    }
                }
                else
                {
                    agent.isStopped = false;
                    // Exponential acceleration toward chaseSpeed
                    currentSpeed = Mathf.Lerp(currentSpeed, chaseSpeed, 1 - Mathf.Exp(-Time.deltaTime / SpeedSmoothTime));
                    agent.speed = currentSpeed;
                    agent.SetDestination(player.position);
                }
            }
            else
            {
                // Patrol or idle
                currentSpeed = Mathf.Lerp(currentSpeed, patrolSpeed, 1 - Mathf.Exp(-Time.deltaTime / SpeedSmoothTime));
                agent.speed = currentSpeed;
                // Add patrol logic here if desired
            }
        }

        private void UpdateAnimations()
        {
            if (animator == null) return;
            animator.SetFloat(animIDSpeed, currentSpeed);
        }

        private void PerformAttack()
        {
            lastAttackTime = Time.time;
            
            if (animator != null)
            {
                animator.SetTrigger(animIDAttack);
            }

            // Damage player (call via animation event for better timing)
            DamagePlayer();
        }

        // Called by animation event
        public void OnAttackHit()
        {
            DamagePlayer();
        }

        private void DamagePlayer()
        {
            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange)
            {
                // Try to damage player
                IDamageable playerDamageable = player.GetComponent<IDamageable>();
                if (playerDamageable != null)
                {
                    playerDamageable.TakeDamage(attackDamage);
                }
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return; // Prevent taking damage if already dead

            currentHealth -= damage;

            // Fire health changed event
            OnHealthChanged.Invoke(currentHealth / maxHealth);

            // Play hit animation only if not dead after damage
            if (!isDead && animator != null && !isStunned)
            {
                animator.SetTrigger(animIDHit);
            }

            // Visual feedback
            StartCoroutine(FlashRed());

            // Check for death
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            if (isDead) return;

            isDead = true;

            // Fire health changed event (0 health)
            OnHealthChanged.Invoke(0f);

            // Set IsDead bool in animator
            if (animator != null)
            {
                animator.SetBool(animIDIsDead, true);
                animator.SetTrigger(animIDDeath);
            }

            // Disable AI
            if (agent != null)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // Disable collision
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            // Destroy after delay
            EnemyManager.Instance.UnregisterEnemy(gameObject);
            Destroy(gameObject, 3f);
        }

        public void Stun(float duration)
        {
            if (isDead) return;

            isStunned = true;
            stunEndTime = Time.time + duration;

            if (agent != null)
            {
                agent.isStopped = true;
            }

            // Visual effect for stun
            StartCoroutine(StunEffect(duration));
        }

       

        private System.Collections.IEnumerator FlashRed()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            Color originalColor = Color.white;
            
            if (renderers.Length > 0 && renderers[0].material.HasProperty("_Color"))
            {
                originalColor = renderers[0].material.color;
            }

            // Flash red
            foreach (Renderer r in renderers)
            {
                if (r.material.HasProperty("_Color"))
                {
                    r.material.color = Color.red;
                }
            }

            yield return new WaitForSeconds(0.1f);

            // Return to original
            foreach (Renderer r in renderers)
            {
                if (r.material.HasProperty("_Color"))
                {
                    r.material.color = originalColor;
                }
            }
        }

        private System.Collections.IEnumerator StunEffect(float duration)
        {
            // Add visual stun effect (stars, particles, etc.)
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                // Rotate model to indicate stun
                transform.Rotate(Vector3.up, 360f * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        

        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        #region Public API
        public float GetHealthPercentage() => currentHealth / maxHealth;
        public bool IsDead() => isDead;
        public bool IsStunned() => isStunned;
        #endregion
    }
}
