using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // Required for loading scenes
using Maskbound.Scripts.Core;

namespace Maskbound.Core
{
    // Attach this script to the mask piece prefab
    public class MaskPart : MonoBehaviour
    {
        [SerializeField] private GameObject VfxPrefab; // Optional: assign a VFX prefab for pickup effect
        [SerializeField] private AudioClip pickupSound; // Optional: assign a pickup sound effect
        [SerializeField] private AudioSource audioSource; // Optional: assign an AudioSource for playing the sound
        
        private bool isPickedUp = false;

        private void Awake()
        {
            // Auto-grab AudioSource if not assigned
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isPickedUp) return;

            if (other.CompareTag("Player"))
            {
                isPickedUp = true;

                // 1. Play VFX
                if (VfxPrefab != null)
                {
                    Instantiate(VfxPrefab, transform.position, Quaternion.identity);
                }

                // 2. Play Sound
                if (audioSource != null && pickupSound != null)
                {
                    audioSource.PlayOneShot(pickupSound);
                }

                // 3. Hide the mask visual (so it looks like it's been "picked up" instantly)
                // We don't destroy yet because we need the Coroutine to finish
                foreach (Renderer r in GetComponentsInChildren<Renderer>())
                {
                    r.enabled = false;
                }

                StartCoroutine(HandleMaskPickup());
            }
        }

        private IEnumerator HandleMaskPickup()
        {
            // Wait for effects or dramatic pause
            yield return new WaitForSeconds(5f);

            // Notify the GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMaskObtained();
            }
        }
    }
}