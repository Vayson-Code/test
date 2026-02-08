using UnityEngine;
using System.Collections.Generic;

namespace Maskbound.Scripts.Enemis
{
    public class EnemyManager : MonoBehaviour
    {
        #region Singleton
        public static EnemyManager Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region Fields
        [SerializeField] private GameObject maskPiecePrefab;
        [SerializeField] private int dropCount = 1;
        private List<GameObject> enemies = new List<GameObject>();
        private GameObject lastEnemyKilled;
        #endregion

        #region Enemy Tracking
        
        public void RegisterEnemy(GameObject enemy)
        {
            if (!enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        public void UnregisterEnemy(GameObject enemy)
        {
            if (enemies.Contains(enemy))
            {
                enemies.Remove(enemy);
                lastEnemyKilled = enemy;
                if (enemies.Count == 0)
                {
                    DropMaskPiece();
                }
            }
        }
        #endregion

        #region Mask Drop
        private void DropMaskPiece()
        {
            if (maskPiecePrefab == null || lastEnemyKilled == null) return;
            Vector3 dropPosition = lastEnemyKilled.transform.position;
            Instantiate(maskPiecePrefab, dropPosition, Quaternion.identity);
            Debug.Log("Mask piece dropped at: " + dropPosition);
        }
        #endregion
    }
}