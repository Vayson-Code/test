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

        public override void ApplyEffect(GameObject player)
        {
            var timeController = player.GetComponent<ITimeController>();
            if (timeController != null)
            {
                timeController.FreezeTime(freezeDuration, restoreDuration, minTimeScale);
            }
        }
    }
}
