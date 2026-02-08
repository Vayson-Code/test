using UnityEngine;
using Maskbound.Scripts.Skills.Interfaces;

namespace Maskbound.Scripts.Skills
{
    [CreateAssetMenu(menuName = "Skills/FirePunchSkill")]
    public class FirePunchSkill : Skills
    {
        public float aoeRadius = 5f;
        public int damage = 20;
        public LayerMask enemyLayer;
        public GameObject effectPrefab; // Assign your VFX prefab in the inspector

        public override void ApplyEffect(GameObject player)
        {
            // Spawn effect at player's position
            if (effectPrefab != null)
            {
                GameObject effect = GameObject.Instantiate(effectPrefab, player.transform.position, Quaternion.identity);
                GameObject.Destroy(effect, 2f); // Destroy after 3 seconds (adjust as needed)
            }
            Collider[] hitColliders = Physics.OverlapSphere(player.transform.position, aoeRadius, enemyLayer);
            foreach (var hitCollider in hitColliders)
            {
                var enemy = hitCollider.GetComponent<IEnemyDamageable>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }
    }
}
