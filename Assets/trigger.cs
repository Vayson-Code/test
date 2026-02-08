using UnityEngine;

public class trigger : MonoBehaviour
{
[Header("Target")]
    public GameObject targetToDisable; // drag the GameObject here
    public GameObject platformToDisable;
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (targetToDisable != null)
            targetToDisable.SetActive(false);
        if (platformToDisable != null)
            platformToDisable.SetActive(false);

        // Optional: destroy THIS script (not required, but explicit)
        Destroy(this);
    } 
}
