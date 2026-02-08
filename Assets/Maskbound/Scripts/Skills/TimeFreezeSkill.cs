using UnityEngine;
using System.Collections;
using Maskbound.Scripts.Skills.Interfaces;

namespace Maskbound.Scripts.Skills
{
    [CreateAssetMenu(menuName = "Skills/TimeFreezeSkill")]
    public class TimeFreezeSkill : Skills
    {
        public float freezeDuration = 2f;
        public float restoreDuration = 2f;
        public float minTimeScale = 0.01f;
        public GameObject effectPrefab; // Assign your VFX prefab in the inspector

        public override void ApplyEffect(GameObject player)
        {
            // Spawn effect at player's position
            if (effectPrefab != null)
            {
                GameObject effect = GameObject.Instantiate(effectPrefab, player.transform.position, Quaternion.identity, player.transform);
                GameObject.Destroy(effect, freezeDuration + restoreDuration); // Destroy after total effect duration
            }
            var timeController = player.GetComponent<ITimeController>();
            if (timeController != null)
            {
                timeController.FreezeTime(freezeDuration, restoreDuration, minTimeScale);
            }
        }
    }
}
