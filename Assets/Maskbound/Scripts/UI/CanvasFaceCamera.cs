using UnityEngine;

namespace Maskbound.UI
{
    // Attach this script to your Canvas or UI root object
    public class CanvasFaceCamera : MonoBehaviour
    {
        private Transform camTransform;

        private void Start()
        {
            if (Camera.main != null)
                camTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (camTransform == null)
                return;
            // Make the canvas face the camera
            transform.forward = camTransform.forward;
        }
    }
}
