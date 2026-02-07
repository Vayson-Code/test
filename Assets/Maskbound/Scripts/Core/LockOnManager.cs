using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

namespace Maskbound.Core
{
    public class LockOnManager : MonoBehaviour
    {
        [Header("Lock-On Settings")]
        [SerializeField] private float lockOnRadius = 10f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private Transform playerTransform;

        [Header("Camera Reference")]
        [SerializeField] private CinemachineCamera cinemachineCam;

        [Header("Lock-On UI")]
        [SerializeField] private GameObject lockOnUIWorldPrefab; // World-space canvas or 3D object
        [SerializeField] private float lockOnUIHeight = 1f; // Adjustable UI height above enemy
        [SerializeField] private float uiOffsetTowardCamera = 0.5f; // How far to push UI toward camera
        private GameObject lockOnUIInstance;

        private List<Enemy> nearbyEnemies = new List<Enemy>();
        private int currentIndex = -1;
        private Enemy currentTarget;
        private bool isLockedOn = false;

        // Input System actions
        private InputAction lockOnAction;
        private InputAction switchTargetAction;

        private void Awake()
        {
            // Auto-find player transform if not assigned
            if (playerTransform == null)
                playerTransform = transform;

            // Auto-find Cinemachine camera if not assigned
            if (cinemachineCam == null)
                cinemachineCam = FindFirstObjectByType<CinemachineCamera>();
        }

        private void OnEnable()
        {
            // Setup Input System actions
            lockOnAction = new InputAction("LockOn", binding: "<Mouse>/middleButton");
            lockOnAction.performed += ctx => ToggleLockOn();
            lockOnAction.Enable();

            switchTargetAction = new InputAction("SwitchTarget", binding: "<Mouse>/scroll");
            switchTargetAction.performed += ctx => OnSwitchTarget(ctx);
            switchTargetAction.Enable();
        }

        private void OnDisable()
        {
            lockOnAction?.Disable();
            switchTargetAction?.Disable();
        }

        private void Update()
        {
            UpdateLockOnUI();
            
            // Optional: Auto-unlock if target is too far
            if (currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(playerTransform.position, currentTarget.transform.position);
                if (distanceToTarget > lockOnRadius * 1.5f)
                {
                    ClearLockOn();
                }
            }
        }

        private void ToggleLockOn()
        {
            if (isLockedOn)
            {
                ClearLockOn();
            }
            else
            {
                FindNearbyEnemies();
                LockOnClosestEnemy();
            }
        }

        private void OnSwitchTarget(InputAction.CallbackContext ctx)
        {
            if (currentTarget != null && nearbyEnemies.Count > 1)
            {
                float scroll = ctx.ReadValue<Vector2>().y; // Use Vector2 for scroll
                if (scroll > 0f)
                    SwitchTarget(1);
                else if (scroll < 0f)
                    SwitchTarget(-1);
            }
        }

        private void FindNearbyEnemies()
        {
            nearbyEnemies.Clear();
            Collider[] hits = Physics.OverlapSphere(playerTransform.position, lockOnRadius, enemyLayer);
            
            Debug.Log($"Found {hits.Length} colliders in range");
            
            foreach (var hit in hits)
            {
                // Try to get Enemy component from the hit object first
                Enemy enemy = hit.GetComponent<Enemy>();
                
                // If not found, search in parent hierarchy
                if (enemy == null)
                    enemy = hit.GetComponentInParent<Enemy>();
                
                // Add enemy if found and not already in list (avoid duplicates from multiple child colliders)
                if (enemy != null && !enemy.IsDead() && !nearbyEnemies.Contains(enemy))
                {
                    nearbyEnemies.Add(enemy);
                    Debug.Log($"Added enemy: {enemy.name}");
                }
            }
            
            // Sort by distance
            nearbyEnemies.Sort((a, b) =>
                Vector3.Distance(playerTransform.position, a.transform.position)
                .CompareTo(Vector3.Distance(playerTransform.position, b.transform.position)));
            
            Debug.Log($"Total valid enemies: {nearbyEnemies.Count}");
        }

        private void LockOnClosestEnemy()
        {
            if (nearbyEnemies.Count == 0)
            {
                Debug.LogWarning("No enemies found in range!");
                ClearLockOn();
                return;
            }
            currentIndex = 0;
            SetLockOnTarget(nearbyEnemies[currentIndex]);
        }

        private void SwitchTarget(int direction)
        {
            if (nearbyEnemies.Count == 0) return;
            currentIndex = (currentIndex + direction + nearbyEnemies.Count) % nearbyEnemies.Count;
            SetLockOnTarget(nearbyEnemies[currentIndex]);
        }

        private void SetLockOnTarget(Enemy enemy)
        {
            currentTarget = enemy;
            isLockedOn = true;
            
            Debug.Log($"Locked onto: {enemy.name}");
            
            // Update Cinemachine Orbital Camera
            if (cinemachineCam != null)
            {
                cinemachineCam.Follow = enemy.transform;
                cinemachineCam.LookAt = enemy.transform;
            }
            else
            {
                Debug.LogWarning("CinemachineCamera is not assigned!");
            }
            
            // Show lock-on UI (world-space canvas or 3D object)
            if (lockOnUIInstance != null)
                Destroy(lockOnUIInstance);
                
            if (lockOnUIWorldPrefab != null)
            {
                lockOnUIInstance = Instantiate(lockOnUIWorldPrefab);
                Debug.Log($"Created lock-on UI for: {enemy.name}");
            }
        }

        private void UpdateLockOnUI()
        {
            if (currentTarget == null || currentTarget.IsDead())
            {
                ClearLockOn();
                return;
            }
            
            // Update UI position to always show in front of enemy relative to camera
            if (lockOnUIInstance != null && Camera.main != null)
            {
                // Calculate position above enemy
                Vector3 enemyPos = currentTarget.transform.position + Vector3.up * lockOnUIHeight;
                
                // Get direction from camera to enemy
                Vector3 cameraToEnemy = (enemyPos - Camera.main.transform.position).normalized;
                
                // Place UI slightly in front of the enemy toward the camera
                Vector3 uiPos = enemyPos - cameraToEnemy * uiOffsetTowardCamera;
                
                lockOnUIInstance.transform.position = uiPos;
                
                // Make UI face camera
                lockOnUIInstance.transform.forward = Camera.main.transform.forward;
            }
        }

        private void ClearLockOn()
        {
            currentTarget = null;
            currentIndex = -1;
            isLockedOn = false;
            
            if (lockOnUIInstance != null)
                Destroy(lockOnUIInstance);
                
            if (cinemachineCam != null)
            {
                cinemachineCam.Follow = playerTransform;
                cinemachineCam.LookAt = playerTransform;
            }
            
            Debug.Log("Lock-on cleared");
        }

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            if (playerTransform == null) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, lockOnRadius);
            
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(playerTransform.position, currentTarget.transform.position);
            }
        }
    }
}