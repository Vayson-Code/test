using UnityEngine;

public class Mask : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;
    public float lifeTime = 6f;

    [Header("Direction (set by spawner)")]
    public Vector3 moveDir = Vector3.forward;

    [Header("Knockback")]
    public float pushForce = 14f;
    public float upwardPop = 2f;
    public float dodgeHeightTolerance = 0.25f;

    [Header("Feel")]
    public bool resetHorizontalVelocity = true;

    private float timer;

    void Update()
    {
        // MOVE USING moveDir (NOT transform.forward)
        transform.position += moveDir.normalized * speed * Time.deltaTime;

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

        if (playerBottom > maskTop - dodgeHeightTolerance)
        {
            Rigidbody prb = col.rigidbody;
            if (prb != null)
                prb.linearVelocity = new Vector3(prb.linearVelocity.x, Mathf.Max(prb.linearVelocity.y, 6f), prb.linearVelocity.z);

            Destroy(gameObject);
            return;
        }

        Rigidbody rb = col.rigidbody;
        if (rb != null)
        {
            Vector3 pushDir = moveDir;
            pushDir.y = 0f;
            pushDir.Normalize();

            if (resetHorizontalVelocity)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            rb.AddForce(pushDir * pushForce + Vector3.up * upwardPop, ForceMode.VelocityChange);
        }

        Destroy(gameObject);
    }
}
