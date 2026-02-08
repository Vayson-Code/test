using Maskbound.Scripts.Core;
using UnityEngine;

namespace Maskbound.Core
{
    public class MaskPartsManager : MonoBehaviour
    {
        // this script is responsible for managing the visual parts of the mask by enabling the next part of the mask when the player obtains a new mask
        
            [SerializeField] private GameObject[] maskParts; // Array of mask part GameObjects to enable sequentially
        
            
            private void Start()
            {
                for (int i = 0; i <= GameManager.Instance.CurrentMapIndex; i++)
                {
                    if (maskParts[i] == null)
                    {
                        Debug.LogError($"MaskPartsManager: Mask part at index {i} is not assigned in the inspector.");
                    }
                    else {
                        maskParts[i].SetActive(true);
                    }
                }
                // Subscribe to the mask obtained event
                GameManager.Instance.OnMaskObtainedEvent += OnMaskObtained;
                
                // Ensure all mask parts are initially disabled
                foreach (var part in maskParts)
                {
                    part.SetActive(false);
                }
            }

            private void OnMaskObtained(object sender, GameManager.MaskObtainedEventArgs e)
            {
                    maskParts[e.newMapIndex].SetActive(true);
            }
    }
}