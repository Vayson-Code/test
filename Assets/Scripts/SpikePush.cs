using UnityEngine;

public class SpikePushAnySide : MonoBehaviour
{
    [Header("Knockback")]
    public float pushForce = 14f;
    public float upwardPop = 2f;

    [Header("Feel")]
    public bool resetHorizontalVelocity = true;

    private Collider spikeCol;

    void Awake()
    {
        spikeCol = GetComponent<Collider>();
    }

    void OnCollisionEnter(Collision col)
    {
        Push(col);
    }

    void OnCollisionStay(Collision col)
    {
        // Optional: keeps pushing if player keeps touching
        // Remove this method if you only want one push on enter
        Push(col);
    }

    void Push(Collision col)
    {
        if (!col.gameObject.CompareTag("Player"))
            return;

        Rigidbody rb = col.rigidbody;
        if (rb == null || spikeCol == null)
            return;

        Vector3 playerPos = col.transform.position;

        // Get the closest point on the spike collider to the player
        Vector3 closest = spikeCol.ClosestPoint(playerPos);

        // Push direction = away from the spike
        Vector3 pushDir = (playerPos - closest);
        pushDir.y = 0f;

        // Fallback if somehow perfectly centered
        if (pushDir.sqrMagnitude < 0.0001f)
            pushDir = -transform.forward;

        pushDir.Normalize();

        if (resetHorizontalVelocity)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        rb.AddForce(pushDir * pushForce + Vector3.up * upwardPop, ForceMode.VelocityChange);
    }
}
