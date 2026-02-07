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

        public override void ApplyEffect(GameObject player)
        {
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
