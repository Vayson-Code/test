using System;
using UnityEngine;

namespace Maskbound.Scripts.Core
{
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] mapGameObjects;

        private void Start()
        {
            GameManager.Instance.OnMaskObtainedEvent += RegenerateMap;
        }

        public void RegenerateMap(object sender, GameManager.MaskObtainedEventArgs e)
        {
            for (int i = 0; i < mapGameObjects.Length; i++)
            {
                mapGameObjects[i].SetActive(i == e.newMapIndex);
            }
            Debug.Log($"Enabled map index: {e.newMapIndex}, disabled others.");
        }
    }
}
