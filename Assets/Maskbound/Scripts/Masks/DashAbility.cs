using System.Collections;
using UnityEngine;

namespace Maskbound.Core
{
    public class DashAbility : MonoBehaviour
    {
        private ThirdPersonController controller;
        private Animator animator;
        private CharacterController characterController;
        
        private bool isDashing;
        private Vector3 dashDirection;
        private float dashSpeed;
        private float dashTimer;
        private float dashDuration;
        private MaskData currentMask;

        private GameObject trailEffect;

        private void Awake()
        {
            controller = GetComponent<ThirdPersonController>();
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
        }

        public void PerformDash(Vector3 direction, float distance, float duration, MaskData mask)
        {
            if (isDashing) return;

            StartCoroutine(DashRoutine(direction, distance, duration, mask));
        }

        private IEnumerator DashRoutine(Vector3 direction, float distance, float duration, MaskData mask)
        {
            isDashing = true;
            currentMask = mask;
            dashDirection = direction.normalized;
            dashDuration = duration;
            dashTimer = 0f;
            dashSpeed = distance / duration;

            // Spawn effects
            SpawnDashEffects();

            // Disable gravity during dash
            float originalGravity = -24f; // Should get from controller

            while (dashTimer < dashDuration)
            {
                dashTimer += Time.deltaTime;
                
                // Calculate dash movement
                Vector3 movement = dashDirection * dashSpeed * Time.deltaTime;
                
                // Apply mask-specific modifications
                ApplyMaskModifiers(ref movement);

                characterController.Move(movement);

                yield return null;
            }

            // Apply exit effects based on mask type
            ApplyExitEffects();

            // Cleanup
            CleanupEffects();
            isDashing = false;
        }

        private void SpawnDashEffects()
        {
            // Spawn trail effect if available
            if (currentMask != null && currentMask.trailEffectPrefab != null)
            {
                trailEffect = Instantiate(currentMask.trailEffectPrefab, transform.position, Quaternion.identity);
                trailEffect.transform.SetParent(transform);
            }

            // Spawn particle effect
            if (currentMask != null && currentMask.abilityEffectPrefab != null)
            {
                ParticleSystem effect = Instantiate(currentMask.abilityEffectPrefab, transform.position, Quaternion.LookRotation(dashDirection));
                effect.transform.SetParent(transform);
                Destroy(effect.gameObject, 2f);
            }

            // Play sound
            if (currentMask != null && currentMask.abilitySound != null)
            {
                AudioSource.PlayClipAtPoint(currentMask.abilitySound, transform.position);
            }
        }

        private void ApplyMaskModifiers(ref Vector3 movement)
        {
            if (currentMask == null) return;

            switch (currentMask.maskType)
            {
                case MaskType.Wind:
                    // Add slight upward drift for wind
                    movement.y += currentMask.windUpwardBoost * Time.deltaTime;
                    break;

                case MaskType.Stone:
                    // Check for breakable objects
                    CheckForBreakables();
                    break;

                case MaskType.Flame:
                    // Leave burning trail
                    CreateBurnEffect();
                    break;
            }
        }

        private void ApplyExitEffects()
        {
            if (currentMask == null || controller == null) return;

            switch (currentMask.maskType)
            {
                case MaskType.Wind:
                    // Small upward boost on exit
                    controller.AddImpulse(Vector3.up * currentMask.windUpwardBoost * 10f);
                    break;

                case MaskType.Stone:
                    // Create impact effect
                    CreateStoneImpact();
                    break;

                case MaskType.Flame:
                    // Speed burst forward
                    Vector3 flameBoost = dashDirection * dashSpeed * (currentMask.flameSpeedBoost - 1f);
                    controller.AddImpulse(flameBoost);
                    break;
            }
        }

        private void CheckForBreakables()
        {
            // Raycast forward to detect breakable objects
            RaycastHit hit;
            if (Physics.Raycast(transform.position, dashDirection, out hit, 1f))
            {
                IBreakable breakable = hit.collider.GetComponent<IBreakable>();
                if (breakable != null)
                {
                    breakable.Break();
                }

                // Stun enemies
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Apply stun effect
                    IStunnable stunnable = hit.collider.GetComponent<IStunnable>();
                    if (stunnable != null)
                    {
                        stunnable.Stun(currentMask.stoneStunDuration);
                    }
                }
            }
        }

        private void CreateBurnEffect()
        {
            // Create a temporary burn zone at current position
            GameObject burnZone = new GameObject("BurnZone");
            burnZone.transform.position = transform.position;
            
            BurnEffect burn = burnZone.AddComponent<BurnEffect>();
            burn.Initialize(currentMask.flameBurnDPS, currentMask.flameBurnDuration);
            
            Destroy(burnZone, currentMask.flameBurnDuration);
        }

        private void CreateStoneImpact()
        {
            // Create shockwave effect
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, currentMask.stoneMassIncrease * 5f);
            
            foreach (Collider hit in hitColliders)
            {
                IStunnable stunnable = hit.GetComponent<IStunnable>();
                if (stunnable != null)
                {
                    stunnable.Stun(currentMask.stoneStunDuration);
                }
            }
        }

        private void CleanupEffects()
        {
            if (trailEffect != null)
            {
                Destroy(trailEffect);
            }
        }

        public bool IsDashing() => isDashing;
    }

    public interface IBreakable
    {
        void Break();
    }

    public interface IStunnable
    {
        void Stun(float duration);
    }
}
