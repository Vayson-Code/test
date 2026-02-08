using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

namespace Maskbound.Core
{
    public class LockOnManager : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private CinemachineCamera freeLookCam; // Drag "FreeLook Camera" here
        [SerializeField] private CinemachineCamera lockOnCam;  // Drag "back follow" here

        [Header("Lock-On Settings")]
        [SerializeField] private float lockOnRadius = 15f;
        [SerializeField] private float lookRotationSpeed = 10f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private Transform playerTransform;

        [Header("Lock-On UI")]
        [SerializeField] private GameObject lockOnUIWorldPrefab;
        [SerializeField] private float lockOnUIHeight = 1.5f;
        [SerializeField] private float uiOffsetTowardCamera = 0.5f;
        private GameObject lockOnUIInstance;

        private List<Enemy> nearbyEnemies = new List<Enemy>();
        private int currentIndex = -1;
        private Enemy currentTarget;
        private bool isLockedOn = false;

        private InputAction lockOnAction;
        private InputAction switchTargetAction;

        private void Awake()
        {
            if (playerTransform == null) playerTransform = transform;
            
            // Initial Camera State
            if (freeLookCam != null) freeLookCam.Priority = 10;
            if (lockOnCam != null) lockOnCam.Priority = 5;
        }

        private void OnEnable()
        {
            lockOnAction = new InputAction("LockOn", binding: "<Mouse>/middleButton");
            lockOnAction.performed += _ => ToggleLockOn();
            lockOnAction.Enable();

            switchTargetAction = new InputAction("SwitchTarget", binding: "<Mouse>/scroll");
            switchTargetAction.performed += OnSwitchTarget;
            switchTargetAction.Enable();
        }

        private void OnDisable()
        {
            lockOnAction?.Disable();
            switchTargetAction?.Disable();
        }

        private void Update()
        {
            if (isLockedOn && currentTarget != null)
            {
                // Force Player to rotate toward Enemy (Y-axis only)
                Vector3 dir = currentTarget.transform.position - playerTransform.position;
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    playerTransform.rotation = Quaternion.Slerp(
                        playerTransform.rotation, 
                        Quaternion.LookRotation(dir), 
                        Time.deltaTime * lookRotationSpeed);
                }

                // Check for death or distance
                if (currentTarget.IsDead() || Vector3.Distance(playerTransform.position, currentTarget.transform.position) > lockOnRadius * 1.5f)
                {
                    ClearLockOn();
                }
            }

            UpdateLockOnUI();
        }

        private void ToggleLockOn()
        {
            if (isLockedOn) ClearLockOn();
            else SearchAndLock();
        }

        private void SearchAndLock()
        {
            nearbyEnemies.Clear();
            Collider[] hits = Physics.OverlapSphere(playerTransform.position, lockOnRadius, enemyLayer);
            
            foreach (var hit in hits)
            {
                Enemy enemy = hit.GetComponent<Enemy>() ?? hit.GetComponentInParent<Enemy>();
                if (enemy != null && !enemy.IsDead()) nearbyEnemies.Add(enemy);
            }

            if (nearbyEnemies.Count > 0)
            {
                nearbyEnemies.Sort((a, b) => Vector3.Distance(playerTransform.position, a.transform.position)
                    .CompareTo(Vector3.Distance(playerTransform.position, b.transform.position)));
                
                SetLockOnTarget(nearbyEnemies[0]);
            }
        }

        private void SetLockOnTarget(Enemy enemy)
        {
            currentTarget = enemy;
            isLockedOn = true;

            if (lockOnCam != null && freeLookCam != null)
            {
                // Set the "back follow" camera to watch the enemy
                lockOnCam.LookAt = currentTarget.transform;
                
                // Switch priorities to swap cameras
                lockOnCam.Priority = 20;
                freeLookCam.Priority = 10;
            }

            if (lockOnUIInstance != null) Destroy(lockOnUIInstance);
            if (lockOnUIWorldPrefab != null) lockOnUIInstance = Instantiate(lockOnUIWorldPrefab);
        }

        private void ClearLockOn()
        {
            isLockedOn = false;
            currentTarget = null;

            if (lockOnCam != null && freeLookCam != null)
            {
                lockOnCam.Priority = 5;
                freeLookCam.Priority = 10;
            }

            if (lockOnUIInstance != null) Destroy(lockOnUIInstance);
        }

        private void OnSwitchTarget(InputAction.CallbackContext ctx)
        {
            if (!isLockedOn || nearbyEnemies.Count <= 1) return;
            float scroll = ctx.ReadValue<Vector2>().y;
            int step = scroll > 0 ? 1 : -1;
            currentIndex = (currentIndex + step + nearbyEnemies.Count) % nearbyEnemies.Count;
            SetLockOnTarget(nearbyEnemies[currentIndex]);
        }

        private void UpdateLockOnUI()
        {
            if (currentTarget == null || !isLockedOn || lockOnUIInstance == null) return;

            Vector3 enemyPos = currentTarget.transform.position + Vector3.up * lockOnUIHeight;
            Vector3 camPos = Camera.main.transform.position;
            Vector3 dirToCam = (camPos - enemyPos).normalized;
            
            lockOnUIInstance.transform.position = enemyPos + dirToCam * uiOffsetTowardCamera;
            lockOnUIInstance.transform.forward = Camera.main.transform.forward;
        }
    }
}