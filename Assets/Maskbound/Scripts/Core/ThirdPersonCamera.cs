using UnityEngine;

namespace Maskbound.Core
{
    /// <summary>
    /// Third-person camera controller with smooth follow and rotation
    /// Can be replaced with Cinemachine for more advanced features
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0);

        [Header("Camera Position")]
        [SerializeField] private float distance = 5f;
        [SerializeField] private float height = 2f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 10f;

        [Header("Rotation")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float gamepadSensitivity = 100f;
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private bool invertY = false;

        [Header("Rotation Limits")]
        [SerializeField] private float minVerticalAngle = -30f;
        [SerializeField] private float maxVerticalAngle = 60f;

        [Header("Collision")]
        [SerializeField] private bool useCollision = true;
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private LayerMask collisionLayers;

        [Header("Shake")]
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float shakeMagnitude = 0.1f;

        // State
        private float currentX = 0f;
        private float currentY = 20f;
        private float currentDistance;
        private Vector3 shakeOffset;
        private float shakeTimer;

        // Smooth damping
        private Vector3 velocity = Vector3.zero;

        private void Start()
        {
            currentDistance = distance;

            // Lock cursor in game
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Initialize rotation based on target
            if (target != null)
            {
                Vector3 direction = transform.position - target.position;
                currentX = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                currentY = Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            UpdateShake();
            UpdateCameraPosition();
        }

        private void HandleInput()
        {
            // Mouse input
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Gamepad input (right stick)
            float gamepadX = Input.GetAxis("Look Horizontal") * gamepadSensitivity * Time.deltaTime;
            float gamepadY = Input.GetAxis("Look Vertical") * gamepadSensitivity * Time.deltaTime;

            // Combine inputs
            float inputX = mouseX + gamepadX;
            float inputY = mouseY + gamepadY;

            // Apply rotation
            currentX += inputX;
            currentY += inputY * (invertY ? 1f : -1f);

            // Clamp vertical rotation
            currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);

            // Zoom with scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                currentDistance -= scroll * 2f;
                currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            }
        }

        private void UpdateCameraPosition()
        {
            // Calculate desired position
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 targetPosition = target.position + targetOffset;

            // Calculate camera position behind target
            Vector3 direction = rotation * -Vector3.forward;
            Vector3 desiredPosition = targetPosition + direction * currentDistance + Vector3.up * height;

            // Check for collision if enabled
            if (useCollision)
            {
                RaycastHit hit;
                Vector3 rayDirection = desiredPosition - targetPosition;
                float rayDistance = rayDirection.magnitude;

                if (Physics.SphereCast(targetPosition, collisionRadius, rayDirection.normalized, out hit, rayDistance, collisionLayers))
                {
                    desiredPosition = targetPosition + rayDirection.normalized * (hit.distance - collisionRadius);
                }
            }

            // Apply shake offset
            desiredPosition += shakeOffset;

            // Smooth follow
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / smoothSpeed);
            transform.position = smoothedPosition;

            // Always look at target
            transform.LookAt(targetPosition);
        }

        private void UpdateShake()
        {
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;

                // Generate random shake offset
                shakeOffset = Random.insideUnitSphere * shakeMagnitude;
                shakeOffset.z = 0; // Don't shake forward/backward

                // Decrease shake over time
                float shakePercent = shakeTimer / shakeDuration;
                shakeOffset *= shakePercent;
            }
            else
            {
                shakeOffset = Vector3.zero;
            }
        }

        public void Shake(float magnitude, float duration)
        {
            shakeMagnitude = magnitude;
            shakeDuration = duration;
            shakeTimer = duration;
        }

        public void Shake()
        {
            shakeTimer = shakeDuration;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void OnDrawGizmosSelected()
        {
            if (target == null) return;

            // Draw camera target point
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position + targetOffset, 0.2f);

            // Draw camera position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);

            // Draw line to target
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position + targetOffset);
        }

        #region Public API
        public void ResetRotation()
        {
            if (target != null)
            {
                currentX = target.eulerAngles.y;
                currentY = 20f;
            }
        }

        public void SetDistance(float newDistance)
        {
            currentDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
        }

        public float GetCurrentX() => currentX;
        public float GetCurrentY() => currentY;
        #endregion
    }
}
