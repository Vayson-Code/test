using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Maskbound.Scripts.Core
{
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] mapGameObjects;
        [SerializeField] private string[] mapSceneNames; // Add scene names for each map

        private void Start()
        {
            GameManager.Instance.OnMaskObtainedEvent += RegenerateMap;
        }

        public void RegenerateMap(object sender, GameManager.MaskObtainedEventArgs e)
        {
            // Load the next scene directly
            SceneManager.LoadScene(mapSceneNames[e.newMapIndex]);
        }
    }
}
