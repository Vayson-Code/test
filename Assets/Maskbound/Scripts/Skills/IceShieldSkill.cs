using UnityEngine;
using System.Collections;
using Maskbound.Scripts.Skills.Interfaces;

namespace Maskbound.Scripts.Skills
{
    [CreateAssetMenu(menuName = "Skills/IceShieldSkill")]
    public class IceShieldSkill : Skills
    {
        public float shieldDuration = 5f;
        public GameObject effectPrefab; // Assign your VFX prefab in the inspector

        public override void ApplyEffect(GameObject player)
        {
            // Spawn effect at player's position
            if (effectPrefab != null)
            {
                GameObject effect = GameObject.Instantiate(effectPrefab, player.transform.position, Quaternion.identity, player.transform);
                GameObject.Destroy(effect, shieldDuration); // Destroy after shield duration
            }
            var shield = player.GetComponent<IPlayreShield>();
            if (shield != null)
            {
                shield.ActivateShield(shieldDuration);
            }
        }
    }
}
