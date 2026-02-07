using UnityEngine;

namespace Maskbound.Scripts.Skills
{
    public class Skills : ScriptableObject
    {
        //scriptable object for skills, each skill will have a name, description, and an effect that can be applied to the player when the skill is obtained
        public string skillName;
        public string description;
        public Sprite icon;
        
            // Method to apply the skill's effect to the player
            public virtual void ApplyEffect(GameObject player)
    {
        // Override this method in derived skill classes to apply specific effects to the player
    }
    }
}