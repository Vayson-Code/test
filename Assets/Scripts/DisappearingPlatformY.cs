using UnityEngine;
using System.Collections;

public class DisappearingPlatformsY : MonoBehaviour
{
    [Header("Timing")]
    public float respawnDelay = 5f;

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

        // Disable instantly
        meshRenderer.enabled = false;
        meshCollider.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        // Re-enable
        meshRenderer.enabled = true;
        meshCollider.enabled = true;

        isActive = true;
    }
}
