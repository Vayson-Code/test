using UnityEngine;

public class ActivatePlatformOnce : MonoBehaviour, IInteractable
{
    [SerializeField] private MovingPlatform platform;
    [SerializeField] private string description = "Activate platform";

    public void Interact()
    {
        if (platform == null)
            return;

        platform.Activate();

        // Destroy THIS script after interaction
        Destroy(this);
    }

    public string GetDescription()
    {
        return description;
    }
}
