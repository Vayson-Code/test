using UnityEngine;
using UnityEngine.UI;

namespace Maskbound.Core
{
    // Attach this script to the health bar UI object
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Enemy enemy;
        [SerializeField] private Image healthFillImage;

        private void Awake()
        {
            if (enemy == null)
                enemy = GetComponentInParent<Enemy>();
            if (healthFillImage == null)
                healthFillImage = GetComponentInChildren<Image>(); // Assign the filled image
        }

        private void OnEnable()
        {
            if (enemy != null)
                enemy.OnHealthChanged.AddListener(UpdateHealthBar);
        }

        private void OnDisable()
        {
            if (enemy != null)
                enemy.OnHealthChanged.RemoveListener(UpdateHealthBar);
        }

        private void UpdateHealthBar(float healthPercent)
        {
            if (healthFillImage != null)
                healthFillImage.fillAmount = healthPercent;
        }
    }
}
