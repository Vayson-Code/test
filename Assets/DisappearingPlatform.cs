using UnityEngine;
using System.Collections;

public class DisappearingPlatform : MonoBehaviour
{
    [Header("Timing")]
    public float disappearDelay = 2f;   // ⏱ delay BEFORE disappearing 
    public float respawnDelay = 5f;        // ⏱ delay BEFORE reappearing

    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private bool isActive = true;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isActive)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(DisappearRoutine());
        }
    }

    IEnumerator DisappearRoutine()
    {
        isActive = false;

        // ⏳ wait before disappearing
        yield return new WaitForSeconds(disappearDelay);

        // Disable
        meshRenderer.enabled = false;
        meshCollider.enabled = false;

        // ⏳ wait before reappearing
        yield return new WaitForSeconds(respawnDelay);

        // Re-enable
        meshRenderer.enabled = true;
        meshCollider.enabled = true;

        isActive = true;
    }
}

