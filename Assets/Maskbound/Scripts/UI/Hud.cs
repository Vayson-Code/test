using System;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

public class Hud : MonoBehaviour
    { 
        [SerializeField] private Image healthBar;
       [SerializeField] private ThirdPersonController playerController;
        private void Start()
        {
           playerController.OnHealthChanged+= UpdateHealthDisplay;
        }

        private void UpdateHealthDisplay(object sender, ThirdPersonController.ddHealthChangedEventArgs e)
        {
            // Here you would update your health bar UI based on e.currentHealth and e.maxHealth
                float healthPercent = e.hp;
            Debug.Log($"Health Updated:"+e.hp);
        }
    }