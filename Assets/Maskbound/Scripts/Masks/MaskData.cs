using UnityEngine;

namespace Maskbound.Core
{
    public enum MaskType
    {
        Wind = 0,
        Stone = 1,
        Flame = 2
    }

    public enum AbilityType
    {
        Dash,
        Grapple,
        Slam,
        Blink
    }

    [CreateAssetMenu(fileName = "New Mask", menuName = "Maskbound/Mask Data")]
    public class MaskData : ScriptableObject
    {
        [Header("Identity")]
        public string maskName = "New Mask";
        public MaskType maskType;
        public Sprite maskIcon;
        
        [TextArea(3, 5)]
        public string description;

        [Header("Ability")]
        public AbilityType abilityType;
        public float abilityCooldown = 2.6f;

        [Header("Movement Modifiers")]
        [Range(0f, 2f)] public float speedMultiplier = 1f;
        [Range(0f, 2f)] public float jumpHeightMultiplier = 1f;
        [Range(0f, 2f)] public float airControlMultiplier = 1f;

        [Header("Ability Parameters")]
        public float dashDistance = 7f;
        public float dashDuration = 0.18f;
        public float grappleRange = 12f;
        public float grappleSpeed = 10f;
        public float slamForce = 20f;
        public float slamRadius = 1.5f;
        public float blinkDistance = 5f;

        [Header("Effects")]
        public Color maskColor = Color.white;
        public ParticleSystem abilityEffectPrefab;
        public AudioClip abilitySound;
        public GameObject trailEffectPrefab;

        [Header("Mask-Specific Modifiers")]
        // Wind
        [Tooltip("Wind: Additional upward impulse on ability exit")]
        public float windUpwardBoost = 0.15f;
        
        // Stone
        [Tooltip("Stone: Mass increase percentage")]
        [Range(0f, 1f)] public float stoneMassIncrease = 0.25f;
        [Tooltip("Stone: Stun duration on impact")]
        public float stoneStunDuration = 0.5f;
        
        // Flame
        [Tooltip("Flame: Speed boost multiplier on exit")]
        public float flameSpeedBoost = 1.2f;
        [Tooltip("Flame: Burn damage per second")]
        public float flameBurnDPS = 6f;
        [Tooltip("Flame: Burn duration")]
        public float flameBurnDuration = 1.5f;

        /// <summary>
        /// Execute the mask's ability
        /// </summary>
        public void ExecuteAbility(GameObject player, ThirdPersonController controller)
        {
            switch (abilityType)
            {
                case AbilityType.Dash:
                    ExecuteDash(player, controller);
                    break;
                case AbilityType.Grapple:
                    ExecuteGrapple(player, controller);
                    break;
                case AbilityType.Slam:
                    ExecuteSlam(player, controller);
                    break;
                case AbilityType.Blink:
                    ExecuteBlink(player, controller);
                    break;
            }
        }

        private void ExecuteDash(GameObject player, ThirdPersonController controller)
        {
            Vector3 dashDirection = controller.GetMoveDirection();
            if (dashDirection.magnitude < 0.1f)
            {
                dashDirection = player.transform.forward;
            }

            float distance = dashDistance;
            float duration = dashDuration;

            // Apply mask-specific modifiers
            switch (maskType)
            {
                case MaskType.Wind:
                    // Longer dash with more control
                    distance *= 1.3f;
                    break;
                case MaskType.Stone:
                    // Shorter, heavier dash
                    distance *= 0.8f;
                    break;
                case MaskType.Flame:
                    // Speed boost
                    distance *= flameSpeedBoost;
                    break;
            }

            DashAbility dashAbility = player.GetComponent<DashAbility>();
            if (dashAbility == null)
            {
                dashAbility = player.AddComponent<DashAbility>();
            }
            dashAbility.PerformDash(dashDirection, distance, duration, this);
        }

        private void ExecuteGrapple(GameObject player, ThirdPersonController controller)
        {
            GrappleAbility grappleAbility = player.GetComponent<GrappleAbility>();
            if (grappleAbility == null)
            {
                grappleAbility = player.AddComponent<GrappleAbility>();
            }
            grappleAbility.PerformGrapple(grappleRange, grappleSpeed, this);
        }

        private void ExecuteSlam(GameObject player, ThirdPersonController controller)
        {
            SlamAbility slamAbility = player.GetComponent<SlamAbility>();
            if (slamAbility == null)
            {
                slamAbility = player.AddComponent<SlamAbility>();
            }
            slamAbility.PerformSlam(slamForce, slamRadius, this);
        }

        private void ExecuteBlink(GameObject player, ThirdPersonController controller)
        {
            BlinkAbility blinkAbility = player.GetComponent<BlinkAbility>();
            if (blinkAbility == null)
            {
                blinkAbility = player.AddComponent<BlinkAbility>();
            }
            blinkAbility.PerformBlink(blinkDistance, this);
        }

        public string GetAbilityDescription()
        {
            string baseDesc = $"{maskName} - {abilityType}\n{description}";
            
            switch (maskType)
            {
                case MaskType.Wind:
                    baseDesc += "\n• Enhanced air control\n• Longer dashes and glides";
                    break;
                case MaskType.Stone:
                    baseDesc += "\n• Increased impact\n• Breaks obstacles\n• Stuns enemies";
                    break;
                case MaskType.Flame:
                    baseDesc += "\n• Speed bursts\n• Burning trails\n• Fire damage over time";
                    break;
            }

            return baseDesc;
        }
    }
}
