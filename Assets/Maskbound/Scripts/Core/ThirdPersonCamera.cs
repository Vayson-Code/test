using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace Maskbound.Core
{
    /// <summary>
    /// Simple input handler for Cinemachine FreeLook camera
    /// FreeLook handles all the heavy lifting - we just feed it input
    /// </summary>
    public class ThirdPersonCameraInput : MonoBehaviour
    {
        [SerializeField] private CinemachineFreeLook freeLookCamera;
        [SerializeField] private string horizontalAxisName = "Mouse X";
        [SerializeField] private string verticalAxisName = "Mouse Y";

        private void Awake()
        {
            if (freeLookCamera == null)
            {
                freeLookCamera = GetComponent<CinemachineFreeLook>();
            }

            if (freeLookCamera != null)
            {
                // Set axis names so FreeLook can read input
                freeLookCamera.m_XAxis.m_InputAxisName = horizontalAxisName;
                freeLookCamera.m_YAxis.m_InputAxisName = verticalAxisName;
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SetTarget(Transform newTarget)
        {
            if (freeLookCamera != null)
            {
                freeLookCamera.Follow = newTarget;
                freeLookCamera.LookAt = newTarget;
            }
        }

        public void Shake(float intensity)
        {
            var impulse = freeLookCamera.GetComponent<CinemachineImpulseSource>();
            if (impulse != null)
            {
                impulse.GenerateImpulse(intensity);
            }
        }
    }
}