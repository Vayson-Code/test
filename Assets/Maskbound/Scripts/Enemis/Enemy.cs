using UnityEngine;
using UnityEngine.AI;

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
        [SerializeField] private float contactDamage = 10f;
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

        // Animation hashes
        private int animIDSpeed;
        private int animIDAttack;
        private int animIDHit;
        private int animIDDeath;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            
            currentHealth = maxHealth;
            
            AssignAnimationIDs();
        }

        private void Start()
        {
            // Find player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
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

            // Check if player in detection range
            if (distanceToPlayer <= detectionRange)
            {
                // Attack if in range
                if (distanceToPlayer <= attackRange)
                {
                    agent.isStopped = true;
                    
                    // Face player
                    Vector3 direction = (player.position - transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation, 
                            Quaternion.LookRotation(direction), 
                            Time.deltaTime * 5f
                        );
                    }

                    // Attack
                    if (Time.time - lastAttackTime >= attackCooldown)
                    {
                        PerformAttack();
                    }
                }
                else
                {
                    // Chase player
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                    agent.SetDestination(player.position);
                }
            }
            else
            {
                // Patrol or idle
                agent.speed = patrolSpeed;
                // Add patrol logic here if desired
            }
        }

        private void UpdateAnimations()
        {
            if (animator == null) return;

            float speed = agent != null ? agent.velocity.magnitude : 0f;
            animator.SetFloat(animIDSpeed, speed);
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
            if (isDead) return;

            currentHealth -= damage;

            // Play hit animation
            if (animator != null && !isStunned)
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

            // Play death animation
            if (animator != null)
            {
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

            // Drop items
            DropLoot();

            // Destroy after delay
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

        private void DropLoot()
        {
            if (dropItems.Length == 0) return;

            for (int i = 0; i < dropCount; i++)
            {
                GameObject item = dropItems[Random.Range(0, dropItems.Length)];
                Vector3 dropPosition = transform.position + Random.insideUnitSphere * 0.5f;
                dropPosition.y = transform.position.y;
                
                Instantiate(item, dropPosition, Quaternion.identity);
            }
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

        private void OnCollisionEnter(Collision collision)
        {
            // Deal contact damage to player
            if (collision.gameObject.CompareTag("Player"))
            {
                IDamageable playerDamageable = collision.gameObject.GetComponent<IDamageable>();
                if (playerDamageable != null)
                {
                    playerDamageable.TakeDamage(contactDamage);
                }
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
