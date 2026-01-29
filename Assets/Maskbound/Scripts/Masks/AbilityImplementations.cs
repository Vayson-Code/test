using System.Collections;
using UnityEngine;

namespace Maskbound.Core
{
    // ==================== GRAPPLE ABILITY ====================
    public class GrappleAbility : MonoBehaviour
    {
        private ThirdPersonController controller;
        private CharacterController characterController;
        private LineRenderer lineRenderer;
        
        private bool isGrappling;
        private Vector3 grapplePoint;
        private float grappleSpeed;

        private void Awake()
        {
            controller = GetComponent<ThirdPersonController>();
            characterController = GetComponent<CharacterController>();
            
            // Setup line renderer for grapple visualization
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.enabled = false;
        }

        public void PerformGrapple(float range, float speed, MaskData mask)
        {
            if (isGrappling) return;

            // Find grapple point
            if (FindGrapplePoint(range, out Vector3 point))
            {
                StartCoroutine(GrappleRoutine(point, speed, mask));
            }
        }

        private bool FindGrapplePoint(float range, out Vector3 point)
        {
            point = Vector3.zero;

            // Try to find tagged grapple points first
            GameObject[] grapplePoints = GameObject.FindGameObjectsWithTag("GrapplePoint");
            GameObject closest = null;
            float closestDist = range;

            foreach (GameObject gp in grapplePoints)
            {
                float dist = Vector3.Distance(transform.position, gp.transform.position);
                if (dist < closestDist)
                {
                    closest = gp;
                    closestDist = dist;
                }
            }

            if (closest != null)
            {
                point = closest.transform.position;
                return true;
            }

            // If no grapple point, try to grapple to enemy
            Collider[] enemies = Physics.OverlapSphere(transform.position, range, LayerMask.GetMask("Enemy"));
            if (enemies.Length > 0)
            {
                point = enemies[0].transform.position;
                return true;
            }

            return false;
        }

        private IEnumerator GrappleRoutine(Vector3 target, float speed, MaskData mask)
        {
            isGrappling = true;
            grapplePoint = target;
            grappleSpeed = speed;

            // Enable visual line
            lineRenderer.enabled = true;
            lineRenderer.material.color = mask.maskColor;

            while (Vector3.Distance(transform.position, grapplePoint) > 0.5f)
            {
                // Move toward grapple point
                Vector3 direction = (grapplePoint - transform.position).normalized;
                Vector3 movement = direction * grappleSpeed * Time.deltaTime;
                
                characterController.Move(movement);

                // Update line renderer
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, grapplePoint);

                yield return null;
            }

            // Apply arrival effects based on mask
            ApplyArrivalEffects(mask);

            lineRenderer.enabled = false;
            isGrappling = false;
        }

        private void ApplyArrivalEffects(MaskData mask)
        {
            if (mask == null) return;

            switch (mask.maskType)
            {
                case MaskType.Wind:
                    // Give a boost for swing momentum
                    if (controller != null)
                    {
                        Vector3 swingBoost = controller.GetMoveDirection() * 5f;
                        controller.AddImpulse(swingBoost + Vector3.up * mask.windUpwardBoost * 15f);
                    }
                    break;

                case MaskType.Stone:
                    // Create impact stun
                    Collider[] hits = Physics.OverlapSphere(transform.position, mask.stoneStunDuration * 2f);
                    foreach (Collider hit in hits)
                    {
                        IStunnable stunnable = hit.GetComponent<IStunnable>();
                        stunnable?.Stun(mask.stoneStunDuration);
                    }
                    break;

                case MaskType.Flame:
                    // Knockback and ignite
                    Collider[] enemies = Physics.OverlapSphere(grapplePoint, 2f, LayerMask.GetMask("Enemy"));
                    foreach (Collider enemy in enemies)
                    {
                        Rigidbody rb = enemy.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            Vector3 knockback = (enemy.transform.position - transform.position).normalized * 10f;
                            rb.AddForce(knockback, ForceMode.Impulse);
                        }

                        // Apply burn
                        BurnEffect burn = enemy.gameObject.AddComponent<BurnEffect>();
                        burn.Initialize(mask.flameBurnDPS, mask.flameBurnDuration);
                    }
                    break;
            }
        }

        public bool IsGrappling() => isGrappling;
    }

    // ==================== SLAM ABILITY ====================
    public class SlamAbility : MonoBehaviour
    {
        private ThirdPersonController controller;
        private CharacterController characterController;
        
        private bool isSlamming;

        private void Awake()
        {
            controller = GetComponent<ThirdPersonController>();
            characterController = GetComponent<CharacterController>();
        }

        public void PerformSlam(float force, float radius, MaskData mask)
        {
            if (isSlamming) return;

            StartCoroutine(SlamRoutine(force, radius, mask));
        }

        private IEnumerator SlamRoutine(float force, float radius, MaskData mask)
        {
            isSlamming = true;

            // Apply downward force
            if (controller != null)
            {
                controller.AddImpulse(Vector3.down * force);
            }

            // Wait for ground impact
            bool grounded = false;
            while (!grounded)
            {
                grounded = Physics.Raycast(transform.position, Vector3.down, 0.2f);
                yield return null;
            }

            // Create impact effect
            CreateSlamImpact(radius, mask);

            isSlamming = false;
        }

        private void CreateSlamImpact(float radius, MaskData mask)
        {
            // Spawn impact visual
            if (mask != null && mask.abilityEffectPrefab != null)
            {
                ParticleSystem impact = Instantiate(mask.abilityEffectPrefab, transform.position, Quaternion.identity);
                Destroy(impact.gameObject, 2f);
            }

            // Apply mask-specific effects
            switch (mask.maskType)
            {
                case MaskType.Wind:
                    // Rebound shockwave and upward bounce
                    Collider[] windHits = Physics.OverlapSphere(transform.position, radius);
                    foreach (Collider hit in windHits)
                    {
                        Rigidbody rb = hit.GetComponent<Rigidbody>();
                        if (rb != null && hit.gameObject != gameObject)
                        {
                            Vector3 pushDirection = (hit.transform.position - transform.position).normalized;
                            rb.AddForce(pushDirection * 10f, ForceMode.Impulse);
                        }
                    }
                    // Give player a bounce
                    controller?.AddImpulse(Vector3.up * 8f);
                    break;

                case MaskType.Stone:
                    // Heavy ground pound - break floor and stun
                    Collider[] stoneHits = Physics.OverlapSphere(transform.position, radius * 1.5f);
                    foreach (Collider hit in stoneHits)
                    {
                        IBreakable breakable = hit.GetComponent<IBreakable>();
                        breakable?.Break();

                        IStunnable stunnable = hit.GetComponent<IStunnable>();
                        stunnable?.Stun(mask.stoneStunDuration);
                    }
                    break;

                case MaskType.Flame:
                    // Explosive slam - upward flame burst
                    Collider[] flameHits = Physics.OverlapSphere(transform.position, radius);
                    foreach (Collider hit in flameHits)
                    {
                        IDamageable damageable = hit.GetComponent<IDamageable>();
                        if (damageable != null && hit.gameObject != gameObject)
                        {
                            BurnEffect burn = hit.gameObject.AddComponent<BurnEffect>();
                            burn.Initialize(mask.flameBurnDPS, mask.flameBurnDuration);
                        }
                    }
                    // Vertical boost for player
                    controller?.AddImpulse(Vector3.up * 12f);
                    break;
            }

            // Camera shake and hitstop
            Time.timeScale = 0.1f;
            StartCoroutine(ResetTimeScale());
        }

        private IEnumerator ResetTimeScale()
        {
            yield return new WaitForSecondsRealtime(0.05f);
            Time.timeScale = 1f;
        }

        public bool IsSlamming() => isSlamming;
    }

    // ==================== BLINK ABILITY ====================
    public class BlinkAbility : MonoBehaviour
    {
        private ThirdPersonController controller;
        
        private bool isBlinking;

        private void Awake()
        {
            controller = GetComponent<ThirdPersonController>();
        }

        public void PerformBlink(float distance, MaskData mask)
        {
            if (isBlinking) return;

            StartCoroutine(BlinkRoutine(distance, mask));
        }

        private IEnumerator BlinkRoutine(float distance, MaskData mask)
        {
            isBlinking = true;

            // Calculate blink direction
            Vector3 blinkDirection = controller.GetMoveDirection();
            if (blinkDirection.magnitude < 0.1f)
            {
                blinkDirection = transform.forward;
            }

            // Calculate target position
            Vector3 targetPosition = transform.position + blinkDirection.normalized * distance;

            // Check if path is clear (for stone phasing)
            bool canPhase = mask.maskType == MaskType.Stone;
            if (!canPhase)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, blinkDirection, out hit, distance))
                {
                    targetPosition = hit.point - blinkDirection.normalized * 0.5f;
                }
            }

            // Disable collisions briefly
            Collider playerCollider = GetComponent<Collider>();
            if (playerCollider != null)
            {
                playerCollider.enabled = false;
            }

            // Instant teleport
            transform.position = targetPosition;

            // Apply arrival effects
            ApplyBlinkArrivalEffects(mask);

            yield return new WaitForSeconds(0.12f);

            // Re-enable collisions
            if (playerCollider != null)
            {
                playerCollider.enabled = true;
            }

            isBlinking = false;
        }

        private void ApplyBlinkArrivalEffects(MaskData mask)
        {
            if (mask == null) return;

            // Spawn arrival effect
            if (mask.abilityEffectPrefab != null)
            {
                ParticleSystem effect = Instantiate(mask.abilityEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect.gameObject, 2f);
            }

            switch (mask.maskType)
            {
                case MaskType.Wind:
                    // Small glide on exit
                    if (controller != null)
                    {
                        Vector3 glideForce = controller.GetMoveDirection() * 3f + Vector3.up * mask.windUpwardBoost * 5f;
                        controller.AddImpulse(glideForce);
                    }
                    break;

                case MaskType.Stone:
                    // Ground thud effect
                    Collider[] hits = Physics.OverlapSphere(transform.position, 2f);
                    foreach (Collider hit in hits)
                    {
                        IStunnable stunnable = hit.GetComponent<IStunnable>();
                        stunnable?.Stun(mask.stoneStunDuration * 0.5f);
                    }
                    break;

                case MaskType.Flame:
                    // Explosive arrival - push enemies back
                    Collider[] enemies = Physics.OverlapSphere(transform.position, 3f);
                    foreach (Collider enemy in enemies)
                    {
                        if (enemy.gameObject == gameObject) continue;

                        Rigidbody rb = enemy.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            Vector3 pushDirection = (enemy.transform.position - transform.position).normalized;
                            rb.AddForce(pushDirection * 15f, ForceMode.Impulse);
                        }

                        // Light ground on fire
                        BurnEffect burn = enemy.gameObject.AddComponent<BurnEffect>();
                        burn?.Initialize(mask.flameBurnDPS * 0.5f, mask.flameBurnDuration);
                    }
                    break;
            }
        }

        public bool IsBlinking() => isBlinking;
    }

    // ==================== BURN EFFECT ====================
    public class BurnEffect : MonoBehaviour
    {
        private float damagePerSecond;
        private float duration;
        private float timer;

        public void Initialize(float dps, float dur)
        {
            damagePerSecond = dps;
            duration = dur;
            timer = 0f;
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer >= duration)
            {
                Destroy(this);
                return;
            }

            // Apply damage
            IDamageable damageable = GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }
    }
}
