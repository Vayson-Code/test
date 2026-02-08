using System;
using UnityEngine;

namespace Maskbound.Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        public event EventHandler<MaskObtainedEventArgs> OnMaskObtainedEvent;
        public class MaskObtainedEventArgs : EventArgs
        {
            public int newMapIndex;
        }
        public static GameManager Instance { get; private set; }

        //public MapManager mapManager;

        // Track current map index
        private int currentMapIndex;
        public int CurrentMapIndex => currentMapIndex;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            currentMapIndex = 3;
        }

        // Call this when the player obtains the mask
        public void OnMaskObtained()
        {
            ProgressToNextMap();
        }

        // Progress to the next map and regenerate dynamically
        private void ProgressToNextMap()
        { 
            currentMapIndex++;
          OnMaskObtainedEvent?.Invoke(this, new MaskObtainedEventArgs { newMapIndex = currentMapIndex });
        }
    }
}