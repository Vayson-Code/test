using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // NEW input system

public class PlayerInteraction : MonoBehaviour
{
    public Camera mainCam;
    public float interactionDistance = 2f;

    public GameObject interactionUI;
    public TextMeshProUGUI interactionText;

    [Header("Ray Origin")]
    public Transform rayOrigin;
    public Vector3 originOffset = new Vector3(0f, 1f, 0f);

    private void Update()
    {
        InteractionRay();
    }

    void InteractionRay()
    {
        Transform originT = rayOrigin != null ? rayOrigin : transform;
        Vector3 origin = originT.position + originT.TransformDirection(originOffset);
        Vector3 direction = originT.forward;

        bool hitSomething = false;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                hitSomething = true;
                interactionText.text = interactable.GetDescription();

                // ✅ NEW INPUT SYSTEM — I KEY
                if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
                {
                    interactable.Interact();
                }
            }
        }

        if (interactionUI != null)
            interactionUI.SetActive(hitSomething);

        Debug.DrawRay(origin, direction * interactionDistance,
            hitSomething ? Color.green : Color.red);
    }
}
