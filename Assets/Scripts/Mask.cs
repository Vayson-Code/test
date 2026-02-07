using UnityEngine;

public class Mask : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;
    public float lifeTime = 6f;

    [Header("Knockback")]
    public float pushForce = 14f;          // increase for stronger push
    public float upwardPop = 2f;           // little pop upward
    public float dodgeHeightTolerance = 0.25f;

    [Header("Feel")]
    public bool resetHorizontalVelocity = true; // makes push feel stronger/cleaner

    private float timer;

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("Player"))
            return;

        Collider maskCol = GetComponent<Collider>();
        Collider playerCol = col.collider;

        float maskTop = maskCol.bounds.max.y;
        float playerBottom = playerCol.bounds.min.y;

        // Player is above → dodge (jumped it)
        if (playerBottom > maskTop - dodgeHeightTolerance)
        {
            Rigidbody prb = col.rigidbody;
            if (prb != null)
            {
                prb.linearVelocity = new Vector3(prb.linearVelocity.x, Mathf.Max(prb.linearVelocity.y, 6f), prb.linearVelocity.z);
            }

            Destroy(gameObject);
            return;
        }

        // Push player in the SAME direction the mask moves
        Rigidbody rb = col.rigidbody;
        if (rb != null)
        {
            Vector3 pushDir = transform.forward;
            pushDir.y = 0f;
            pushDir.Normalize();

            if (resetHorizontalVelocity)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            rb.AddForce(pushDir * pushForce + Vector3.up * upwardPop, ForceMode.VelocityChange);
        }

        Destroy(gameObject);
    }
}
