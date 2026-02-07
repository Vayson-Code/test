using UnityEngine;
using System.Collections;
using Maskbound.Scripts.Skills.Interfaces;

namespace Maskbound.Scripts.Skills
{
    [CreateAssetMenu(menuName = "Skills/IceShieldSkill")]
    public class IceShieldSkill : Skills
    {
        public float shieldDuration = 5f;

        public override void ApplyEffect(GameObject player)
        {
            var shield = player.GetComponent<IPlayreShield>();
            if (shield != null)
            {
                shield.ActivateShield(shieldDuration);
            }
        }
    }
}
