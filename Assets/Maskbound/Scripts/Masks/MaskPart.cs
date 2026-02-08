using UnityEngine;
using System.Collections;
using Maskbound.Scripts.Core;

namespace Maskbound.Core
{
    // Attach this script to the mask piece prefab
    public class MaskPart : MonoBehaviour
    {
        private bool isPickedUp = false;

        private void OnTriggerEnter(Collider other)
        {
            if (isPickedUp) return;
            if (other.CompareTag("Player"))
            {
                isPickedUp = true;
                Debug.Log("Mask part picked up!");
                StartCoroutine(HandleMaskPickup());
                
            }
        }

        private IEnumerator HandleMaskPickup()
        {
            // Optionally: play pickup animation/effects here
            yield return new WaitForSeconds(5f);
            GameManager.Instance.OnMaskObtained();
            Destroy(gameObject);
        }
    }
}

